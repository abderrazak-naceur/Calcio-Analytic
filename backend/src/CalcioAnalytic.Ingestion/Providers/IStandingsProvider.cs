namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Capability for retrieving competition standings from a provider.
/// </summary>
public interface IStandingsProvider : IProviderAdapter
{
    /// <summary>
    /// Retrieves the standings table for a competition and season.
    /// </summary>
    /// <param name="competitionExternalId">The provider-native competition identifier.</param>
    /// <param name="seasonExternalId">The provider-native season identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderStandingRowDto>> GetStandingsAsync(
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default);
}
