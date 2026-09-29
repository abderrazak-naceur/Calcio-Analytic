using CalcioAnalytic.Contracts.Providers;

namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Capability for retrieving odds, bookmakers and markets from a provider.
/// </summary>
public interface IOddsProvider : IProviderAdapter
{
    /// <summary>
    /// Retrieves the odds snapshots available for a match.
    /// </summary>
    /// <param name="matchExternalId">The provider-native match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderOddsSnapshotDto>> GetOddsAsync(
        string matchExternalId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the bookmakers exposed by the provider.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderBookmakerDto>> GetBookmakersAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the betting markets exposed by the provider.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderMarketDto>> GetMarketsAsync(CancellationToken ct = default);
}
