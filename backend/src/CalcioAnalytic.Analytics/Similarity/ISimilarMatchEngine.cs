namespace CalcioAnalytic.Analytics.Similarity;

/// <summary>
/// Finds the historical matches most similar to a target match, using only the
/// pre-computed feature vectors it is given.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purity.</b> Implementations perform no data access and are fully
/// deterministic: the same target, candidate set, and options always produce the
/// same result, including a stable tie-break.
/// </para>
/// <para>
/// <b>Anti-leakage (Phase 12 acceptance).</b> The engine reasons only over the
/// as-of feature values supplied in <see cref="SimilarityFeatures"/>. It cannot
/// detect look-ahead bias; ensuring every feature is point-in-time correct
/// (computed at the historical cutoff, with no future data) is the caller's
/// responsibility.
/// </para>
/// </remarks>
public interface ISimilarMatchEngine
{
    /// <summary>
    /// Ranks <paramref name="candidates"/> by similarity to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The match to find neighbors for.</param>
    /// <param name="candidates">
    /// The historical matches to search. The target itself (matched by
    /// <see cref="SimilarityFeatures.MatchId"/>) is excluded automatically. May be
    /// empty, in which case an empty result is returned.
    /// </param>
    /// <param name="options">
    /// Optional tuning. When <c>null</c>, <see cref="SimilarityOptions.Default"/>
    /// is used.
    /// </param>
    /// <returns>
    /// Up to <see cref="SimilarityOptions.TopK"/> results ordered by descending
    /// <see cref="SimilarMatch.SimilarityScore"/>, with ties broken deterministically
    /// by ascending <see cref="SimilarMatch.MatchId"/>.
    /// </returns>
    IReadOnlyList<SimilarMatch> FindSimilar(
        SimilarityFeatures target,
        IEnumerable<SimilarityFeatures> candidates,
        SimilarityOptions? options = null);
}
