namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a team as delivered by an external data provider.
/// Adapters map vendor formats into this DTO so provider-native shapes never reach the Domain.
/// </summary>
/// <param name="ExternalId">The provider-native identifier for the team (provider-defined format).</param>
/// <param name="Name">The full team name.</param>
/// <param name="ShortName">An optional short or abbreviated team name.</param>
/// <param name="CountryName">The optional country name the team is associated with.</param>
/// <param name="CountryCode">The optional country code (e.g. ISO alpha code) for the team.</param>
public sealed record ProviderTeamDto(
    string ExternalId,
    string Name,
    string? ShortName = null,
    string? CountryName = null,
    string? CountryCode = null);
