using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Contracts.Providers;
using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Ingestion.Providers;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

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
    private readonly CalcioAnalyticDbContext _db;

    public SportmonksIngestionController(
        IProviderRegistry providers,
        ICatalogIngestionService catalog,
        IMatchIngestionService matches,
        IOddsIngestionService odds,
        CalcioAnalyticDbContext db)
    {
        _providers = providers;
        _catalog = catalog;
        _matches = matches;
        _odds = odds;
        _db = db;
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
        return Ok(await ImportSeasonCore(request.LeagueId, request.SeasonId, request.SeedDemoOdds, ct)
            .ConfigureAwait(false));
    }

    [HttpPost("seasons/import-all")]
    [ProducesResponseType(typeof(SportmonksBulkSeasonImportResult), StatusCodes.Status200OK)]
    [RequestSizeLimit(1_048_576)]
    public async Task<ActionResult<SportmonksBulkSeasonImportResult>> ImportAllLeaguesForSeason(
        [FromBody] SportmonksBulkSeasonImportRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SeasonLabelOrId))
        {
            return BadRequest(new { message = "seasonLabelOrId is required." });
        }

        if (request.SeasonLabelOrId.Trim().Length > 64)
        {
            return BadRequest(new { message = "seasonLabelOrId must be at most 64 characters." });
        }

        // Production-safe default: importing every subscribed league in one
        // request can take a very long time and hit provider rate limits.
        // Callers can still opt into more via MaxLeagues/SkipLeagues.
        const int defaultMaxLeagues = 10;
        const int hardMaxLeagues = 250;
        var maxLeagues = request.MaxLeagues is > 0 ? request.MaxLeagues.Value : defaultMaxLeagues;
        maxLeagues = Math.Min(maxLeagues, hardMaxLeagues);
        var skipLeagues = Math.Max(0, request.SkipLeagues ?? 0);

        var football = _providers.Get<IFootballProvider>(ProviderCode);
        var leagues = await football.GetCompetitionsAsync(ct).ConfigureAwait(false);
        var imported = new List<SportmonksSeasonImportResult>();
        var skipped = new List<SportmonksBulkSeasonSkip>();

        foreach (var league in leagues.Skip(skipLeagues).Take(maxLeagues))
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<ProviderSeasonDto> seasons;
            try
            {
                seasons = await football.GetSeasonsAsync(league.ExternalId, ct).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
            {
                skipped.Add(new SportmonksBulkSeasonSkip(league.ExternalId, league.Name, exception.Message));
                continue;
            }

            var season = FindSeason(seasons, request.SeasonLabelOrId);
            if (season is null)
            {
                skipped.Add(new SportmonksBulkSeasonSkip(
                    league.ExternalId,
                    league.Name,
                    $"No season matching '{request.SeasonLabelOrId}' was returned for this league."));
                continue;
            }

            try
            {
                imported.Add(await ImportSeasonCore(league.ExternalId, season.ExternalId, request.SeedDemoOdds, ct)
                    .ConfigureAwait(false));
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
            {
                skipped.Add(new SportmonksBulkSeasonSkip(league.ExternalId, league.Name, exception.Message));
            }
        }

        return Ok(new SportmonksBulkSeasonImportResult(
            request.SeasonLabelOrId,
            imported.Count,
            skipped.Count,
            imported.Sum(x => x.FixturesUpserted),
            imported.Sum(x => x.OddsSnapshotsInserted),
            imported.Sum(x => x.DemoOddsSnapshotsInserted),
            imported,
            skipped));
    }

    [HttpPost("demo-odds/seed")]
    [ProducesResponseType(typeof(DemoOddsSeedResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<DemoOddsSeedResult>> SeedDemoOdds(
        [FromBody] DemoOddsSeedRequest request,
        CancellationToken ct)
    {
        return Ok(await SeedDemoOddsAsync(request.LeagueId, request.SeasonId, ct).ConfigureAwait(false));
    }

    private async Task<SportmonksSeasonImportResult> ImportSeasonCore(
        string leagueId,
        string seasonId,
        bool seedDemoOdds,
        CancellationToken ct)
    {
        var catalog = await _catalog.IngestCatalogAsync(
            ProviderCode,
            leagueId,
            seasonId,
            ct).ConfigureAwait(false);
        var fixtures = await _matches.IngestFixturesAsync(
            ProviderCode,
            leagueId,
            seasonId,
            ct).ConfigureAwait(false);

        var fixtureProvider = _providers.Get<IFixtureProvider>(ProviderCode);
        var providerFixtures = await fixtureProvider.GetFixturesAsync(
            leagueId,
            seasonId,
            ct).ConfigureAwait(false);

        var completedFixtures = providerFixtures
            .Where(IsCompleted)
            .ToArray();
        var snapshotsInserted = 0;
        var snapshotsSkipped = 0;
        var oddsRequestsAttempted = 0;
        string? oddsWarning = null;
        var demoOddsInserted = 0;
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

        if (seedDemoOdds && snapshotsInserted == 0)
        {
            var demoResult = await SeedDemoOddsAsync(leagueId, seasonId, ct).ConfigureAwait(false);
            demoOddsInserted = demoResult.SnapshotsInserted;
            oddsWarning = (oddsWarning is null ? string.Empty : oddsWarning + " ") +
                "Development demo odds were seeded so the analytics screens have data. " +
                "These odds are synthetic and must not be used as real bookmaker prices.";
        }

        return new SportmonksSeasonImportResult(
            leagueId,
            seasonId,
            fixtures.MatchesUpserted,
            completedFixtures.Length,
            oddsRequestsAttempted,
            snapshotsInserted,
            snapshotsSkipped,
            demoOddsInserted,
            oddsWarning,
            catalog);
    }

    private static bool IsCompleted(ProviderMatchDto fixture) =>
        fixture.HomeScore is not null &&
        fixture.AwayScore is not null &&
        fixture.Status.Equals("finished", StringComparison.OrdinalIgnoreCase);

    private static ProviderSeasonDto? FindSeason(
        IEnumerable<ProviderSeasonDto> seasons,
        string labelOrId)
    {
        return seasons.FirstOrDefault(season =>
                string.Equals(season.ExternalId, labelOrId, StringComparison.OrdinalIgnoreCase))
            ?? seasons.FirstOrDefault(season =>
                string.Equals(season.Label, labelOrId, StringComparison.OrdinalIgnoreCase))
            ?? seasons.FirstOrDefault(season =>
                season.Label.Contains(labelOrId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<DemoOddsSeedResult> SeedDemoOddsAsync(
        string? leagueId,
        string? seasonId,
        CancellationToken ct)
    {
        var sportmonks = await _db.Providers.FirstOrDefaultAsync(p => p.Code == ProviderCode, ct)
            .ConfigureAwait(false);

        var bookmaker = await _db.Bookmakers.FirstOrDefaultAsync(b => b.Code == "DEMO-HIGH-ODDS", ct)
            .ConfigureAwait(false);
        if (bookmaker is null)
        {
            bookmaker = new Bookmaker
            {
                Id = Guid.NewGuid(),
                Name = "Demo High Odds Lab (synthetic)",
                Code = "DEMO-HIGH-ODDS",
                IsEnabled = true,
            };
            await _db.Bookmakers.AddAsync(bookmaker, ct).ConfigureAwait(false);
        }

        var market = await _db.Markets.FirstOrDefaultAsync(m => m.Code == "1X2", ct)
            .ConfigureAwait(false);
        if (market is null)
        {
            market = new Market
            {
                Id = Guid.NewGuid(),
                Name = "1X2",
                Code = "1X2",
                Description = "Full-time result",
            };
            await _db.Markets.AddAsync(market, ct).ConfigureAwait(false);
        }

        var query = _db.Matches
            .Where(match =>
                match.HomeScore != null &&
                match.AwayScore != null &&
                (match.Status == MatchStatus.Finished ||
                 match.Status == MatchStatus.SettlementPending ||
                 match.Status == MatchStatus.Analyzed ||
                 match.Status == MatchStatus.Reconciled));

        if (!string.IsNullOrWhiteSpace(seasonId) && sportmonks is not null)
        {
            query =
                from match in query
                join map in _db.ProviderEntityMaps on match.SeasonId equals map.InternalId
                where map.ProviderId == sportmonks.Id && map.EntityType == "Season" && map.ExternalId == seasonId
                select match;
        }

        if (!string.IsNullOrWhiteSpace(leagueId) && sportmonks is not null)
        {
            query =
                from match in query
                join map in _db.ProviderEntityMaps on match.CompetitionId equals map.InternalId
                where map.ProviderId == sportmonks.Id && map.EntityType == "Competition" && map.ExternalId == leagueId
                select match;
        }

        var matches = await query
            .OrderByDescending(match => match.KickoffUtc)
            .Take(2_000)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var inserted = 0;
        foreach (var match in matches)
        {
            var line = await _db.MarketLines.FirstOrDefaultAsync(
                    existing => existing.MatchId == match.Id &&
                        existing.MarketId == market.Id &&
                        existing.Period == "FullTime" &&
                        existing.Line == null,
                    ct)
                .ConfigureAwait(false);

            if (line is null)
            {
                line = new MarketLine
                {
                    Id = Guid.NewGuid(),
                    MatchId = match.Id,
                    MarketId = market.Id,
                    Period = "FullTime",
                };
                await _db.MarketLines.AddAsync(line, ct).ConfigureAwait(false);
            }

            foreach (var selectionName in new[] { "Home", "Draw", "Away" })
            {
                var selection = await _db.Selections.FirstOrDefaultAsync(
                        existing => existing.MarketLineId == line.Id && existing.Name == selectionName,
                        ct)
                    .ConfigureAwait(false);
                if (selection is null)
                {
                    selection = new Selection
                    {
                        Id = Guid.NewGuid(),
                        MarketLineId = line.Id,
                        Name = selectionName,
                    };
                    await _db.Selections.AddAsync(selection, ct).ConfigureAwait(false);
                }

                var closing = DemoClosingOdds(match, selectionName);
                var opening = Math.Max(1.25m, closing * 0.92m);
                inserted += await AddDemoSnapshotAsync(match, bookmaker.Id, line.Id, selection.Id, selectionName, opening, OddsSnapshotKind.Opening, ct)
                    .ConfigureAwait(false);
                inserted += await AddDemoSnapshotAsync(match, bookmaker.Id, line.Id, selection.Id, selectionName, closing, OddsSnapshotKind.Closing, ct)
                    .ConfigureAwait(false);
            }
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new DemoOddsSeedResult(matches.Count, inserted);
    }

    private async Task<int> AddDemoSnapshotAsync(
        Match match,
        Guid bookmakerId,
        Guid marketLineId,
        Guid selectionId,
        string selectionName,
        decimal odds,
        OddsSnapshotKind kind,
        CancellationToken ct)
    {
        var timestamp = kind == OddsSnapshotKind.Opening
            ? match.KickoffUtc.AddDays(-3)
            : match.KickoffUtc.AddMinutes(-10);
        var hash = ComputeHash($"demo|{match.Id}|{selectionName}|{kind}|{odds.ToString(CultureInfo.InvariantCulture)}");

        var exists = await _db.OddsSnapshots.AnyAsync(snapshot => snapshot.PayloadHash == hash, ct)
            .ConfigureAwait(false);
        if (exists)
        {
            return 0;
        }

        await _db.OddsSnapshots.AddAsync(new OddsSnapshot
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            BookmakerId = bookmakerId,
            MarketLineId = marketLineId,
            SelectionId = selectionId,
            DecimalOdds = decimal.Round(odds, 2),
            ImpliedProbability = odds > 1m ? decimal.Round(1m / odds, 6) : 0m,
            IsLive = false,
            Kind = kind,
            Period = "FullTime",
            BookmakerTimestampUtc = timestamp,
            ProviderTimestampUtc = timestamp,
            IngestionTimestampUtc = DateTime.UtcNow,
            PayloadHash = hash,
        }, ct).ConfigureAwait(false);
        return 1;
    }

    private static decimal DemoClosingOdds(Match match, string selectionName)
    {
        // Deterministic pseudo-random draw in [0, 1) derived from the stable
        // match/selection key. Demo data must still show a realistic market:
        // high-priced selections usually lose, otherwise High Odds would report
        // a misleading 100% win rate on synthetic rows.
        var seedBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"demo|{match.Id}|{selectionName}"));
        var draw = (seedBytes[0] * 256 + seedBytes[1]) / 65535.0;
        var outcomeIsHighPriced = draw < 0.75;
        var outcome = outcomeIsHighPriced
            ? (match.HomeScore == match.AwayScore
                ? "Draw"
                : match.HomeScore > match.AwayScore ? "Home" : "Away")
            : (match.HomeScore == match.AwayScore
                ? (seedBytes[2] % 2 == 0 ? "Home" : "Away")
                : "Draw");
        var seed = seedBytes[3];
        var closing = selectionName == outcome
            ? selectionName == "Draw"
                ? 5.8m + seed / 100m
                : 6.2m + seed / 40m
            : selectionName == "Draw"
                ? 3.2m + seed / 90m
                : 2.0m + seed / 120m;

        // Keep the closing price at or above the demo filter floor so the new
        // synthetic rows remain visible from High Odds Intelligence.
        return Math.Max(closing, 6.0m);
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}

public sealed record SportmonksSeasonImportRequest(string LeagueId, string SeasonId, bool SeedDemoOdds = false);

public sealed record SportmonksBulkSeasonImportRequest(
    string SeasonLabelOrId,
    bool SeedDemoOdds = true,
    int? MaxLeagues = null,
    int? SkipLeagues = null);

public sealed record SportmonksBulkSeasonSkip(string LeagueId, string LeagueName, string Reason);

public sealed record SportmonksBulkSeasonImportResult(
    string SeasonLabelOrId,
    int LeaguesImported,
    int LeaguesSkipped,
    int FixturesUpserted,
    int OddsSnapshotsInserted,
    int DemoOddsSnapshotsInserted,
    IReadOnlyList<SportmonksSeasonImportResult> Results,
    IReadOnlyList<SportmonksBulkSeasonSkip> Skipped);

public sealed record DemoOddsSeedRequest(string? LeagueId = null, string? SeasonId = null);

public sealed record DemoOddsSeedResult(int MatchesProcessed, int SnapshotsInserted);

public sealed record SportmonksSeasonImportResult(
    string LeagueId,
    string SeasonId,
    int FixturesUpserted,
    int CompletedFixturesWithOddsRequested,
    int OddsRequestsAttempted,
    int OddsSnapshotsInserted,
    int DuplicateOddsSnapshotsSkipped,
    int DemoOddsSnapshotsInserted,
    string? OddsWarning,
    CatalogIngestionResult Catalog);
