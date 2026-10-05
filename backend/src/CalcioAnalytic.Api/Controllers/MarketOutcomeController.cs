using CalcioAnalytic.Analytics.MarketIntelligence;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Domain.Settlement;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Controllers;

[ApiController]
[Route("api/v1/analytics/market-outcomes")]
public sealed class MarketOutcomeController : ControllerBase
{
    private static readonly string[] SupportedMarkets = ["1X2", "OU", "BTTS"];
    private readonly CalcioAnalyticDbContext _db;

    public MarketOutcomeController(CalcioAnalyticDbContext db) => _db = db;

    /// <summary>
    /// Compares pre-kickoff market prices with settled reality. Each result is
    /// calculated per bookmaker and market line so the platform never creates a
    /// synthetic market by mixing bookmakers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MarketOutcomeAnalyticsResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MarketOutcomeAnalyticsResponseDto>> Get(
        [FromQuery] Guid? matchId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string marketCode = "1X2",
        [FromQuery] decimal? minFavoriteOdds = null,
        [FromQuery] decimal? maxFavoriteOdds = null,
        [FromQuery] decimal upsetThreshold = 5m,
        [FromQuery] Guid? bookmakerId = null,
        [FromQuery] string? classification = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var normalizedMarket = marketCode.Trim().ToUpperInvariant();
        if (!SupportedMarkets.Contains(normalizedMarket, StringComparer.Ordinal))
            return BadRequest(new { message = "marketCode must be 1X2, OU, or BTTS." });

        if (upsetThreshold <= 1m || upsetThreshold > 1000m)
            return BadRequest(new { message = "upsetThreshold must be between 1.01 and 1000." });

        if (minFavoriteOdds is <= 1m || maxFavoriteOdds is <= 1m)
            return BadRequest(new { message = "Favorite odds filters must be greater than 1." });

        if (minFavoriteOdds.HasValue && maxFavoriteOdds.HasValue && minFavoriteOdds > maxFavoriteOdds)
            return BadRequest(new { message = "minFavoriteOdds must not exceed maxFavoriteOdds." });

        var normalizedClassification = string.IsNullOrWhiteSpace(classification)
            ? null
            : classification.Trim().ToUpperInvariant();

        if (normalizedClassification is not null &&
            !new[] { "HIT", "MISS", "UNKNOWN", "UPSET" }.Contains(normalizedClassification, StringComparer.Ordinal))
            return BadRequest(new { message = "classification must be HIT, MISS, UPSET, or UNKNOWN." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var to = matchId.HasValue
            ? (toUtc ?? DateTime.MaxValue).ToUniversalTime()
            : (toUtc ?? DateTime.UtcNow).ToUniversalTime();
        var from = matchId.HasValue
            ? (fromUtc ?? DateTime.MinValue).ToUniversalTime()
            : (fromUtc ?? to.AddDays(-30)).ToUniversalTime();
        if (from >= to)
            return BadRequest(new { message = "fromUtc must be earlier than toUtc." });

        var rows = await (
            from snapshot in _db.OddsSnapshots.AsNoTracking()
            join line in _db.MarketLines.AsNoTracking() on snapshot.MarketLineId equals line.Id
            join market in _db.Markets.AsNoTracking() on line.MarketId equals market.Id
            join selection in _db.Selections.AsNoTracking() on snapshot.SelectionId equals selection.Id
            join bookmaker in _db.Bookmakers.AsNoTracking() on snapshot.BookmakerId equals bookmaker.Id
            join match in _db.Matches.AsNoTracking() on snapshot.MatchId equals match.Id
            join settlement in _db.MarketSettlements.AsNoTracking()
                on new { snapshot.MatchId, snapshot.MarketLineId, snapshot.SelectionId }
                equals new { settlement.MatchId, settlement.MarketLineId, settlement.SelectionId }
            where snapshot.IsLive == false
                && snapshot.ProviderTimestampUtc < match.KickoffUtc
                && match.KickoffUtc >= from
                && match.KickoffUtc <= to
                && (matchId == null || match.Id == matchId.Value)
                && (match.Status == MatchStatus.Finished ||
                    match.Status == MatchStatus.SettlementPending ||
                    match.Status == MatchStatus.Analyzed ||
                    match.Status == MatchStatus.Reconciled)
                && market.Code == normalizedMarket
                && (line.Period == null || line.Period == "FullTime")
                && (bookmakerId == null || snapshot.BookmakerId == bookmakerId.Value)
            select new RawMarketOutcomeRow(
                snapshot.MatchId,
                match.KickoffUtc,
                match.CompetitionId,
                match.Competition!.Name,
                match.HomeTeamId,
                match.HomeTeam!.Name,
                match.AwayTeamId,
                match.AwayTeam!.Name,
                snapshot.BookmakerId,
                bookmaker.Name,
                line.Id,
                market.Code,
                line.Line,
                selection.Name,
                snapshot.DecimalOdds,
                snapshot.ProviderTimestampUtc,
                settlement.Status))
            .ToListAsync(ct);

        var latest = rows
            .GroupBy(x => new { x.MatchId, x.BookmakerId, x.MarketLineId, x.SelectionName })
            .Select(g => g.OrderByDescending(x => x.Timestamp).First())
            .GroupBy(x => new { x.MatchId, x.BookmakerId, x.MarketLineId })
            .Select(group =>
            {
                var outcome = MarketOutcomeAnalyzer.Analyze(
                    group.Select(x => new MarketOutcomeSelection(
                        x.SelectionName,
                        x.DecimalOdds,
                        x.SettlementStatus)),
                    upsetThreshold);

                var first = group.First();
                return new MarketOutcomeRowDto(
                    first.MatchId,
                    first.KickoffUtc,
                    first.CompetitionId,
                    first.CompetitionName,
                    first.HomeTeamId,
                    first.HomeTeamName,
                    first.AwayTeamId,
                    first.AwayTeamName,
                    first.BookmakerId,
                    first.BookmakerName,
                    first.MarketLineId,
                    first.MarketCode,
                    first.Line,
                    outcome.FavoriteSelectionName ?? "Unknown",
                    outcome.FavoriteOdds ?? 0m,
                    outcome.FavoriteStatus.ToString(),
                    outcome.Classification.ToString().ToUpperInvariant(),
                    outcome.IsUpset,
                    outcome.WinningSelectionName,
                    outcome.WinningOdds);
            })
            .Where(x => x.FavoriteOdds > 0m)
            .Where(x => !minFavoriteOdds.HasValue || x.FavoriteOdds >= minFavoriteOdds.Value)
            .Where(x => !maxFavoriteOdds.HasValue || x.FavoriteOdds <= maxFavoriteOdds.Value)
            .Where(x => normalizedClassification is null
                || x.Classification == normalizedClassification
                || (normalizedClassification == "UPSET" && x.IsUpset))
            .OrderByDescending(x => x.KickoffUtc)
            .ThenByDescending(x => x.FavoriteOdds)
            .ToList();

        var total = latest.Count;
        var hitCount = latest.Count(x => x.Classification == "HIT");
        var missCount = latest.Count(x => x.Classification == "MISS");
        var upsetCount = latest.Count(x => x.IsUpset);
        var unknownCount = latest.Count(x => x.Classification == "UNKNOWN");
        var decided = hitCount + missCount;
        var failureRate = decided == 0 ? null : decimal.Round(missCount * 100m / decided, 2);

        var paged = latest.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var query = new MarketOutcomeQueryDto(
            matchId,
            from,
            to,
            normalizedMarket,
            minFavoriteOdds,
            maxFavoriteOdds,
            upsetThreshold,
            bookmakerId,
            normalizedClassification,
            page,
            pageSize);

        var thresholdStats = new[] { 5m, 6m, 7m, 8m, 10m }
            .Select(threshold =>
            {
                var winnerCount = latest.Count(x =>
                    x.Classification == "MISS" &&
                    x.WinnerOdds.HasValue &&
                    x.WinnerOdds.Value >= threshold);
                return new MarketOutcomeThresholdStatDto(
                    threshold,
                    winnerCount,
                    missCount == 0 ? null : decimal.Round(winnerCount * 100m / missCount, 2));
            })
            .ToList();

        var favoriteOddsRanges = BuildFavoriteOddsRanges(latest);

        var summary = new MarketOutcomeSummaryDto(
            total,
            hitCount,
            missCount,
            upsetCount,
            unknownCount,
            failureRate,
            thresholdStats,
            favoriteOddsRanges);

        return Ok(new MarketOutcomeAnalyticsResponseDto(
            query,
            summary,
            paged,
            total,
            page,
            pageSize));
    }

    private static IReadOnlyList<MarketFailureOddsRangeDto> BuildFavoriteOddsRanges(
        IReadOnlyList<MarketOutcomeRowDto> rows)
    {
        var definitions = new (string Name, decimal Min, decimal? Max)[]
        {
            ("1.01–1.20", 1.01m, 1.20m),
            ("1.21–1.40", 1.21m, 1.40m),
            ("1.41–1.60", 1.41m, 1.60m),
            ("1.61–1.80", 1.61m, 1.80m),
            ("1.81–2.00", 1.81m, 2.00m),
            ("2.01–2.50", 2.01m, 2.50m),
            ("2.51–3.00", 2.51m, 3.00m),
            ("3.01+", 3.01m, null),
        };

        return definitions.Select(definition =>
        {
            var bucket = rows.Where(row =>
                row.FavoriteOdds >= definition.Min &&
                (!definition.Max.HasValue || row.FavoriteOdds <= definition.Max.Value));
            var list = bucket.ToList();
            var hits = list.Count(row => row.Classification == "HIT");
            var misses = list.Count(row => row.Classification == "MISS");
            var upsets = list.Count(row => row.IsUpset);
            var decided = hits + misses;

            return new MarketFailureOddsRangeDto(
                definition.Name,
                list.Count,
                hits,
                misses,
                upsets,
                decided == 0 ? null : decimal.Round(misses * 100m / decided, 2));
        }).ToList();
    }

    private sealed record RawMarketOutcomeRow(
        Guid MatchId,
        DateTime KickoffUtc,
        Guid CompetitionId,
        string CompetitionName,
        Guid HomeTeamId,
        string HomeTeamName,
        Guid AwayTeamId,
        string AwayTeamName,
        Guid BookmakerId,
        string BookmakerName,
        Guid MarketLineId,
        string MarketCode,
        decimal? Line,
        string SelectionName,
        decimal DecimalOdds,
        DateTime Timestamp,
        SettlementStatus SettlementStatus);
}
