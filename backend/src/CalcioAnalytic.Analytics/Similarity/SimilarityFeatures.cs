namespace CalcioAnalytic.Analytics.Similarity;

/// <summary>
/// The comparable feature vector for a single match, used by
/// <see cref="ISimilarMatchEngine"/> to measure how alike two matches are.
/// </summary>
/// <remarks>
/// <para>
/// <b>Point-in-time correctness (anti-leakage).</b> Every numeric value on this
/// record must be computed <i>as-of the historical cutoff</i> for the match
/// (typically kickoff, or an earlier decision point). The engine performs no
/// data access and cannot verify this; it operates purely on the values it is
/// given. It is the caller's responsibility to supply cutoff-correct features
/// that contain no future information (Phase 12 acceptance).
/// </para>
/// <para>
/// Feature values are exposed as <see cref="double"/> because the engine's
/// distance math (min-max normalization and weighted Euclidean distance) is
/// performed in <see cref="double"/>. Callers that hold <see cref="decimal"/>
/// source values (e.g. odds) should convert at the boundary; the small precision
/// loss is irrelevant to a relative similarity ranking.
/// </para>
/// </remarks>
/// <param name="MatchId">The match this feature vector describes.</param>
/// <param name="CompetitionId">The competition the match belongs to.</param>
/// <param name="IsHome">
/// Whether the subject team is the home team. Carried for context and caller
/// filtering; it is not used in the distance computation.
/// </param>
/// <param name="HomeRating">The home team's strength rating, as-of the cutoff.</param>
/// <param name="AwayRating">The away team's strength rating, as-of the cutoff.</param>
/// <param name="RatingDifference">
/// The signed rating gap (typically <c>HomeRating - AwayRating</c>), as-of the cutoff.
/// </param>
/// <param name="HomeFormPoints">
/// The home team's recent form measured in points, as-of the cutoff.
/// </param>
/// <param name="AwayFormPoints">
/// The away team's recent form measured in points, as-of the cutoff.
/// </param>
/// <param name="HomeGoalsAvg">
/// The home team's average goals (scored) over the form window, as-of the cutoff.
/// </param>
/// <param name="AwayGoalsAvg">
/// The away team's average goals (scored) over the form window, as-of the cutoff.
/// </param>
/// <param name="OpeningHomeOdds">
/// The opening decimal odds for the home outcome, as-of the cutoff.
/// </param>
/// <param name="ClosingHomeOdds">
/// The closing decimal odds for the home outcome, as-of the cutoff.
/// </param>
/// <param name="MovementPercentage">
/// The relative movement of the home odds from opening to closing, expressed as a
/// fraction (e.g. <c>0.05</c> for a 5% drift), as-of the cutoff.
/// </param>
public sealed record SimilarityFeatures(
    Guid MatchId,
    Guid CompetitionId,
    bool IsHome,
    double HomeRating,
    double AwayRating,
    double RatingDifference,
    double HomeFormPoints,
    double AwayFormPoints,
    double HomeGoalsAvg,
    double AwayGoalsAvg,
    double OpeningHomeOdds,
    double ClosingHomeOdds,
    double MovementPercentage);
