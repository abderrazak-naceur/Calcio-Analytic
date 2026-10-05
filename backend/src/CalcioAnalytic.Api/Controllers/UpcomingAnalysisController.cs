using CalcioAnalytic.Analytics.MarketIntelligence;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Settlement;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Controllers;

[ApiController]
[Route("api/v1/analytics/upcoming")]
public sealed class UpcomingAnalysisController : ControllerBase
{
    private readonly CalcioAnalyticDbContext _db;

    public UpcomingAnalysisController(CalcioAnalyticDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(UpcomingAnalysisResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpcomingAnalysisResponseDto>> Get(
        [FromQuery] string window = "today",
        [FromQuery] string marketCode = "1X2",
        [FromQuery] int minimumComparableSamples = 5,
        CancellationToken ct = default)
    {
        var normalizedMarket = marketCode.Trim().ToUpperInvariant();
        if (normalizedMarket != "1X2")
            return BadRequest(new { message = "Upcoming analysis currently supports 1X2." });

        minimumComparableSamples = Math.Clamp(minimumComparableSamples, 1, 500);
        var now = DateTime.UtcNow;
        var (fromUtc, toUtc) = ResolveWindow(window, now);

        if (fromUtc >= toUtc)
            return BadRequest(new { message = "The requested window is invalid." });

        var upcoming = await (
            from match in _db.Matches.AsNoTracking()
            join line in _db.MarketLines.AsNoTracking() on match.Id equals line.MatchId
            join market in _db.Markets.AsNoTracking() on line.MarketId equals market.Id
            join snapshot in _db.OddsSnapshots.AsNoTracking() on line.Id equals snapshot.MarketLineId
            join selection in _db.Selections.AsNoTracking() on snapshot.SelectionId equals selection.Id
            join bookmaker in _db.Bookmakers.AsNoTracking() on snapshot.BookmakerId equals bookmaker.Id
            where match.KickoffUtc >= fromUtc
                && match.KickoffUtc <= toUtc
                && (match.Status == MatchStatus.Scheduled || match.Status == MatchStatus.PreMatch)
                && market.Code == normalizedMarket
                && (line.Period == null || line.Period == "FullTime")
                && !snapshot.IsLive
                && snapshot.ProviderTimestampUtc < match.KickoffUtc
            select new UpcomingRawRow(
                match.Id,
                match.KickoffUtc,
                match.Competition!.Name,
                match.HomeTeam!.Name,
                match.AwayTeam!.Name,
                bookmaker.Id,
                bookmaker.Name,
                market.Code,
                line.Id,
                snapshot.SelectionId,
                selection.Name,
                snapshot.DecimalOdds,
                snapshot.ProviderTimestampUtc))
            .ToListAsync(ct);

        var latest = upcoming
            .GroupBy(x => new { x.MatchId, x.BookmakerId, x.MarketLineId, x.SelectionId })
            .Select(g => g.OrderByDescending(x => x.Timestamp).First())
            .GroupBy(x => new { x.MatchId, x.BookmakerId, x.MarketLineId })
            .Select(g =>
            {
                var favorite = g.OrderBy(x => x.DecimalOdds).First();
                return new UpcomingMarketRow(favorite, g.ToList());
            })
            .Where(x => x.Favorite.DecimalOdds > 1m)
            .OrderBy(x => x.Favorite.KickoffUtc)
            .ThenBy(x => x.Favorite.DecimalOdds)
            .ToList();

        var historical = await LoadHistorical(ct);
        var results = latest.Select(row =>
        {
            var comparable = historical
                .Where(x => x.FavoriteOdds >= OddsBucket.Min(row.Favorite.DecimalOdds)
                    && x.FavoriteOdds <= OddsBucket.Max(row.Favorite.DecimalOdds))
                .ToList();
            var stats = BuildComparableStats(comparable);
            return new UpcomingAnalysisRowDto(
                row.Favorite.MatchId,
                row.Favorite.KickoffUtc,
                row.Favorite.CompetitionName,
                row.Favorite.HomeTeamName,
                row.Favorite.AwayTeamName,
                row.Favorite.BookmakerId,
                row.Favorite.BookmakerName,
                row.Favorite.MarketCode,
                row.Favorite.SelectionName,
                row.Favorite.DecimalOdds,
                comparable.Count,
                stats);
        }).ToList();

        return Ok(new UpcomingAnalysisResponseDto(
            new UpcomingAnalysisQueryDto(window.Trim().ToLowerInvariant(), fromUtc, toUtc, normalizedMarket, minimumComparableSamples),
            results.Where(x => x.ComparableSampleSize >= minimumComparableSamples).ToList(),
            results.Count));
    }

    private async Task<List<HistoricalRow>> LoadHistorical(CancellationToken ct)
    {
        var rows = await (
            from snapshot in _db.OddsSnapshots.AsNoTracking()
            join line in _db.MarketLines.AsNoTracking() on snapshot.MarketLineId equals line.Id
            join market in _db.Markets.AsNoTracking() on line.MarketId equals market.Id
            join selection in _db.Selections.AsNoTracking() on snapshot.SelectionId equals selection.Id
            join match in _db.Matches.AsNoTracking() on snapshot.MatchId equals match.Id
            join settlement in _db.MarketSettlements.AsNoTracking()
                on new { snapshot.MatchId, snapshot.MarketLineId, snapshot.SelectionId }
                equals new { settlement.MatchId, settlement.MarketLineId, settlement.SelectionId }
            where market.Code == "1X2"
                && !snapshot.IsLive
                && snapshot.ProviderTimestampUtc < match.KickoffUtc
                && (match.Status == MatchStatus.Finished || match.Status == MatchStatus.SettlementPending || match.Status == MatchStatus.Analyzed || match.Status == MatchStatus.Reconciled)
                && (line.Period == null || line.Period == "FullTime")
            select new HistoricalRaw(
                snapshot.MatchId,
                line.Id,
                selection.Name,
                snapshot.DecimalOdds,
                snapshot.ProviderTimestampUtc,
                settlement.Status))
            .ToListAsync(ct);

        return rows
            .GroupBy(x => new { x.MatchId, x.MarketLineId, x.SelectionName })
            .Select(g => g.OrderByDescending(x => x.Timestamp).First())
            .GroupBy(x => new { x.MatchId, x.MarketLineId })
            .Select(g =>
            {
                var favorite = g.OrderBy(x => x.Odds).First();
                var winner = g.FirstOrDefault(x => x.SettlementStatus == SettlementStatus.Won);
                var won = favorite.SettlementStatus == SettlementStatus.Won;
                return new HistoricalRow(favorite.Odds, won, winner?.Odds);
            })
            .Where(x => x.FavoriteOdds > 1m)
            .ToList();
    }

    private static UpcomingComparableStatsDto BuildComparableStats(IReadOnlyList<HistoricalRow> rows)
    {
        if (rows.Count == 0)
            return new(0, 0, 0, 0, null, null, null, null, null, null, null);

        var stats = MarketOutcomeStatisticsCalculator.Calculate(rows.Select(x => (x.FavoriteOdds, (bool?)x.Won)));
        var upsets = rows.Count(x => !x.Won && x.WinnerOdds.HasValue && x.WinnerOdds.Value >= 5m);
        var averageFavorite = rows.Average(x => x.FavoriteOdds);
        var winners = rows.Where(x => x.WinnerOdds.HasValue).Select(x => x.WinnerOdds!.Value).ToList();

        return new(
            rows.Count,
            stats.Wins,
            stats.Losses,
            upsets,
            stats.ActualProbabilityPercentage,
            stats.Losses * 100m / rows.Count,
            decimal.Round(averageFavorite, 2),
            winners.Count == 0 ? null : decimal.Round(winners.Average(), 2),
            stats.ImpliedProbabilityPercentage,
            stats.ActualProbabilityPercentage,
            stats.RoiPercentage);
    }

    private static (DateTime From, DateTime To) ResolveWindow(string value, DateTime now)
    {
        var key = value.Trim().ToLowerInvariant();
        var today = now.Date;
        return key switch
        {
            "today" => (today, today.AddDays(1).AddTicks(-1)),
            "tomorrow" => (today.AddDays(1), today.AddDays(2).AddTicks(-1)),
            "2d" or "2" => (now, now.AddDays(2)),
            "7d" or "7" => (now, now.AddDays(7)),
            "14d" or "14" => (now, now.AddDays(14)),
            "30d" or "30" => (now, now.AddDays(30)),
            _ => throw new ArgumentException("window must be today, tomorrow, 2d, 7d, 14d, or 30d."),
        };
    }

    private readonly record struct UpcomingRawRow(
        Guid MatchId, DateTime KickoffUtc, string CompetitionName, string HomeTeamName,
        string AwayTeamName, Guid BookmakerId, string BookmakerName, string MarketCode,
        Guid MarketLineId, Guid SelectionId, string SelectionName, decimal DecimalOdds, DateTime Timestamp);

    private sealed record UpcomingMarketRow(UpcomingRawRow Favorite, IReadOnlyList<UpcomingRawRow> Selections);
    private sealed record HistoricalRaw(Guid MatchId, Guid MarketLineId, string SelectionName, decimal Odds, DateTime Timestamp, SettlementStatus SettlementStatus);
    private sealed record HistoricalRow(decimal FavoriteOdds, bool Won, decimal? WinnerOdds);

    private static class OddsBucket
    {
        public static decimal Min(decimal odds) => odds <= 1.20m ? 1.01m : odds <= 1.40m ? 1.21m : odds <= 1.60m ? 1.41m : odds <= 1.80m ? 1.61m : odds <= 2m ? 1.81m : odds <= 2.50m ? 2.01m : odds <= 3m ? 2.51m : 3.01m;
        public static decimal Max(decimal odds) => odds <= 1.20m ? 1.20m : odds <= 1.40m ? 1.40m : odds <= 1.60m ? 1.60m : odds <= 1.80m ? 1.80m : odds <= 2m ? 2m : odds <= 2.50m ? 2.50m : odds <= 3m ? 3m : 1000m;
    }
}
