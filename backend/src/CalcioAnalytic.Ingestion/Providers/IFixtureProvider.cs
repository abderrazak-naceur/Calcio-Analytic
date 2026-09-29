using CalcioAnalytic.Contracts.Providers;

namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Capability for retrieving match fixtures from a provider.
/// </summary>
public interface IFixtureProvider : IProviderAdapter
{
    /// <summary>
    /// Retrieves the fixtures for a competition and season.
    /// </summary>
    /// <param name="competitionExternalId">The provider-native competition identifier.</param>
    /// <param name="seasonExternalId">The provider-native season identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderMatchDto>> GetFixturesAsync(
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single fixture by its provider-native identifier.
    /// </summary>
    /// <param name="matchExternalId">The provider-native match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>The fixture, or <c>null</c> when the provider has no such match.</returns>
    Task<ProviderMatchDto?> GetFixtureAsync(
        string matchExternalId,
        CancellationToken ct = default);
}
