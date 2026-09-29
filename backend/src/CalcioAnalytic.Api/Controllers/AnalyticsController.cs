using CalcioAnalytic.Analytics.Match;
using CalcioAnalytic.Analytics.Odds;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Controllers;

/// <summary>
/// Read endpoints exposing derived analytics for a match: raw odds snapshots,
/// per-selection odds movement, cross-bookmaker dispersion, and the full match
/// analysis report. All data is loaded read-only and projected to DTOs; EF
/// entities are never returned directly.
/// </summary>
[ApiController]
[Route("api/v1/analytics/matches")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly CalcioAnalyticDbContext _db;
    private readonly IMatchAnalysisEngine _engine;

    public AnalyticsController(CalcioAnalyticDbContext db, IMatchAnalysisEngine engine)
    {
        _db = db;
        _engine = engine;
    }

    /// <summary>
    /// Returns every odds snapshot captured for the match, ordered by capture time.
    /// </summary>
    /// <param name="id">The canonical match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("{id:guid}/odds")]
    [ProducesResponseType(typeof(IReadOnlyList<OddsSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OddsSnapshotDto>>> GetOdds(Guid id, CancellationToken ct)
    {
        var snapshots = await _db.OddsSnapshots.AsNoTracking()
            .Where(s => s.MatchId == id)
            .OrderBy(s => s.ProviderTimestampUtc)
            .ThenBy(s => s.IngestionTimestampUtc)
            .Select(s => new OddsSnapshotDto(
                s.Id,
                s.BookmakerId,
                s.MarketLineId,
                s.SelectionId,
                s.DecimalOdds,
                s.ImpliedProbability,
                s.IsLive,
                s.Kind,
                s.BookmakerTimestampUtc,
                s.ProviderTimestampUtc))
            .ToListAsync(ct);

        return Ok(snapshots);
    }

    /// <summary>
    /// Computes per-selection odds movement for the match. Snapshots are grouped
    /// by (bookmaker, market line, selection) and each group is analyzed
    /// independently.
    /// </summary>
    /// <param name="id">The canonical match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("{id:guid}/movement")]
    [ProducesResponseType(typeof(IReadOnlyList<OddsMovementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OddsMovementDto>>> GetMovement(Guid id, CancellationToken ct)
    {
        var snapshots = await _db.OddsSnapshots.AsNoTracking()
            .Where(s => s.MatchId == id)
            .ToListAsync(ct);

        var results = snapshots
            .GroupBy(s => new { s.BookmakerId, s.MarketLineId, s.SelectionId })
            .Select(g => new OddsMovementDto(
                g.Key.BookmakerId,
                g.Key.MarketLineId,
                g.Key.SelectionId,
                OddsMovementAnalyzer.Analyze(g)))
            .ToList();

        return Ok(results);
    }

    /// <summary>
    /// Computes cross-bookmaker price dispersion for the match. Snapshots are
    /// grouped by (market line, selection) and analyzed using the latest price
    /// per bookmaker.
    /// </summary>
    /// <param name="id">The canonical match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("{id:guid}/bookmakers")]
    [ProducesResponseType(typeof(IReadOnlyList<BookmakerDispersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookmakerDispersionDto>>> GetBookmakers(Guid id, CancellationToken ct)
    {
        var snapshots = await _db.OddsSnapshots.AsNoTracking()
            .Where(s => s.MatchId == id)
            .ToListAsync(ct);

        var results = snapshots
            .GroupBy(s => new { s.MarketLineId, s.SelectionId })
            .Select(g => new BookmakerDispersionDto(
                g.Key.MarketLineId,
                g.Key.SelectionId,
                BookmakerAnalyzer.AnalyzeLatestPerBookmaker(g)))
            .ToList();

        return Ok(results);
    }

    /// <summary>
    /// Loads the match and all related data, runs the analysis engine, and returns
    /// the structured report. Returns 404 when the match does not exist.
    /// </summary>
    /// <param name="id">The canonical match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MatchAnalysisReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchAnalysisReport>> GetAnalysis(Guid id, CancellationToken ct)
    {
        var match = await _db.Matches.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (match is null)
        {
            return NotFound();
        }

        var oddsSnapshots = await _db.OddsSnapshots.AsNoTracking()
            .Where(s => s.MatchId == id)
            .ToListAsync(ct);

        var marketLines = await _db.MarketLines.AsNoTracking()
            .Where(l => l.MatchId == id)
            .ToListAsync(ct);

        var marketLineIds = marketLines.Select(l => l.Id).ToList();

        var selections = await _db.Selections.AsNoTracking()
            .Where(sel => marketLineIds.Contains(sel.MarketLineId))
            .ToListAsync(ct);

        var statistics = await _db.MatchStatistics.AsNoTracking()
            .Where(s => s.MatchId == id)
            .ToListAsync(ct);

        var events = await _db.MatchEvents.AsNoTracking()
            .Where(e => e.MatchId == id)
            .ToListAsync(ct);

        var settlements = await _db.MarketSettlements.AsNoTracking()
            .Where(s => s.MatchId == id)
            .ToListAsync(ct);

        var bookmakerIds = oddsSnapshots
            .Select(s => s.BookmakerId)
            .Distinct()
            .ToList();

        var input = new MatchAnalysisInput(
            match,
            oddsSnapshots,
            marketLines,
            selections,
            bookmakerIds,
            statistics,
            events,
            settlements);

        var output = _engine.AnalyzeToJson(input);

        return Ok(output.Report);
    }
}
