namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a competition as delivered by an external data provider.
/// </summary>
/// <param name="ExternalId">The provider-native identifier for the competition (provider-defined format).</param>
/// <param name="Name">The competition display name.</param>
/// <param name="CountryName">The optional country name the competition belongs to.</param>
/// <param name="Tier">The optional tier or division level of the competition.</param>
public sealed record ProviderCompetitionDto(
    string ExternalId,
    string Name,
    string? CountryName = null,
    string? Tier = null);
