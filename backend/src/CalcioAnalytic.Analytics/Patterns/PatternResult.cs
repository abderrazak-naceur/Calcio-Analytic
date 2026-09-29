namespace CalcioAnalytic.Analytics.Patterns;

/// <summary>
/// A Wilson score confidence interval for a proportion, expressed as decimal
/// bounds in the range 0..1.
/// </summary>
/// <param name="Lower">The lower bound of the interval.</param>
/// <param name="Upper">The upper bound of the interval.</param>
public readonly record struct ConfidenceInterval(decimal Lower, decimal Upper);

/// <summary>
/// The aggregated, deterministic output of the
/// <see cref="IHistoricalPatternEngine"/> for one <see cref="PatternQuery"/> over
/// a supplied set of <see cref="PatternMatchRecord"/> values.
/// </summary>
/// <param name="SampleSize">The number of records that matched the query.</param>
/// <param name="ResultDistribution">
/// Counts of full-time outcomes keyed by "Home", "Draw", and "Away".
/// </param>
/// <param name="ResultPercentages">
/// The fraction (0..1) of the sample for each of "Home", "Draw", and "Away".
/// </param>
/// <param name="AverageTotalGoals">The mean total goals across records with a known total, or null when none.</param>
/// <param name="OverUnderDistribution">
/// Counts keyed by "Over2.5" and "Under2.5" across records with a known total.
/// </param>
/// <param name="AverageClosingHomeOdds">The mean closing home odds across records with known odds, or null when none.</param>
/// <param name="AverageMovementPercentage">The mean movement percentage across records with a known movement, or null when none.</param>
/// <param name="DataCompleteness">
/// The fraction (0..1) of matched records that carry a non-null closing home odds value.
/// </param>
/// <param name="HomeWinConfidenceInterval">
/// The Wilson score 95% confidence interval for the home-win proportion, or null
/// when the sample is too small to be meaningful.
/// </param>
/// <param name="Methodology">The methodology version identifier.</param>
/// <param name="QueryHash">The hash of the query that produced this result.</param>
public sealed record PatternResult(
    int SampleSize,
    IReadOnlyDictionary<string, int> ResultDistribution,
    IReadOnlyDictionary<string, decimal> ResultPercentages,
    decimal? AverageTotalGoals,
    IReadOnlyDictionary<string, int> OverUnderDistribution,
    decimal? AverageClosingHomeOdds,
    decimal? AverageMovementPercentage,
    decimal DataCompleteness,
    ConfidenceInterval? HomeWinConfidenceInterval,
    string Methodology,
    string QueryHash);
