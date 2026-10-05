using CalcioAnalytic.Analytics.MarketIntelligence;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Settlement;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Controllers;

[ApiController]
[Route("api/v1/analytics/daily-report")]
public sealed class DailyMarketReportController : ControllerBase
{
    private readonly CalcioAnalyticDbContext _db;

    public DailyMarketReportController(CalcioAnalyticDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(DailyMarketReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DailyMarketReportDto>> Get(
        [FromQuery] DateTime? dateUtc = null,
        CancellationToken ct = default)
    {
        var date = (dateUtc ?? DateTime.UtcNow).Date;
        var fromUtc = date;
        var to = date.AddDays(1);

        var rows = await (
            from snapshot in _db.OddsSnapshots.AsNoTracking()
            join line in _db.MarketLines.AsNoTracking() on snapshot.MarketLineId equals line.Id
            join market in _db.Markets.AsNoTracking() on line.MarketId equals market.Id
            join selection in _db.Selections.AsNoTracking() on snapshot.SelectionId equals selection.Id
            join match in _db.Matches.AsNoTracking() on snapshot.MatchId equals match.Id
            join settlement in _db.MarketSettlements.AsNoTracking()
                on new { snapshot.MatchId, snapshot.MarketLineId, snapshot.SelectionId }
                equals new { settlement.MatchId, settlement.MarketLineId, settlement.SelectionId }
            where !snapshot.IsLive
                && snapshot.ProviderTimestampUtc < match.KickoffUtc
                && match.KickoffUtc >= fromUtc && match.KickoffUtc < to
                && market.Code == "1X2"
                && (line.Period == null || line.Period == "FullTime")
            select new RawRow(
                match.Id,
                match.KickoffUtc,
                match.Competition!.Name,
                match.HomeTeam!.Name,
                match.AwayTeam!.Name,
                line.Id,
                selection.Name,
                snapshot.DecimalOdds,
                snapshot.ProviderTimestampUtc,
                settlement.Status))
            .ToListAsync(ct);

        var settled = rows
            .GroupBy(x => new { x.MatchId, x.MarketLineId, x.SelectionName })
            .Select(g => g.OrderByDescending(x => x.Timestamp).First())
            .GroupBy(x => new { x.MatchId, x.MarketLineId })
            .Select(g =>
            {
                var favorite = g.OrderBy(x => x.Odds).First();
                var winner = g.FirstOrDefault(x => x.Status == SettlementStatus.Won);
                var hit = favorite.Status == SettlementStatus.Won;
                return new DailyMarketReportRowDto(
                    favorite.MatchId,
                    favorite.KickoffUtc,
                    favorite.CompetitionName,
                    favorite.HomeTeamName,
                    favorite.AwayTeamName,
                    favorite.SelectionName,
                    favorite.Odds,
                    winner?.SelectionName,
                    winner?.Odds,
                    hit ? "HIT" : favorite.Status == SettlementStatus.Lost ? "MISS" : "UNKNOWN",
                    !hit && winner?.Odds >= 5m);
            })
            .Where(x => x.Classification != "UNKNOWN")
            .OrderByDescending(x => x.IsUpset)
            .ThenByDescending(x => x.WinnerOdds ?? 0m)
            .ToList();

        var upcoming = await _db.Matches.AsNoTracking()
            .CountAsync(m => m.KickoffUtc >= to && m.KickoffUtc < to.AddDays(1)
                && (m.Status == MatchStatus.Scheduled || m.Status == MatchStatus.PreMatch), ct);

        var hits = settled.Count(x => x.Classification == "HIT");
        var failures = settled.Count(x => x.Classification == "MISS");
        var decided = hits + failures;
        var profit = settled.Sum(x => x.Classification == "HIT" ? x.FavoriteOdds - 1m : -1m);

        return Ok(new DailyMarketReportDto(
            date,
            settled.Count,
            hits,
            failures,
            settled.Count(x => x.IsUpset),
            decided == 0 ? null : decimal.Round(failures * 100m / decided, 2),
            settled.Count == 0 ? null : decimal.Round(profit / settled.Count * 100m, 2),
            upcoming,
            settled.Where(x => x.IsUpset).Take(10).ToList()));
    }

    private sealed record RawRow(
        Guid MatchId, DateTime KickoffUtc, string CompetitionName, string HomeTeamName,
        string AwayTeamName, Guid MarketLineId, string SelectionName, decimal Odds,
        DateTime Timestamp, SettlementStatus Status);
}
