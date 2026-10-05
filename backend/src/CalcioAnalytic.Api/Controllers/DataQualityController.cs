using CalcioAnalytic.Analytics.DataQuality;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Controllers;

/// <summary>
/// Read-only data-quality endpoints. Loads a match together with efficient
/// per-relation aggregate counts (no full table loads) and delegates the actual
/// evaluation to the pure <see cref="IDataQualityEngine"/>. All reads use
/// <c>AsNoTracking</c> and run asynchronously.
/// </summary>
[ApiController]
[Route("api/v1/dataquality")]
public sealed class DataQualityController : ControllerBase
{
    private readonly CalcioAnalyticDbContext _db;
    private readonly IDataQualityEngine _engine;

    public DataQualityController(CalcioAnalyticDbContext db, IDataQualityEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    /// <summary>
    /// Evaluates the data-quality of a single match. Loads the match and computes
    /// the related counts with server-side aggregate queries: <c>CountAsync</c>
    /// per related table filtered by <c>MatchId</c> (selections are counted via
    /// their owning market lines), a duplicate-hash flag derived from comparing
    /// the snapshot count against the count of distinct payload hashes, and the
    /// min/max decimal odds via null-safe <c>MinAsync</c>/<c>MaxAsync</c>. The
    /// assembled <see cref="MatchQualityInput"/> is passed to the pure engine.
    /// Returns 404 when the match does not exist.
    /// </summary>
    /// <param name="id">The match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("matches/{id:guid}")]
    [ProducesResponseType(typeof(DataQualityReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DataQualityReportDto>> GetMatchQuality(Guid id, CancellationToken ct)
    {
        var match = await _db.Matches.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (match is null)
        {
            return NotFound();
        }

        var oddsSnapshotCount = await _db.OddsSnapshots.AsNoTracking()
            .CountAsync(o => o.MatchId == id, ct);

        var marketLineCount = await _db.MarketLines.AsNoTracking()
            .CountAsync(l => l.MatchId == id, ct);

        // Selections are keyed by MarketLineId, so count those whose owning line
        // belongs to this match via a correlated subquery (no table load).
        var selectionCount = await _db.Selections.AsNoTracking()
            .CountAsync(s => _db.MarketLines.Any(l => l.MatchId == id && l.Id == s.MarketLineId), ct);

        var statisticCount = await _db.MatchStatistics.AsNoTracking()
            .CountAsync(s => s.MatchId == id, ct);

        var eventCount = await _db.MatchEvents.AsNoTracking()
            .CountAsync(e => e.MatchId == id, ct);

        var settlementCount = await _db.MarketSettlements.AsNoTracking()
            .CountAsync(x => x.MatchId == id, ct);

        var analysisCount = await _db.MatchAnalyses.AsNoTracking()
            .CountAsync(a => a.MatchId == id, ct);

        // Duplicate detection: any repeated payload hash means duplicates exist.
        var distinctHashCount = await _db.OddsSnapshots.AsNoTracking()
            .Where(o => o.MatchId == id)
            .Select(o => o.PayloadHash)
            .Distinct()
            .CountAsync(ct);
        var hasDuplicateOddsHash = oddsSnapshotCount > distinctHashCount;

        // Min/Max decimal odds, null-safe when the match has no snapshots. The
        // aggregate is run over a nullable projection so an empty set yields null
        // rather than throwing.
        decimal? minDecimalOdds = null;
        decimal? maxDecimalOdds = null;
        if (oddsSnapshotCount > 0)
        {
            minDecimalOdds = await _db.OddsSnapshots.AsNoTracking()
                .Where(o => o.MatchId == id)
                .MinAsync(o => (decimal?)o.DecimalOdds, ct);

            maxDecimalOdds = await _db.OddsSnapshots.AsNoTracking()
                .Where(o => o.MatchId == id)
                .MaxAsync(o => (decimal?)o.DecimalOdds, ct);
        }

        var input = new MatchQualityInput(
            match,
            oddsSnapshotCount,
            marketLineCount,
            selectionCount,
            statisticCount,
            eventCount,
            settlementCount,
            analysisCount,
            hasDuplicateOddsHash,
            minDecimalOdds,
            maxDecimalOdds);

        var report = _engine.EvaluateMatch(input, DateTime.UtcNow);

        return Ok(DataQualityReportDto.FromReport(report));
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(DataQualitySummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DataQualitySummaryDto>> GetSummary(CancellationToken ct)
    {
        var totalMatches = await _db.Matches.AsNoTracking().CountAsync(ct);
        var finishedMatches = await _db.Matches.AsNoTracking()
            .CountAsync(m => m.Status == Domain.Matches.MatchStatus.Finished
                || m.Status == Domain.Matches.MatchStatus.Analyzed
                || m.Status == Domain.Matches.MatchStatus.Reconciled
                || m.Status == Domain.Matches.MatchStatus.SettlementPending, ct);

        var missingOdds = await _db.Matches.AsNoTracking()
            .Where(m => m.Status != Domain.Matches.MatchStatus.Scheduled
                && !_db.OddsSnapshots.Any(o => o.MatchId == m.Id))
            .CountAsync(ct);

        var missingResults = await _db.Matches.AsNoTracking()
            .CountAsync(m => (m.Status == Domain.Matches.MatchStatus.Finished
                    || m.Status == Domain.Matches.MatchStatus.Analyzed
                    || m.Status == Domain.Matches.MatchStatus.Reconciled)
                && (m.HomeScore == null || m.AwayScore == null), ct);

        var missingSettlements = await _db.Matches.AsNoTracking()
            .CountAsync(m => (m.Status == Domain.Matches.MatchStatus.Finished
                    || m.Status == Domain.Matches.MatchStatus.Analyzed
                    || m.Status == Domain.Matches.MatchStatus.Reconciled)
                && !_db.MarketSettlements.Any(s => s.MatchId == m.Id), ct);

        var duplicateOddsMatches = await _db.OddsSnapshots.AsNoTracking()
            .GroupBy(o => o.MatchId)
            .Where(g => g.Count() > g.Select(x => x.PayloadHash).Distinct().Count())
            .Select(g => g.Key)
            .CountAsync(ct);

        var staleCutoff = DateTime.UtcNow.AddDays(-2);
        var staleScheduled = await _db.Matches.AsNoTracking()
            .CountAsync(m => (m.Status == Domain.Matches.MatchStatus.Scheduled || m.Status == Domain.Matches.MatchStatus.PreMatch)
                && m.KickoffUtc < staleCutoff, ct);

        var oddsSnapshotCount = await _db.OddsSnapshots.AsNoTracking().CountAsync(ct);
        var oddsProviderCount = await _db.OddsSnapshots.AsNoTracking()
            .Select(o => o.ProviderId)
            .Distinct()
            .CountAsync(ct);
        var oddsBookmakerCount = await _db.OddsSnapshots.AsNoTracking()
            .Select(o => o.BookmakerId)
            .Distinct()
            .CountAsync(ct);
        var oddsMarketCount = await (
            from snapshot in _db.OddsSnapshots.AsNoTracking()
            join line in _db.MarketLines.AsNoTracking() on snapshot.MarketLineId equals line.Id
            select line.MarketId)
            .Distinct()
            .CountAsync(ct);

        var issues = missingOdds + missingResults + missingSettlements + duplicateOddsMatches + staleScheduled;
        var denominator = Math.Max(1, finishedMatches + totalMatches);
        var score = decimal.Max(0m, decimal.Round(100m - issues * 100m / denominator, 2));

        return Ok(new DataQualitySummaryDto(
            totalMatches,
            finishedMatches,
            missingOdds,
            missingResults,
            missingSettlements,
            duplicateOddsMatches,
            staleScheduled,
            score,
            DateTime.UtcNow,
            oddsSnapshotCount,
            oddsProviderCount,
            oddsBookmakerCount,
            oddsMarketCount));
    }
}
