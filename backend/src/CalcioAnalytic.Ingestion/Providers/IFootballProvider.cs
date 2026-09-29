using CalcioAnalytic.Contracts.Providers;

namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Capability for retrieving catalog data (competitions, seasons, teams)
/// from a football data provider.
/// </summary>
public interface IFootballProvider : IProviderAdapter
{
    /// <summary>
    /// Retrieves the competitions exposed by the provider.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderCompetitionDto>> GetCompetitionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the teams participating in a competition for a given season.
    /// </summary>
    /// <param name="competitionExternalId">The provider-native competition identifier.</param>
    /// <param name="seasonExternalId">The provider-native season identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderTeamDto>> GetTeamsAsync(
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the seasons available for a competition.
    /// </summary>
    /// <param name="competitionExternalId">The provider-native competition identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<ProviderSeasonDto>> GetSeasonsAsync(
        string competitionExternalId,
        CancellationToken ct = default);
}
