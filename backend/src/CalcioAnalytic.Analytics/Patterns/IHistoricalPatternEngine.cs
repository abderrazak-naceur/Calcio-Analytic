namespace CalcioAnalytic.Analytics.Patterns;

/// <summary>
/// Answers historical questions over a supplied set of finished matches using a
/// reproducible <see cref="PatternQuery"/>. Implementations must be pure and
/// deterministic: the same query applied to the same records always yields the
/// same <see cref="PatternResult"/>. The engine performs no data access; the
/// caller supplies already-loaded, point-in-time-correct records.
/// </summary>
public interface IHistoricalPatternEngine
{
    /// <summary>
    /// Applies the query's filters to the records and computes outcome
    /// distributions, percentages, averages, data completeness, and a Wilson
    /// score confidence interval for the home-win proportion.
    /// </summary>
    /// <param name="query">The reproducible filter definition.</param>
    /// <param name="records">The historical match records to analyze.</param>
    /// <returns>The aggregated pattern result.</returns>
    PatternResult Analyze(PatternQuery query, IEnumerable<PatternMatchRecord> records);
}
