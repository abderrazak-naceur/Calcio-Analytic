namespace CalcioAnalytic.Api.Contracts.Dtos;

/// <summary>
/// Aggregate read-model powering the dashboard KPIs. Every field is computed
/// server-side from efficient aggregate queries (COUNT / GROUP BY / MAX) rather
/// than by loading whole tables into memory.
/// </summary>
/// <remarks>
/// This is the "read model over raw joins" idea expressed at the API level. At
/// scale these aggregates can be replaced by a materialized database view or a
/// precomputed read model (TASK-022 / Phase 22) without changing this contract.
/// </remarks>
public sealed record DashboardSummaryDto(
    int TotalMatches,
    IReadOnlyDictionary<string, int> MatchesByStatus,
    int FinishedMatches,
    int AnalyzedMatches,
    long OddsSnapshotsCount,
    int BookmakersCount,
    int MarketsCount,
    int CompetitionsCount,
    int TeamsCount,
    DateTime? DataFreshnessUtc,
    int AnalysisBacklog);

/// <summary>
/// A compact projection of a match for the dashboard's recent-matches list.
/// The lifecycle status is projected as its string name.
/// </summary>
public sealed record RecentMatchDto(
    Guid Id,
    Guid HomeTeamId,
    Guid AwayTeamId,
    DateTime KickoffUtc,
    string Status,
    int? HomeScore,
    int? AwayScore);
