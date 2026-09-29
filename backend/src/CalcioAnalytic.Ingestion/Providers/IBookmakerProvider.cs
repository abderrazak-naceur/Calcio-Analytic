using CalcioAnalytic.Contracts.Providers;

namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Capability for retrieving the bookmaker catalog from a provider.
/// </summary>
public interface IBookmakerProvider : IProviderAdapter
{
    /// <summary>
    /// Retrieves the bookmakers exposed by the provider.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderBookmakerDto>> GetBookmakersAsync(CancellationToken ct = default);
}
