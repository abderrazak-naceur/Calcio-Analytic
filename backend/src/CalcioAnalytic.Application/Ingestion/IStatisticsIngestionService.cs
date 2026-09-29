namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// Application service that ingests provider match statistics and timeline
/// events for a match into canonical <c>MatchStatistic</c> and <c>MatchEvent</c>
/// records.
/// </summary>
/// <remarks>
/// Ingestion is idempotent. Statistics are reconciled by (match, team, name) and
/// updated in place when already present; events are reconciled by
/// (match, type, minute, team) and skipped when an identical event already
/// exists, so re-running the same source data neither duplicates statistics nor
/// the event timeline.
/// </remarks>
public interface IStatisticsIngestionService
{
    /// <summary>
    /// Ingests the statistics and events the provider exposes for a single match.
    /// </summary>
    /// <param name="providerCode">The provider code identifying the source adapter.</param>
    /// <param name="matchExternalId">The provider-native match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A summary of the statistics and events inserted.</returns>
    Task<StatisticsIngestionResult> IngestStatisticsAsync(
        string providerCode,
        string matchExternalId,
        CancellationToken ct = default);
}
