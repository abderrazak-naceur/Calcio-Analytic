using CalcioAnalytic.Analytics.Odds;

namespace CalcioAnalytic.Analytics.Markets;

/// <summary>
/// The latest decimal odds for a single selection within a market line, used as
/// input to market-level analysis.
/// </summary>
/// <param name="SelectionId">The selection being priced.</param>
/// <param name="SelectionName">Human-readable selection name (e.g. "Home").</param>
/// <param name="DecimalOdds">The latest decimal odds for the selection.</param>
public sealed record MarketSelectionPrice(Guid SelectionId, string SelectionName, decimal DecimalOdds);

/// <summary>
/// The per-selection breakdown produced by <see cref="MarketAnalyzer"/>. Raw
/// probabilities retain full precision; callers may round for display.
/// </summary>
/// <param name="SelectionId">The selection.</param>
/// <param name="SelectionName">Human-readable selection name.</param>
/// <param name="DecimalOdds">The selection's decimal odds.</param>
/// <param name="ImpliedProbability">Raw implied probability (<c>1 / odds</c>).</param>
/// <param name="NormalizedProbability">
/// Fair probability with the bookmaker margin removed; these sum to 1 across the
/// market.
/// </param>
public sealed record MarketSelectionAnalysis(
    Guid SelectionId,
    string SelectionName,
    decimal DecimalOdds,
    decimal ImpliedProbability,
    decimal NormalizedProbability);

/// <summary>
/// The analysis of a single market line: its overround (bookmaker margin) and a
/// per-selection breakdown of implied and normalized probabilities.
/// </summary>
/// <param name="MarketLineId">The market line analyzed.</param>
/// <param name="Overround">
/// Sum of implied probabilities across all selections; &gt; 1 indicates a margin.
/// </param>
/// <param name="MarginPercentage">
/// The bookmaker margin as a fraction (<c>overround - 1</c>); 0 when the market
/// carries no meaningful prices.
/// </param>
/// <param name="Selections">The per-selection breakdown, in input order.</param>
public sealed record MarketAnalysisResult(
    Guid MarketLineId,
    decimal Overround,
    decimal MarginPercentage,
    IReadOnlyList<MarketSelectionAnalysis> Selections)
{
    /// <summary>Creates an empty result for a market line with no selections.</summary>
    public static MarketAnalysisResult Empty(Guid marketLineId) =>
        new(marketLineId, 0m, 0m, Array.Empty<MarketSelectionAnalysis>());
}

/// <summary>
/// Analyzes a single market line from the latest decimal odds of its selections,
/// computing overround and both implied and normalized (fair) probabilities.
/// </summary>
public static class MarketAnalyzer
{
    /// <summary>
    /// Analyzes a market line.
    /// </summary>
    /// <param name="marketLineId">Identifier of the market line being analyzed.</param>
    /// <param name="selections">
    /// The selections of the market line with their latest decimal odds. May be
    /// null or empty, both handled without throwing.
    /// </param>
    /// <returns>A <see cref="MarketAnalysisResult"/>.</returns>
    public static MarketAnalysisResult Analyze(Guid marketLineId, IEnumerable<MarketSelectionPrice> selections)
    {
        if (selections is null)
        {
            return MarketAnalysisResult.Empty(marketLineId);
        }

        var list = selections.ToList();
        if (list.Count == 0)
        {
            return MarketAnalysisResult.Empty(marketLineId);
        }

        var oddsList = list.Select(s => s.DecimalOdds).ToList();
        var overround = OddsMath.Overround(oddsList);
        var normalized = OddsMath.NormalizedProbabilities(oddsList);

        var breakdown = new MarketSelectionAnalysis[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            var implied = OddsMath.ImpliedProbability(list[i].DecimalOdds);
            breakdown[i] = new MarketSelectionAnalysis(
                list[i].SelectionId,
                list[i].SelectionName,
                list[i].DecimalOdds,
                implied,
                normalized[i]);
        }

        var margin = overround > 0m ? overround - 1m : 0m;

        return new MarketAnalysisResult(marketLineId, overround, margin, breakdown);
    }
}
