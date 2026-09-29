namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a market line (a specific priced variant of a market)
/// as delivered by an external data provider.
/// </summary>
/// <param name="MarketExternalId">The provider-native identifier of the market this line belongs to.</param>
/// <param name="Line">The optional line value (e.g. 2.5 for an Over/Under market).</param>
/// <param name="Period">The optional period the line applies to (e.g. "FullTime", "FirstHalf").</param>
/// <param name="Selections">The selections available on this market line.</param>
public sealed record ProviderMarketLineDto(
    string MarketExternalId,
    decimal? Line,
    string? Period,
    IReadOnlyList<ProviderSelectionDto> Selections);
