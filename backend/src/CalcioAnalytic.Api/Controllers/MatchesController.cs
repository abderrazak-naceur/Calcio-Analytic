using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Controllers;

/// <summary>
/// Read endpoints exposing canonical match data. Queries the database directly
/// (read side) and projects to response DTOs; EF entities are never returned.
/// </summary>
[ApiController]
[Route("api/v1/matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly CalcioAnalyticDbContext _db;

    public MatchesController(CalcioAnalyticDbContext db) => _db = db;

    /// <summary>
    /// Lists matches, optionally filtered by lifecycle status. The status value
    /// is parsed case-insensitively; an unrecognized value is ignored (no filter).
    /// </summary>
    /// <param name="status">Optional match status filter (case-insensitive).</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MatchSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MatchSummaryDto>>> List(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var query = _db.Matches.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<MatchStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(m => m.Status == parsed);
        }

        var matches = await query
            .OrderBy(m => m.KickoffUtc)
            .Select(m => new
            {
                m.Id,
                m.CompetitionId,
                m.SeasonId,
                m.HomeTeamId,
                HomeTeamName = m.HomeTeam!.Name,
                m.AwayTeamId,
                AwayTeamName = m.AwayTeam!.Name,
                m.KickoffUtc,
                m.Status,
                m.HomeScore,
                m.AwayScore,
            })
            .ToListAsync(ct);

        var result = matches
            .Select(m => new MatchSummaryDto(
                m.Id,
                m.CompetitionId,
                m.SeasonId,
                m.HomeTeamId,
                m.HomeTeamName,
                m.AwayTeamId,
                m.AwayTeamName,
                m.KickoffUtc,
                m.Status.ToString(),
                m.HomeScore,
                m.AwayScore))
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Returns the detail of a single match, or 404 when it does not exist.
    /// </summary>
    /// <param name="id">The canonical match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MatchDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var match = await _db.Matches.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new
            {
                m.Id,
                m.CompetitionId,
                m.SeasonId,
                m.HomeTeamId,
                HomeTeamName = m.HomeTeam!.Name,
                m.AwayTeamId,
                AwayTeamName = m.AwayTeam!.Name,
                m.KickoffUtc,
                m.Status,
                m.HomeScore,
                m.AwayScore,
                m.Venue,
                m.Round,
                m.Referee,
                m.HomeScoreHalfTime,
                m.AwayScoreHalfTime,
            })
            .FirstOrDefaultAsync(ct);

        if (match is null)
        {
            return NotFound();
        }

        var dto = new MatchDetailDto(
            match.Id,
            match.CompetitionId,
            match.SeasonId,
            match.HomeTeamId,
            match.HomeTeamName,
            match.AwayTeamId,
            match.AwayTeamName,
            match.KickoffUtc,
            match.Status.ToString(),
            match.HomeScore,
            match.AwayScore,
            match.Venue,
            match.Round,
            match.Referee,
            match.HomeScoreHalfTime,
            match.AwayScoreHalfTime);

        return Ok(dto);
    }
}
