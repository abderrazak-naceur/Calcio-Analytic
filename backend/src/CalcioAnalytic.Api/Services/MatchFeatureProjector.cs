using CalcioAnalytic.Analytics.Patterns;
using CalcioAnalytic.Analytics.Similarity;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;

namespace CalcioAnalytic.Api.Services;

/// <summary>
/// Derives analysis-ready feature carriers (<see cref="PatternMatchRecord"/> and
/// <see cref="SimilarityFeatures"/>) for a match from already-loaded persistence
/// data. This type performs no data access; the caller supplies the match and its
/// 1X2 "Home" odds snapshots. Keeping the projection pure makes it deterministic
/// and independently testable, and keeps the controllers thin.
/// </summary>
/// <remarks>
/// <para>
/// <b>Odds derivation.</b> Opening and closing home prices are taken from the
/// match's 1X2 "Home" odds snapshots. The opening price is the average
/// <see cref="OddsSnapshot.DecimalOdds"/> across bookmakers at the <i>earliest</i>
/// <see cref="OddsSnapshot.ProviderTimestampUtc"/>; the closing price is the same
/// average at the <i>latest</i> provider timestamp. Averaging across bookmakers at
/// each endpoint yields a single, provider-neutral home price and keeps the value
/// stable when several books priced the match. Movement is
/// <c>(closing - opening) / opening</c> when opening &gt; 0, otherwise null.
/// When no 1X2 "Home" snapshots exist, all odds-derived fields are left null (for
/// patterns) or 0 (for similarity), so the endpoint never fabricates prices.
/// </para>
/// <para>
/// <b>Ratings / form / goals are placeholders.</b> Real team strength ratings,
/// recent-form points, and rolling goal averages are not stored yet (a feature
/// store is planned under TASK-031). Until then these <see cref="SimilarityFeatures"/>
/// fields are set to 0. Similarity therefore ranks on real odds-based signals
/// only; it does not invent ratings. This keeps the endpoint honest.
/// </para>
/// </remarks>
public static class MatchFeatureProjector
{
    /// <summary>The market name/code that identifies the 1X2 (match winner) market.</summary>
    public const string OneXTwoMarketToken = "1X2";

    /// <summary>The selection name that identifies the home outcome within a 1X2 line.</summary>
    public const string HomeSelectionName = "Home";

    /// <summary>
    /// The opening and closing home prices derived from a match's 1X2 "Home"
    /// snapshots, plus the relative movement between them. Any component is null
    /// when the underlying snapshots are absent.
    /// </summary>
    /// <param name="OpeningHomeOdds">Average home price at the earliest provider timestamp, or null.</param>
    /// <param name="ClosingHomeOdds">Average home price at the latest provider timestamp, or null.</param>
    /// <param name="MovementPercentage">Relative open-to-close movement as a fraction, or null.</param>
    public readonly record struct HomeOddsSummary(
        decimal? OpeningHomeOdds,
        decimal? ClosingHomeOdds,
        decimal? MovementPercentage);

    /// <summary>
    /// Computes the opening/closing home odds and movement from a set of 1X2
    /// "Home" snapshots for a single match. The caller is responsible for having
    /// already filtered <paramref name="homeSnapshots"/> to the 1X2 "Home"
    /// selection for the target match. An empty set yields all-null components.
    /// </summary>
    public static HomeOddsSummary SummarizeHomeOdds(IReadOnlyCollection<OddsSnapshot> homeSnapshots)
    {
        ArgumentNullException.ThrowIfNull(homeSnapshots);

        if (homeSnapshots.Count == 0)
        {
            return new HomeOddsSummary(null, null, null);
        }

        var earliest = homeSnapshots.Min(s => s.ProviderTimestampUtc);
        var latest = homeSnapshots.Max(s => s.ProviderTimestampUtc);

        // Average across bookmakers at each endpoint for a provider-neutral price.
        var opening = homeSnapshots
            .Where(s => s.ProviderTimestampUtc == earliest)
            .Average(s => s.DecimalOdds);

        var closing = homeSnapshots
            .Where(s => s.ProviderTimestampUtc == latest)
            .Average(s => s.DecimalOdds);

        decimal? movement = opening > 0m
            ? (closing - opening) / opening
            : null;

        return new HomeOddsSummary(opening, closing, movement);
    }

    /// <summary>
    /// Determines the full-time outcome from the home team's perspective.
    /// Returns "Home", "Draw", or "Away" when both scores are known, otherwise
    /// "Unknown".
    /// </summary>
    public static string DeriveOutcome(int? homeScore, int? awayScore)
    {
        if (homeScore is null || awayScore is null)
        {
            return "Unknown";
        }

        if (homeScore > awayScore)
        {
            return "Home";
        }

        return homeScore < awayScore ? "Away" : "Draw";
    }

    /// <summary>
    /// Projects a match and its 1X2 "Home" snapshots to a
    /// <see cref="PatternMatchRecord"/>. Outcome is derived from the score;
    /// total goals is the sum when both scores are known; odds fields come from
    /// <see cref="SummarizeHomeOdds"/>. The over/under opening line is left null
    /// (not yet sourced). <paramref name="bookmakerId"/> is a representative
    /// bookmaker for the home snapshots, when available.
    /// </summary>
    public static PatternMatchRecord ToPatternRecord(
        Match match,
        IReadOnlyCollection<OddsSnapshot> homeSnapshots)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(homeSnapshots);

        var outcome = DeriveOutcome(match.HomeScore, match.AwayScore);

        int? totalGoals = match.HomeScore is int h && match.AwayScore is int a
            ? h + a
            : null;

        var odds = SummarizeHomeOdds(homeSnapshots);

        Guid? bookmakerId = homeSnapshots.Count > 0
            ? homeSnapshots
                .OrderBy(s => s.ProviderTimestampUtc)
                .Select(s => (Guid?)s.BookmakerId)
                .First()
            : null;

        return new PatternMatchRecord(
            MatchId: match.Id,
            CompetitionId: match.CompetitionId,
            SeasonId: match.SeasonId,
            HomeTeamId: match.HomeTeamId,
            AwayTeamId: match.AwayTeamId,
            KickoffUtc: match.KickoffUtc,
            Outcome: outcome,
            HomeScore: match.HomeScore,
            AwayScore: match.AwayScore,
            TotalGoals: totalGoals,
            OpeningHomeOdds: odds.OpeningHomeOdds,
            ClosingHomeOdds: odds.ClosingHomeOdds,
            OpeningOverUnderLine: null,
            MovementPercentage: odds.MovementPercentage,
            BookmakerId: bookmakerId);
    }

    /// <summary>
    /// Projects a match and its 1X2 "Home" snapshots to a
    /// <see cref="SimilarityFeatures"/> vector. Odds fields are populated from the
    /// snapshots (0 when absent). Ratings, form, and goal averages are set to 0 as
    /// documented placeholders until a feature store exists (TASK-031).
    /// </summary>
    public static SimilarityFeatures ToSimilarityFeatures(
        Match match,
        IReadOnlyCollection<OddsSnapshot> homeSnapshots)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(homeSnapshots);

        var odds = SummarizeHomeOdds(homeSnapshots);

        var opening = (double)(odds.OpeningHomeOdds ?? 0m);
        var closing = (double)(odds.ClosingHomeOdds ?? 0m);
        var movement = (double)(odds.MovementPercentage ?? 0m);

        return new SimilarityFeatures(
            MatchId: match.Id,
            CompetitionId: match.CompetitionId,
            IsHome: true,
            HomeRating: 0d,        // placeholder until feature store (TASK-031)
            AwayRating: 0d,        // placeholder until feature store (TASK-031)
            RatingDifference: 0d,  // placeholder until feature store (TASK-031)
            HomeFormPoints: 0d,    // placeholder until feature store (TASK-031)
            AwayFormPoints: 0d,    // placeholder until feature store (TASK-031)
            HomeGoalsAvg: 0d,      // placeholder until feature store (TASK-031)
            AwayGoalsAvg: 0d,      // placeholder until feature store (TASK-031)
            OpeningHomeOdds: opening,
            ClosingHomeOdds: closing,
            MovementPercentage: movement);
    }
}
