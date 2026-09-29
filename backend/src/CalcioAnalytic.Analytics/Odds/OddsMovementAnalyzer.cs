using CalcioAnalytic.Domain.Odds;

namespace CalcioAnalytic.Analytics.Odds;

/// <summary>
/// The outcome of analyzing how a single selection's price moved over time for
/// one (match, bookmaker, market line, selection) tuple. All odds values retain
/// full <see cref="decimal"/> precision; callers may round for display.
/// </summary>
/// <param name="SampleCount">Number of snapshots considered.</param>
/// <param name="OpeningOdds">Decimal odds of the earliest snapshot in the sequence.</param>
/// <param name="ClosingOdds">Decimal odds of the latest snapshot in the sequence.</param>
/// <param name="MinOdds">Lowest decimal odds observed.</param>
/// <param name="MaxOdds">Highest decimal odds observed.</param>
/// <param name="NumberOfChanges">
/// Count of consecutive snapshots whose odds differ from the immediately
/// preceding snapshot (i.e. the number of price moves).
/// </param>
/// <param name="MovementAbsolute">Closing minus opening odds (signed).</param>
/// <param name="MovementPercentage">
/// Movement expressed as a fraction of the opening odds
/// (<c>(closing - opening) / opening</c>); 0 when opening is 0.
/// </param>
/// <param name="Volatility">
/// Population standard deviation of the odds values across the sequence.
/// </param>
public sealed record OddsMovementResult(
    int SampleCount,
    decimal OpeningOdds,
    decimal ClosingOdds,
    decimal MinOdds,
    decimal MaxOdds,
    int NumberOfChanges,
    decimal MovementAbsolute,
    decimal MovementPercentage,
    decimal Volatility)
{
    /// <summary>An empty result, used when no snapshots are supplied.</summary>
    public static OddsMovementResult Empty { get; } =
        new(0, 0m, 0m, 0m, 0m, 0, 0m, 0m, 0m);
}

/// <summary>
/// Analyzes the price movement of a single selection over an ordered sequence of
/// <see cref="OddsSnapshot"/> records. The input is expected to already be scoped
/// to one (match, bookmaker, market line, selection) and ordered chronologically;
/// the analyzer re-orders defensively by capture time regardless.
/// </summary>
public static class OddsMovementAnalyzer
{
    /// <summary>
    /// Computes the movement summary for a selection's snapshot history.
    /// </summary>
    /// <param name="snapshots">
    /// The chronological snapshots for a single selection. May be null, empty, or
    /// a single element, all of which are handled without throwing.
    /// </param>
    /// <returns>An <see cref="OddsMovementResult"/> describing the movement.</returns>
    public static OddsMovementResult Analyze(IEnumerable<OddsSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            return OddsMovementResult.Empty;
        }

        // Defensive ordering: provider timestamp first, then ingestion time as a
        // deterministic tiebreaker so results are stable for identical inputs.
        var ordered = snapshots
            .OrderBy(s => s.ProviderTimestampUtc)
            .ThenBy(s => s.IngestionTimestampUtc)
            .ThenBy(s => s.Id)
            .ToList();

        if (ordered.Count == 0)
        {
            return OddsMovementResult.Empty;
        }

        var opening = ordered[0].DecimalOdds;
        var closing = ordered[^1].DecimalOdds;
        var min = opening;
        var max = opening;
        var changes = 0;

        for (var i = 0; i < ordered.Count; i++)
        {
            var value = ordered[i].DecimalOdds;
            if (value < min)
            {
                min = value;
            }

            if (value > max)
            {
                max = value;
            }

            if (i > 0 && value != ordered[i - 1].DecimalOdds)
            {
                changes++;
            }
        }

        var movementAbsolute = closing - opening;
        var movementPercentage = opening == 0m ? 0m : movementAbsolute / opening;
        var volatility = PopulationStandardDeviation(ordered.Select(s => s.DecimalOdds));

        return new OddsMovementResult(
            ordered.Count,
            opening,
            closing,
            min,
            max,
            changes,
            movementAbsolute,
            movementPercentage,
            volatility);
    }

    /// <summary>
    /// Computes the population standard deviation of a set of decimal values.
    /// Uses <c>double</c> internally for the square root, then returns a decimal.
    /// Returns 0 for empty or single-element inputs.
    /// </summary>
    internal static decimal PopulationStandardDeviation(IEnumerable<decimal> values)
    {
        var list = values as IReadOnlyList<decimal> ?? values.ToList();
        if (list.Count <= 1)
        {
            return 0m;
        }

        decimal sum = 0m;
        for (var i = 0; i < list.Count; i++)
        {
            sum += list[i];
        }

        var mean = sum / list.Count;

        decimal sumSquaredDeviations = 0m;
        for (var i = 0; i < list.Count; i++)
        {
            var deviation = list[i] - mean;
            sumSquaredDeviations += deviation * deviation;
        }

        var variance = sumSquaredDeviations / list.Count;
        return (decimal)Math.Sqrt((double)variance);
    }
}
