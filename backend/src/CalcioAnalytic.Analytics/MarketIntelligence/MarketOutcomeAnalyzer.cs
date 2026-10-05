using CalcioAnalytic.Domain.Settlement;

namespace CalcioAnalytic.Analytics.MarketIntelligence;

/// <summary>
/// Pure Market vs Reality classifier. The lowest decimal odds are treated as
/// the market favorite. A favorite wins => HIT; a favorite loses => MISS.
/// A MISS becomes an UPSET when the actual winning selection reaches the
/// configured winner-odds threshold.
/// </summary>
public static class MarketOutcomeAnalyzer
{
    public static MarketOutcomeResult Analyze(
        IEnumerable<MarketOutcomeSelection> selections,
        decimal upsetThreshold = 5m)
    {
        if (upsetThreshold <= 1m)
            throw new ArgumentOutOfRangeException(nameof(upsetThreshold), "Upset threshold must be greater than 1.");

        ArgumentNullException.ThrowIfNull(selections);

        var list = selections
            .Where(x => !string.IsNullOrWhiteSpace(x.SelectionName) && x.DecimalOdds > 1m)
            .ToList();

        if (list.Count < 2)
            return Unknown();

        var minimumOdds = list.Min(x => x.DecimalOdds);
        var favorites = list.Where(x => x.DecimalOdds == minimumOdds).ToList();

        // Equal-priced favorites do not provide a unique market expectation.
        if (favorites.Count != 1)
            return Unknown();

        var favorite = favorites[0];
        var winners = list.Where(x => x.SettlementStatus == SettlementStatus.Won).ToList();

        // A valid settled market must have exactly one winner.
        if (winners.Count != 1)
            return new MarketOutcomeResult(
                favorite.SelectionName,
                favorite.DecimalOdds,
                favorite.SettlementStatus,
                MarketOutcomeClassification.Unknown,
                winners.Count == 1 ? winners[0].SelectionName : null,
                winners.Count == 1 ? winners[0].DecimalOdds : null,
                MarketOutcomeUpsetClassification.None,
                upsetThreshold);

        var winner = winners[0];

        if (favorite.SettlementStatus == SettlementStatus.Won)
        {
            return new MarketOutcomeResult(
                favorite.SelectionName,
                favorite.DecimalOdds,
                favorite.SettlementStatus,
                MarketOutcomeClassification.Hit,
                winner.SelectionName,
                winner.DecimalOdds,
                MarketOutcomeUpsetClassification.None,
                upsetThreshold);
        }

        if (favorite.SettlementStatus != SettlementStatus.Lost)
        {
            return new MarketOutcomeResult(
                favorite.SelectionName,
                favorite.DecimalOdds,
                favorite.SettlementStatus,
                MarketOutcomeClassification.Unknown,
                winner.SelectionName,
                winner.DecimalOdds,
                MarketOutcomeUpsetClassification.None,
                upsetThreshold);
        }

        var isUpset = winner.DecimalOdds >= upsetThreshold;

        return new MarketOutcomeResult(
            favorite.SelectionName,
            favorite.DecimalOdds,
            favorite.SettlementStatus,
            MarketOutcomeClassification.Miss,
            winner.SelectionName,
            winner.DecimalOdds,
            isUpset ? MarketOutcomeUpsetClassification.Upset : MarketOutcomeUpsetClassification.None,
            upsetThreshold);
    }

    private static MarketOutcomeResult Unknown() =>
        new(
            null,
            null,
            SettlementStatus.Unknown,
            MarketOutcomeClassification.Unknown,
            null,
            null,
            MarketOutcomeUpsetClassification.None,
            null);
}
