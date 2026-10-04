using System.Globalization;
using System.Text;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Controllers;

[ApiController]
[Route("api/v1/analytics/high-odds")]
public sealed class HighOddsController : ControllerBase
{
    private static readonly string[] SupportedSelections = ["Home", "Draw", "Away"];

    private readonly CalcioAnalyticDbContext _db;

    public HighOddsController(CalcioAnalyticDbContext db) => _db = db;

    /// <summary>
    /// Historical 1X2 high-odds intelligence. Only pre-kickoff snapshots are used.
    /// The displayed price is the best observed closing/pre-kickoff price across
    /// the selected bookmakers, while the opening price is retained for movement.
    /// ROI is a flat one-unit historical simulation and is not a prediction.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HighOddsAnalyticsResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HighOddsAnalyticsResponseDto>> Get(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] decimal minOdds = 6m,
        [FromQuery] Guid? competitionId = null,
        [FromQuery] Guid? bookmakerId = null,
        [FromQuery] string? result = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (minOdds < 1.01m || minOdds > 1000m)
            return BadRequest(new { message = "minOdds must be between 1.01 and 1000." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var to = (toUtc ?? DateTime.UtcNow).ToUniversalTime();
        var from = (fromUtc ?? to.AddDays(-30)).ToUniversalTime();
        if (from >= to)
            return BadRequest(new { message = "fromUtc must be earlier than toUtc." });

        var normalizedResult = NormalizeSelection(result);
        if (result is not null && normalizedResult is null)
            return BadRequest(new { message = "result must be Home, Draw, or Away." });

        var matchRows = await _db.Matches.AsNoTracking()
            .Where(m =>
                (m.Status == MatchStatus.Finished ||
                 m.Status == MatchStatus.SettlementPending ||
                 m.Status == MatchStatus.Analyzed ||
                 m.Status == MatchStatus.Reconciled) &&
                m.KickoffUtc >= from &&
                m.KickoffUtc <= to &&
                (competitionId == null || m.CompetitionId == competitionId.Value))
            .Select(m => new MatchRow(
                m.Id,
                m.KickoffUtc,
                m.CompetitionId,
                m.Competition!.Name,
                m.HomeTeamId,
                m.HomeTeam!.Name,
                m.AwayTeamId,
                m.AwayTeam!.Name,
                m.HomeScore,
                m.AwayScore))
            .ToListAsync(ct);

        if (matchRows.Count == 0)
        {
            return Ok(BuildResponse(
                from, to, minOdds, competitionId, bookmakerId, normalizedResult, page, pageSize,
                [], [], [], [], 0));
        }

        var matchIds = matchRows.Select(m => m.Id).ToArray();

        var raw = await (
            from s in _db.OddsSnapshots.AsNoTracking()
            join l in _db.MarketLines.AsNoTracking() on s.MarketLineId equals l.Id
            join market in _db.Markets.AsNoTracking() on l.MarketId equals market.Id
            join selection in _db.Selections.AsNoTracking() on s.SelectionId equals selection.Id
            join bookmaker in _db.Bookmakers.AsNoTracking() on s.BookmakerId equals bookmaker.Id
            join match in _db.Matches.AsNoTracking() on s.MatchId equals match.Id
            where matchIds.Contains(s.MatchId)
                && !s.IsLive
                && s.ProviderTimestampUtc <= match.KickoffUtc
                && s.DecimalOdds >= 1.01m
                && (market.Code == "1X2" ||
                    market.Name == "1X2" ||
                    market.Name.Contains("1X2"))
                && (selection.Name == "Home" ||
                    selection.Name == "Draw" ||
                    selection.Name == "Away")
                && (l.Period == null || l.Period == "FullTime")
                && (bookmakerId == null || s.BookmakerId == bookmakerId.Value)
            select new RawSnapshot(
                s.MatchId,
                s.BookmakerId,
                bookmaker.Name,
                l.Id,
                selection.Name,
                s.DecimalOdds,
                s.Kind,
                s.ProviderTimestampUtc))
            .ToListAsync(ct);

        var matchById = matchRows.ToDictionary(x => x.Id);

        var bookmakerSelections = raw
            .GroupBy(x => new { x.MatchId, x.BookmakerId, x.MarketLineId, x.Selection })
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.Timestamp).ToList();
                var opening = ordered.FirstOrDefault(x => x.Kind == OddsSnapshotKind.Opening) ?? ordered[0];
                var closing = ordered.LastOrDefault(x => x.Kind == OddsSnapshotKind.Closing) ?? ordered[^1];

                return new PriceCandidate(
                    group.Key.MatchId,
                    group.Key.BookmakerId,
                    group.First().BookmakerName,
                    group.Key.MarketLineId,
                    group.Key.Selection,
                    opening.Odds,
                    closing.Odds,
                    closing.Timestamp);
            })
            .ToList();

        // Select the best real closing/pre-kickoff price for each match + outcome.
        // We never mix bookmakers inside one movement series.
        var candidates = bookmakerSelections
            .GroupBy(x => new { x.MatchId, x.Selection })
            .Select(group => group
                .OrderByDescending(x => x.ClosingOdds)
                .ThenBy(x => x.BookmakerName)
                .First())
            .Where(x => x.ClosingOdds >= minOdds)
            .Select(x =>
            {
                var match = matchById[x.MatchId];
                var outcome = ResolveOutcome(match.HomeScore, match.AwayScore);
                var won = outcome == x.Selection;
                var profit = won ? x.ClosingOdds - 1m : -1m;
                var movement = x.OpeningOdds > 0m
                    ? (x.ClosingOdds - x.OpeningOdds) / x.OpeningOdds * 100m
                    : (decimal?)null;

                return new HighOddsRow(
                    match.Id,
                    match.KickoffUtc,
                    match.CompetitionId,
                    match.CompetitionName,
                    match.HomeTeamId,
                    match.HomeTeamName,
                    match.AwayTeamId,
                    match.AwayTeamName,
                    x.Selection,
                    outcome,
                    won,
                    x.ClosingOdds,
                    x.BookmakerId,
                    x.BookmakerName,
                    x.OpeningOdds,
                    x.ClosingOdds,
                    movement,
                    profit);
            })
            .Where(x => normalizedResult is null || x.Result == normalizedResult)
            .OrderByDescending(x => x.KickoffUtc)
            .ThenByDescending(x => x.Odds)
            .ToList();

        var ranges = BuildRanges(candidates);
        var selections = BuildGroups(candidates, x => x.Selection);
        var bookmakers = BuildGroups(candidates, x => x.BookmakerName);

        var total = candidates.Count;
        var paged = candidates
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToList();

        return Ok(BuildResponse(
            from, to, minOdds, competitionId, bookmakerId, normalizedResult, page, pageSize,
            candidates, ranges, selections, bookmakers, paged, total));
    }

    /// <summary>Returns competition and bookmaker filter values for the selected period.</summary>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(HighOddsCatalogDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<HighOddsCatalogDto>> Catalog(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct = default)
    {
        var to = (toUtc ?? DateTime.UtcNow).ToUniversalTime();
        var from = (fromUtc ?? to.AddDays(-365)).ToUniversalTime();

        var competitions = await _db.Matches.AsNoTracking()
            .Where(m => m.KickoffUtc >= from && m.KickoffUtc <= to)
            .Join(_db.Competitions.AsNoTracking(), m => m.CompetitionId, c => c.Id,
                (m, c) => new { c.Id, c.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .Select(x => new HighOddsCatalogItemDto(x.Id, x.Name))
            .ToListAsync(ct);

        var bookmakers = await _db.OddsSnapshots.AsNoTracking()
            .Where(s => s.ProviderTimestampUtc >= from && s.ProviderTimestampUtc <= to && !s.IsLive)
            .Join(_db.Bookmakers.AsNoTracking(), s => s.BookmakerId, b => b.Id,
                (s, b) => new { b.Id, b.Name })
            .Distinct()
            .OrderBy(x => x.Name)
            .Select(x => new HighOddsCatalogItemDto(x.Id, x.Name))
            .ToListAsync(ct);

        return Ok(new HighOddsCatalogDto(competitions, bookmakers));
    }

    /// <summary>Exports the same historical high-odds query as CSV for Excel.</summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] decimal minOdds = 6m,
        [FromQuery] Guid? bookmakerId = null,
        [FromQuery] string? result = null,
        CancellationToken ct = default)
    {
        var response = await Get(fromUtc, toUtc, minOdds, null, bookmakerId, result, 1, 200, ct);
        if (response.Result is not OkObjectResult ok || ok.Value is not HighOddsAnalyticsResponseDto data)
            return response.Result ?? BadRequest();

        var sb = new StringBuilder();
        sb.AppendLine("Kickoff UTC,Competition,Home Team,Away Team,Selection,Odds,Bookmaker,Result,Won,Profit Units,Opening Odds,Closing Odds,Movement %");

        foreach (var row in data.Results)
        {
            sb.AppendLine(string.Join(",",
                Csv(row.KickoffUtc.ToString("O", CultureInfo.InvariantCulture)),
                Csv(row.CompetitionName),
                Csv(row.HomeTeamName),
                Csv(row.AwayTeamName),
                Csv(row.Selection),
                row.Odds.ToString("0.00", CultureInfo.InvariantCulture),
                Csv(row.BookmakerName),
                Csv(row.Result),
                row.Won ? "true" : "false",
                row.ProfitUnits.ToString("0.00", CultureInfo.InvariantCulture),
                row.OpeningOdds?.ToString("0.00", CultureInfo.InvariantCulture) ?? "",
                row.ClosingOdds?.ToString("0.00", CultureInfo.InvariantCulture) ?? "",
                row.MovementPercentage?.ToString("0.00", CultureInfo.InvariantCulture) ?? ""));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv; charset=utf-8", "calcio-high-odds.csv");
    }

    private static HighOddsAnalyticsResponseDto BuildResponse(
        DateTime from,
        DateTime to,
        decimal minOdds,
        Guid? competitionId,
        Guid? bookmakerId,
        string? result,
        int page,
        int pageSize,
        IReadOnlyList<HighOddsRow> candidates,
        IReadOnlyList<HighOddsRangeStatsDto> ranges,
        IReadOnlyList<HighOddsGroupStatsDto> selections,
        IReadOnlyList<HighOddsGroupStatsDto> bookmakers,
        IReadOnlyList<HighOddsSelectionDto> results,
        int totalResults)
    {
        var stake = candidates.Count;
        var profit = candidates.Sum(x => x.ProfitUnits);
        var wins = candidates.Count(x => x.Won);
        var ordered = candidates.OrderBy(x => x.KickoffUtc).ThenBy(x => x.Selection).ToList();

        var running = 0m;
        var peak = 0m;
        var maxDrawdown = 0m;
        var currentWin = 0;
        var currentLoss = 0;
        var maxWin = 0;
        var maxLoss = 0;

        foreach (var row in ordered)
        {
            running += row.ProfitUnits;
            peak = Math.Max(peak, running);
            maxDrawdown = Math.Max(maxDrawdown, peak - running);
            if (row.Won)
            {
                currentWin++;
                currentLoss = 0;
                maxWin = Math.Max(maxWin, currentWin);
            }
            else
            {
                currentLoss++;
                currentWin = 0;
                maxLoss = Math.Max(maxLoss, currentLoss);
            }
        }

        var summary = new HighOddsSummaryDto(
            candidates.Select(x => x.MatchId).Distinct().Count(),
            stake,
            wins,
            Pct(wins, stake),
            stake == 0 ? 0m : candidates.Average(x => x.Odds),
            stake == 0 ? 0m : candidates.Average(x => 100m / x.Odds),
            stake,
            candidates.Sum(x => x.Won ? x.Odds : 0m),
            profit,
            stake == 0 ? 0m : profit / stake * 100m,
            maxDrawdown,
            maxWin,
            maxLoss);

        return new HighOddsAnalyticsResponseDto(
            new HighOddsQueryDto(from, to, minOdds, competitionId, bookmakerId, result, page, pageSize),
            summary,
            ranges,
            selections,
            bookmakers,
            results,
            totalResults,
            page,
            pageSize);
    }

    private static IReadOnlyList<HighOddsRangeStatsDto> BuildRanges(
        IReadOnlyList<HighOddsRow> rows)
    {
        var definitions = new (string Name, decimal Min, decimal Max)[]
        {
            ("6.00–6.99", 6m, 7m),
            ("7.00–7.99", 7m, 8m),
            ("8.00–9.99", 8m, 10m),
            ("10.00–14.99", 10m, 15m),
            ("15.00–19.99", 15m, 20m),
            ("20.00+", 20m, decimal.MaxValue),
        };

        return definitions.Select(d =>
        {
            var subset = rows.Where(x => x.Odds >= d.Min && x.Odds < d.Max).ToList();
            return new HighOddsRangeStatsDto(
                d.Name,
                subset.Count,
                subset.Count(x => x.Won),
                Pct(subset.Count(x => x.Won), subset.Count),
                subset.Count == 0 ? 0m : subset.Average(x => x.Odds),
                subset.Sum(x => x.ProfitUnits),
                Roi(subset));
        }).ToList();
    }

    private static IReadOnlyList<HighOddsGroupStatsDto> BuildGroups(
        IReadOnlyList<HighOddsRow> rows,
        Func<HighOddsRow, string> key)
    {
        return rows.GroupBy(key)
            .Select(g => new HighOddsGroupStatsDto(
                g.Key,
                g.Count(),
                g.Count(x => x.Won),
                Pct(g.Count(x => x.Won), g.Count()),
                g.Average(x => x.Odds),
                g.Sum(x => x.ProfitUnits),
                Roi(g)))
            .OrderByDescending(x => x.Selections)
            .ToList();
    }

    private static HighOddsSelectionDto ToDto(HighOddsRow x) =>
        new(
            x.MatchId,
            x.KickoffUtc,
            x.CompetitionId,
            x.CompetitionName,
            x.HomeTeamId,
            x.HomeTeamName,
            x.AwayTeamId,
            x.AwayTeamName,
            x.Selection,
            x.Result,
            x.Won,
            x.Odds,
            100m / x.Odds,
            x.BookmakerId,
            x.BookmakerName,
            x.OpeningOdds,
            x.ClosingOdds,
            x.MovementPercentage,
            x.ProfitUnits);

    private static string? NormalizeSelection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return SupportedSelections.FirstOrDefault(x =>
            string.Equals(x, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveOutcome(int? home, int? away)
    {
        if (home is null || away is null) return "Unknown";
        return home > away ? "Home" : home < away ? "Away" : "Draw";
    }

    private static decimal Pct(int numerator, int denominator) =>
        denominator == 0 ? 0m : numerator * 100m / denominator;

    private static decimal Roi(IEnumerable<HighOddsRow> rows)
    {
        var list = rows.ToList();
        return list.Count == 0 ? 0m : list.Sum(x => x.ProfitUnits) / list.Count * 100m;
    }

    private static string Csv(string value) =>
        "\"" + value.Replace("\"", "\"\"") + "\"";

    private sealed record MatchRow(
        Guid Id,
        DateTime KickoffUtc,
        Guid CompetitionId,
        string CompetitionName,
        Guid HomeTeamId,
        string HomeTeamName,
        Guid AwayTeamId,
        string AwayTeamName,
        int? HomeScore,
        int? AwayScore);

    private sealed record RawSnapshot(
        Guid MatchId,
        Guid BookmakerId,
        string BookmakerName,
        Guid MarketLineId,
        string Selection,
        decimal Odds,
        OddsSnapshotKind Kind,
        DateTime Timestamp);

    private sealed record PriceCandidate(
        Guid MatchId,
        Guid BookmakerId,
        string BookmakerName,
        Guid MarketLineId,
        string Selection,
        decimal OpeningOdds,
        decimal ClosingOdds,
        DateTime ClosingTimestamp);

    private sealed record HighOddsRow(
        Guid MatchId,
        DateTime KickoffUtc,
        Guid CompetitionId,
        string CompetitionName,
        Guid HomeTeamId,
        string HomeTeamName,
        Guid AwayTeamId,
        string AwayTeamName,
        string Selection,
        string Result,
        bool Won,
        decimal Odds,
        Guid BookmakerId,
        string BookmakerName,
        decimal? OpeningOdds,
        decimal? ClosingOdds,
        decimal? MovementPercentage,
        decimal ProfitUnits);
}
