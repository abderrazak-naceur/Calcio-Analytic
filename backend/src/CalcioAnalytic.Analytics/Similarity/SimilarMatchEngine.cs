using System.Globalization;

namespace CalcioAnalytic.Analytics.Similarity;

/// <summary>
/// Default <see cref="ISimilarMatchEngine"/> implementation: a deterministic
/// k-nearest-neighbor search over min-max normalized features using a weighted
/// Euclidean distance.
/// </summary>
/// <remarks>
/// <para>
/// The algorithm, per call:
/// <list type="number">
///   <item>Exclude the target (by <see cref="SimilarityFeatures.MatchId"/>) from the candidates.</item>
///   <item>
///     Compute per-feature min/max over the surviving candidates <i>plus</i> the
///     target, then min-max normalize every feature to <c>[0, 1]</c>. Features
///     whose range is zero (all values equal) contribute no distance.
///   </item>
///   <item>
///     For each candidate, take the weighted Euclidean distance to the target
///     across the normalized axes and convert it to a similarity score in
///     <c>(0, 1]</c> via <c>score = 1 / (1 + weightedDistance)</c>.
///   </item>
///   <item>
///     Order by descending score, break ties by ascending <c>MatchId</c>, and
///     return the top <see cref="SimilarityOptions.TopK"/>.
///   </item>
/// </list>
/// </para>
/// <para>
/// Normalization spans the target and the candidate set together so that all
/// distances share the same scale for a given query. The engine never reads
/// external data; see <see cref="ISimilarMatchEngine"/> for the anti-leakage
/// contract.
/// </para>
/// </remarks>
public sealed class SimilarMatchEngine : ISimilarMatchEngine
{
    /// <summary>
    /// A single normalizable axis: how to read the raw value from a feature
    /// vector, the weight to apply, and a human-readable label for explanations.
    /// </summary>
    private sealed record Axis(string Label, Func<SimilarityFeatures, double> Selector, double Weight);

    /// <inheritdoc />
    public IReadOnlyList<SimilarMatch> FindSimilar(
        SimilarityFeatures target,
        IEnumerable<SimilarityFeatures> candidates,
        SimilarityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(candidates);

        var opts = options ?? SimilarityOptions.Default;

        if (opts.TopK <= 0)
        {
            return Array.Empty<SimilarMatch>();
        }

        // Exclude the target itself; keep candidate order stable for reproducibility.
        var pool = candidates
            .Where(c => c is not null && c.MatchId != target.MatchId)
            .ToList();

        if (pool.Count == 0)
        {
            return Array.Empty<SimilarMatch>();
        }

        var axes = BuildAxes(opts);

        // Min/max spans the candidates plus the target so all distances share a scale.
        var mins = new double[axes.Length];
        var maxs = new double[axes.Length];
        for (var a = 0; a < axes.Length; a++)
        {
            var value = axes[a].Selector(target);
            mins[a] = value;
            maxs[a] = value;
        }

        foreach (var candidate in pool)
        {
            for (var a = 0; a < axes.Length; a++)
            {
                var value = axes[a].Selector(candidate);
                if (value < mins[a])
                {
                    mins[a] = value;
                }

                if (value > maxs[a])
                {
                    maxs[a] = value;
                }
            }
        }

        var ranges = new double[axes.Length];
        for (var a = 0; a < axes.Length; a++)
        {
            ranges[a] = maxs[a] - mins[a];
        }

        // Pre-normalize the target once.
        var targetNorm = new double[axes.Length];
        for (var a = 0; a < axes.Length; a++)
        {
            targetNorm[a] = Normalize(axes[a].Selector(target), mins[a], ranges[a]);
        }

        var scored = new List<SimilarMatch>(pool.Count);
        foreach (var candidate in pool)
        {
            var weightedSquares = 0.0;
            // Track per-axis normalized absolute difference for the explanation.
            var diffs = new (Axis Axis, double NormalizedDiff)[axes.Length];

            for (var a = 0; a < axes.Length; a++)
            {
                var candidateNorm = Normalize(axes[a].Selector(candidate), mins[a], ranges[a]);
                var diff = candidateNorm - targetNorm[a];
                weightedSquares += axes[a].Weight * diff * diff;
                diffs[a] = (axes[a], Math.Abs(diff));
            }

            var distance = Math.Sqrt(weightedSquares);
            var score = 1.0 / (1.0 + distance);

            scored.Add(new SimilarMatch(candidate.MatchId, score, BuildExplanation(candidate, diffs)));
        }

        return scored
            .OrderByDescending(s => s.SimilarityScore)
            .ThenBy(s => s.MatchId)
            .Take(opts.TopK)
            .ToList();
    }

    /// <summary>
    /// Builds the normalizable axes. Composite groups (form, goals) average their
    /// members into a single normalized axis so the group weight applies once.
    /// </summary>
    private static Axis[] BuildAxes(SimilarityOptions opts) =>
    [
        new Axis("rating difference", f => f.RatingDifference, opts.RatingDifferenceWeight),
        new Axis("home form", f => f.HomeFormPoints, opts.FormWeight),
        new Axis("away form", f => f.AwayFormPoints, opts.FormWeight),
        new Axis("home goals avg", f => f.HomeGoalsAvg, opts.GoalsAvgWeight),
        new Axis("away goals avg", f => f.AwayGoalsAvg, opts.GoalsAvgWeight),
        new Axis("closing odds", f => f.ClosingHomeOdds, opts.ClosingOddsWeight),
        new Axis("odds movement", f => f.MovementPercentage, opts.MovementWeight),
    ];

    /// <summary>
    /// Min-max normalizes a value to <c>[0, 1]</c>. A zero range (all values
    /// equal for the axis) maps to <c>0</c> so the axis contributes no distance.
    /// </summary>
    private static double Normalize(double value, double min, double range) =>
        range <= 0.0 ? 0.0 : (value - min) / range;

    /// <summary>
    /// Produces up to three readable strings for the axes that were most similar
    /// (smallest normalized absolute difference), reporting the raw candidate
    /// value for context. Weightless axes (weight 0) are ignored.
    /// </summary>
    private static IReadOnlyList<string> BuildExplanation(
        SimilarityFeatures candidate,
        (Axis Axis, double NormalizedDiff)[] diffs)
    {
        return diffs
            .Where(d => d.Axis.Weight > 0.0)
            .OrderBy(d => d.NormalizedDiff)
            .ThenBy(d => d.Axis.Label, StringComparer.Ordinal)
            .Take(3)
            .Select(d => Describe(d.Axis, candidate))
            .ToList();
    }

    private static string Describe(Axis axis, SimilarityFeatures candidate)
    {
        var raw = axis.Selector(candidate);
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} close (value={1:0.00})",
            axis.Label,
            raw);
    }
}
