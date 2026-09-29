namespace CalcioAnalytic.Analytics.Similarity;

/// <summary>
/// Tuning options for <see cref="ISimilarMatchEngine.FindSimilar"/>: how many
/// results to return and how strongly each feature group influences the distance.
/// </summary>
/// <remarks>
/// Weights scale each feature's contribution to the weighted Euclidean distance
/// <i>after</i> min-max normalization. A weight of <c>1.0</c> gives a feature its
/// default influence; <c>0.0</c> excludes it entirely; larger values make it
/// matter more. All weights default to <c>1.0</c> so the out-of-the-box behavior
/// treats every feature group equally.
/// </remarks>
/// <param name="TopK">
/// The maximum number of results to return, ordered by descending similarity.
/// Defaults to <c>10</c>. Values &lt;= 0 yield an empty result.
/// </param>
/// <param name="RatingDifferenceWeight">
/// Weight applied to the normalized <see cref="SimilarityFeatures.RatingDifference"/>.
/// </param>
/// <param name="FormWeight">
/// Weight applied to the normalized form features
/// (<see cref="SimilarityFeatures.HomeFormPoints"/> and
/// <see cref="SimilarityFeatures.AwayFormPoints"/>).
/// </param>
/// <param name="GoalsAvgWeight">
/// Weight applied to the normalized average-goals features
/// (<see cref="SimilarityFeatures.HomeGoalsAvg"/> and
/// <see cref="SimilarityFeatures.AwayGoalsAvg"/>).
/// </param>
/// <param name="ClosingOddsWeight">
/// Weight applied to the normalized <see cref="SimilarityFeatures.ClosingHomeOdds"/>.
/// </param>
/// <param name="MovementWeight">
/// Weight applied to the normalized <see cref="SimilarityFeatures.MovementPercentage"/>.
/// </param>
public sealed record SimilarityOptions(
    int TopK = 10,
    double RatingDifferenceWeight = 1.0,
    double FormWeight = 1.0,
    double GoalsAvgWeight = 1.0,
    double ClosingOddsWeight = 1.0,
    double MovementWeight = 1.0)
{
    /// <summary>The default options: <see cref="TopK"/> = 10 and all weights = 1.0.</summary>
    public static SimilarityOptions Default { get; } = new();
}
