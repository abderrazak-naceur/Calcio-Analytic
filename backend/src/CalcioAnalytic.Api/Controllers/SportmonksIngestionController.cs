using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Contracts.Providers;
using CalcioAnalytic.Ingestion.Providers;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CalcioAnalytic.Api.Controllers;

[ApiController]
[Route("api/v1/ingestion/sportmonks")]
public sealed class SportmonksIngestionController : ControllerBase
{
    private const string ProviderCode = "sportmonks";
    private readonly IProviderRegistry _providers;
    private readonly ICatalogIngestionService _catalog;
    private readonly IMatchIngestionService _matches;
    private readonly IOddsIngestionService _odds;

    public SportmonksIngestionController(
        IProviderRegistry providers,
        ICatalogIngestionService catalog,
        IMatchIngestionService matches,
        IOddsIngestionService odds)
    {
        _providers = providers;
        _catalog = catalog;
        _matches = matches;
        _odds = odds;
    }

    [HttpGet("leagues")]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderCompetitionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProviderCompetitionDto>>> GetLeagues(
        CancellationToken ct)
    {
        var football = _providers.Get<IFootballProvider>(ProviderCode);
        return Ok(await football.GetCompetitionsAsync(ct).ConfigureAwait(false));
    }

    [HttpGet("leagues/{leagueId}/seasons")]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderSeasonDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProviderSeasonDto>>> GetSeasons(
        string leagueId,
        CancellationToken ct)
    {
        var football = _providers.Get<IFootballProvider>(ProviderCode);
        return Ok(await football.GetSeasonsAsync(leagueId, ct).ConfigureAwait(false));
    }

    [HttpPost("seasons/import")]
    [ProducesResponseType(typeof(SportmonksSeasonImportResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SportmonksSeasonImportResult>> ImportSeason(
        [FromBody] SportmonksSeasonImportRequest request,
        CancellationToken ct)
    {
        var catalog = await _catalog.IngestCatalogAsync(
            ProviderCode,
            request.LeagueId,
            request.SeasonId,
            ct).ConfigureAwait(false);
        var fixtures = await _matches.IngestFixturesAsync(
            ProviderCode,
            request.LeagueId,
            request.SeasonId,
            ct).ConfigureAwait(false);

        var fixtureProvider = _providers.Get<IFixtureProvider>(ProviderCode);
        var providerFixtures = await fixtureProvider.GetFixturesAsync(
            request.LeagueId,
            request.SeasonId,
            ct).ConfigureAwait(false);

        var completedFixtures = providerFixtures
            .Where(IsCompleted)
            .ToArray();
        var snapshotsInserted = 0;
        var snapshotsSkipped = 0;
        var oddsRequestsAttempted = 0;
        string? oddsWarning = null;
        foreach (var fixture in completedFixtures)
        {
            oddsRequestsAttempted++;
            OddsIngestionResult result;
            try
            {
                result = await _odds.IngestOddsAsync(ProviderCode, fixture.ExternalId, ct)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
            {
                oddsWarning =
                    "Sportmonks denied access to the pre-match odds endpoint. " +
                    "Catalog and fixtures were imported, but odds were skipped. " +
                    "Enable the Sportmonks Odds add-on to import odds snapshots.";
                break;
            }

            snapshotsInserted += result.SnapshotsInserted;
            snapshotsSkipped += result.SnapshotsSkippedDuplicate;
        }

        return Ok(new SportmonksSeasonImportResult(
            request.LeagueId,
            request.SeasonId,
            fixtures.MatchesUpserted,
            completedFixtures.Length,
            oddsRequestsAttempted,
            snapshotsInserted,
            snapshotsSkipped,
            oddsWarning,
            catalog));
    }

    private static bool IsCompleted(ProviderMatchDto fixture) =>
        fixture.HomeScore is not null &&
        fixture.AwayScore is not null &&
        fixture.Status.Equals("finished", StringComparison.OrdinalIgnoreCase);
}

public sealed record SportmonksSeasonImportRequest(string LeagueId, string SeasonId);

public sealed record SportmonksSeasonImportResult(
    string LeagueId,
    string SeasonId,
    int FixturesUpserted,
    int CompletedFixturesWithOddsRequested,
    int OddsRequestsAttempted,
    int OddsSnapshotsInserted,
    int DuplicateOddsSnapshotsSkipped,
    string? OddsWarning,
    CatalogIngestionResult Catalog);
