namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a bookmaker as delivered by an external data provider.
/// </summary>
/// <param name="ExternalId">The provider-native identifier for the bookmaker (provider-defined format).</param>
/// <param name="Name">The bookmaker display name.</param>
/// <param name="Code">An optional provider or vendor code for the bookmaker.</param>
public sealed record ProviderBookmakerDto(
    string ExternalId,
    string Name,
    string? Code = null);
