using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Services;

/// <summary>
/// Read-side helper that computes the dashboard summary from the database using
/// SQL-translatable aggregate queries (COUNT / GROUP BY / MAX). It never loads
/// whole tables: only small aggregate result sets cross the wire.
/// </summary>
/// <remarks>
/// Implemented as a static class taking the <see cref="CalcioAnalyticDbContext"/>
/// so it needs no DI registration and does not touch the application pipeline.
/// NOTE: these aggregates can be replaced by a materialized view / precomputed
/// read model at scale (TASK-022 / Phase 22); this service is the honest,
/// query-per-KPI baseline until then.
/// </remarks>
public static class DashboardReadService
{
    /// <summary>
    /// Computes the full dashboard summary. Each KPI is a separate aggregate
    /// query so nothing larger than a handful of rows is materialized. All
    /// queries are read-only.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    public static async Task<DashboardSummaryDto> GetSummaryAsync(
        CalcioAnalyticDbContext db,
        CancellationToken ct = default)
    {
        // Total matches — COUNT(*).
        var totalMatches = await db.Matches.AsNoTracking().CountAsync(ct);

        // Matches grouped by status — GROUP BY translated to SQL. The enum is
        // stored as an integer/string by the provider; we project the count per
        // status key and map the enum to its name in memory (a tiny, bounded set).
        var statusCounts = await db.Matches.AsNoTracking()
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var matchesByStatus = statusCounts
            .ToDictionary(x => x.Status.ToString(), x => x.Count);

        // Finished matches — COUNT with a WHERE predicate.
        var finishedMatches = await db.Matches.AsNoTracking()
            .CountAsync(m => m.Status == MatchStatus.Finished, ct);

        // Distinct matches that have at least one analysis row — COUNT(DISTINCT).
        var analyzedMatches = await db.MatchAnalyses.AsNoTracking()
            .Select(a => a.MatchId)
            .Distinct()
            .CountAsync(ct);

        // Odds snapshots — LongCount to be safe on high-volume tables.
        var oddsSnapshotsCount = await db.OddsSnapshots.AsNoTracking().LongCountAsync(ct);

        var bookmakersCount = await db.Bookmakers.AsNoTracking().CountAsync(ct);
        var marketsCount = await db.Markets.AsNoTracking().CountAsync(ct);
        var competitionsCount = await db.Competitions.AsNoTracking().CountAsync(ct);
        var teamsCount = await db.Teams.AsNoTracking().CountAsync(ct);

        // Data freshness — MAX(IngestionTimestampUtc). Projected to a nullable
        // first so an empty table yields null instead of throwing.
        var dataFreshnessUtc = await db.OddsSnapshots.AsNoTracking()
            .Select(s => (DateTime?)s.IngestionTimestampUtc)
            .MaxAsync(ct);

        // Analysis backlog — Finished matches with NO analysis row. Expressed as
        // a correlated NOT EXISTS subquery (translated to SQL); no join/table load.
        var analysisBacklog = await db.Matches.AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished
                && !db.MatchAnalyses.Any(a => a.MatchId == m.Id))
            .CountAsync(ct);

        return new DashboardSummaryDto(
            totalMatches,
            matchesByStatus,
            finishedMatches,
            analyzedMatches,
            oddsSnapshotsCount,
            bookmakersCount,
            marketsCount,
            competitionsCount,
            teamsCount,
            dataFreshnessUtc,
            analysisBacklog);
    }

    /// <summary>
    /// Returns the most recent matches by kickoff time. Read-only, projected to
    /// a compact DTO, and bounded by <paramref name="take"/>.
    /// </summary>
    /// <param name="db">The application database context.</param>
    /// <param name="take">Maximum number of matches to return (clamped to 1..100).</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    public static async Task<IReadOnlyList<RecentMatchDto>> GetRecentMatchesAsync(
        CalcioAnalyticDbContext db,
        int take,
        CancellationToken ct = default)
    {
        var limit = Math.Clamp(take, 1, 100);

        // Project scalars in SQL (status stays an enum) and take only N rows.
        // Team names are joined here so the dashboard never renders raw ids.
        var rows = await db.Matches.AsNoTracking()
            .OrderByDescending(m => m.KickoffUtc)
            .Take(limit)
            .Select(m => new
            {
                m.Id,
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

        return rows
            .Select(m => new RecentMatchDto(
                m.Id,
                m.HomeTeamId,
                m.HomeTeamName,
                m.AwayTeamId,
                m.AwayTeamName,
                m.KickoffUtc,
                m.Status.ToString(),
                m.HomeScore,
                m.AwayScore))
            .ToList();
    }
}
