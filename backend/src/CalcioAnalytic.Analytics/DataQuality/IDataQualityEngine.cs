namespace CalcioAnalytic.Analytics.DataQuality;

/// <summary>
/// A pure engine that evaluates the data-quality of a single match from a
/// pre-loaded <see cref="MatchQualityInput"/>. Implementations perform no data
/// access and are deterministic: for a given input and reference time they always
/// produce the same <see cref="DataQualityReport"/>.
/// </summary>
public interface IDataQualityEngine
{
    /// <summary>
    /// Evaluates every data-quality check against <paramref name="input"/> and
    /// produces a severity-weighted quality report.
    /// </summary>
    /// <param name="input">The pre-loaded match snapshot to evaluate.</param>
    /// <param name="nowUtc">
    /// The reference "now" (UTC) used for time-relative checks such as detecting
    /// stale scheduled matches. Injected for determinism.
    /// </param>
    /// <returns>An immutable report describing each check and the overall score.</returns>
    DataQualityReport EvaluateMatch(MatchQualityInput input, DateTime nowUtc);
}
