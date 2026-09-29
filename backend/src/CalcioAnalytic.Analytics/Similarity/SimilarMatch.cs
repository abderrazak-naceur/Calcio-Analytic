namespace CalcioAnalytic.Analytics.Similarity;

/// <summary>
/// A single similarity result: a candidate match together with its similarity to
/// the target and a human-readable explanation of the strongest contributors.
/// </summary>
/// <param name="MatchId">The candidate match this result describes.</param>
/// <param name="SimilarityScore">
/// The similarity to the target in the range <c>(0, 1]</c>, where <c>1</c> means
/// the two feature vectors are identical (zero weighted distance) and values
/// approaching <c>0</c> mean increasingly dissimilar.
/// </param>
/// <param name="Explanation">
/// A short, ordered list of human-readable strings describing the features that
/// were <i>most</i> similar between the candidate and the target (e.g.
/// <c>"rating difference close (|Δ|=0.30)"</c>). Ordered from most to least
/// similar contributor.
/// </param>
public sealed record SimilarMatch(
    Guid MatchId,
    double SimilarityScore,
    IReadOnlyList<string> Explanation);
