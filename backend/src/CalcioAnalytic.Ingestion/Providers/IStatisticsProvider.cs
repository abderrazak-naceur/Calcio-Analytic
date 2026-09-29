using CalcioAnalytic.Contracts.Providers;

namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Capability for retrieving match statistics and events from a provider.
/// </summary>
public interface IStatisticsProvider : IProviderAdapter
{
    /// <summary>
    /// Retrieves the statistics recorded for a match.
    /// </summary>
    /// <param name="matchExternalId">The provider-native match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderStatisticDto>> GetStatisticsAsync(
        string matchExternalId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the timeline events recorded for a match.
    /// </summary>
    /// <param name="matchExternalId">The provider-native match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderEventDto>> GetEventsAsync(
        string matchExternalId,
        CancellationToken ct = default);
}
