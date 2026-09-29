using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Api.Services;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace CalcioAnalytic.Api.Controllers;

/// <summary>
/// Read endpoints powering the dashboard. All KPIs are computed from efficient
/// aggregate queries (COUNT / GROUP BY / MAX) against the database rather than
/// by loading whole tables; EF entities are never returned directly.
/// </summary>
/// <remarks>
/// NOTE: the summary aggregates can later be backed by a materialized database
/// view or precomputed read model at scale (TASK-022 / Phase 22) without
/// changing these routes or contracts.
/// </remarks>
[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly CalcioAnalyticDbContext _db;

    public DashboardController(CalcioAnalyticDbContext db) => _db = db;

    /// <summary>
    /// Returns the aggregate dashboard summary (counts, status breakdown, data
    /// freshness, and analysis backlog).
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken ct)
    {
        var summary = await DashboardReadService.GetSummaryAsync(_db, ct);
        return Ok(summary);
    }

    /// <summary>
    /// Returns the most recent matches by kickoff time, newest first.
    /// </summary>
    /// <param name="take">Maximum number of matches to return (default 10, clamped to 1..100).</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentMatchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecentMatchDto>>> GetRecent(
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        var matches = await DashboardReadService.GetRecentMatchesAsync(_db, take, ct);
        return Ok(matches);
    }
}
