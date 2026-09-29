using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Application.Ingestion;
using Microsoft.AspNetCore.Mvc;

namespace CalcioAnalytic.Api.Controllers;

/// <summary>
/// Write endpoints that trigger provider ingestion runs. Each action is a thin
/// delegation to the corresponding application ingestion service and returns the
/// service's own result summary.
/// </summary>
[ApiController]
[Route("api/v1/ingestion")]
public sealed class IngestionController : ControllerBase
{
    private readonly ICatalogIngestionService _catalog;
    private readonly IMatchIngestionService _match;
    private readonly IOddsIngestionService _odds;
    private readonly IStatisticsIngestionService _statistics;

    public IngestionController(
        ICatalogIngestionService catalog,
        IMatchIngestionService match,
        IOddsIngestionService odds,
        IStatisticsIngestionService statistics)
    {
        _catalog = catalog;
        _match = match;
        _odds = odds;
        _statistics = statistics;
    }

    /// <summary>
    /// Ingests the catalog (competition, seasons, teams, bookmakers, markets) for
    /// a provider competition and season.
    /// </summary>
    /// <param name="request">The provider, competition, and season identifiers.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpPost("catalog")]
    [ProducesResponseType(typeof(CatalogIngestionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogIngestionResult>> IngestCatalog(
        [FromBody] CatalogIngestionRequest request,
        CancellationToken ct)
    {
        var result = await _catalog.IngestCatalogAsync(
            request.ProviderCode,
            request.CompetitionExternalId,
            request.SeasonExternalId,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Ingests all fixtures for a provider competition and season.
    /// </summary>
    /// <param name="request">The provider, competition, and season identifiers.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpPost("fixtures")]
    [ProducesResponseType(typeof(MatchIngestionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchIngestionResult>> IngestFixtures(
        [FromBody] FixturesIngestionRequest request,
        CancellationToken ct)
    {
        var result = await _match.IngestFixturesAsync(
            request.ProviderCode,
            request.CompetitionExternalId,
            request.SeasonExternalId,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Ingests a single fixture by its provider-native identifier, returning the
    /// canonical match identifier. Returns 404 when the provider has no such
    /// fixture.
    /// </summary>
    /// <param name="request">The provider and match identifiers.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpPost("fixture")]
    [ProducesResponseType(typeof(FixtureIngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FixtureIngestionResponse>> IngestFixture(
        [FromBody] MatchIngestionRequest request,
        CancellationToken ct)
    {
        var matchId = await _match.IngestFixtureAsync(
            request.ProviderCode,
            request.MatchExternalId,
            ct);

        return matchId is null
            ? NotFound()
            : Ok(new FixtureIngestionResponse(matchId.Value));
    }

    /// <summary>
    /// Ingests the odds snapshots the provider exposes for a single match.
    /// </summary>
    /// <param name="request">The provider and match identifiers.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpPost("odds")]
    [ProducesResponseType(typeof(OddsIngestionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<OddsIngestionResult>> IngestOdds(
        [FromBody] MatchIngestionRequest request,
        CancellationToken ct)
    {
        var result = await _odds.IngestOddsAsync(
            request.ProviderCode,
            request.MatchExternalId,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Ingests the statistics and events the provider exposes for a single match.
    /// </summary>
    /// <param name="request">The provider and match identifiers.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpPost("statistics")]
    [ProducesResponseType(typeof(StatisticsIngestionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<StatisticsIngestionResult>> IngestStatistics(
        [FromBody] MatchIngestionRequest request,
        CancellationToken ct)
    {
        var result = await _statistics.IngestStatisticsAsync(
            request.ProviderCode,
            request.MatchExternalId,
            ct);

        return Ok(result);
    }
}
