namespace CalcioAnalytic.Analytics.MarketIntelligence;

/// <summary>
/// Classification of a market's expected outcome versus the settled result.
/// </summary>
public enum MarketOutcomeClassification
{
    Unknown,
    Hit,
    Miss,
}

/// <summary>
/// Secondary classification for a failed favorite.
/// </summary>
public enum MarketOutcomeUpsetClassification
{
    None,
    Upset,
}

/// <summary>
/// A priced selection paired with its settled status.
/// </summary>
public sealed record MarketOutcomeSelection(
    string SelectionName,
    decimal DecimalOdds,
    Domain.Settlement.SettlementStatus SettlementStatus);

/// <summary>
/// Immutable result of comparing the market favorite with the actual winner.
/// </summary>
public sealed record MarketOutcomeResult(
    string? FavoriteSelectionName,
    decimal? FavoriteOdds,
    Domain.Settlement.SettlementStatus FavoriteStatus,
    MarketOutcomeClassification Classification,
    string? WinningSelectionName,
    decimal? WinningOdds,
    MarketOutcomeUpsetClassification UpsetClassification,
    decimal? UpsetThreshold)
{
    public bool IsFavoriteFailure =>
        Classification == MarketOutcomeClassification.Miss;

    public bool IsUpset =>
        UpsetClassification == MarketOutcomeUpsetClassification.Upset;
}
