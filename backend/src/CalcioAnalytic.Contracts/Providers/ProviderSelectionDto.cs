namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a single selection within a market line
/// (e.g. "Home", "Over") as delivered by an external data provider.
/// </summary>
/// <param name="Name">The selection name (e.g. "Home", "Away", "Over", "Under").</param>
/// <param name="DecimalOdds">The odds expressed in decimal format.</param>
/// <param name="FractionalOdds">The optional odds expressed in fractional format (e.g. "5/2").</param>
/// <param name="AmericanOdds">The optional odds expressed in American/moneyline format.</param>
/// <param name="IsSuspended">Whether the selection is currently suspended by the provider.</param>
public sealed record ProviderSelectionDto(
    string Name,
    decimal DecimalOdds,
    string? FractionalOdds = null,
    int? AmericanOdds = null,
    bool IsSuspended = false);
