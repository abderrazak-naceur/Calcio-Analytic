namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a betting market as delivered by an external data provider.
/// </summary>
/// <param name="ExternalId">The provider-native identifier for the market (provider-defined format).</param>
/// <param name="Name">The market display name (e.g. "Match Winner", "Over/Under").</param>
/// <param name="Code">An optional provider or vendor code for the market.</param>
/// <param name="Description">An optional human-readable description of the market.</param>
public sealed record ProviderMarketDto(
    string ExternalId,
    string Name,
    string? Code = null,
    string? Description = null);
