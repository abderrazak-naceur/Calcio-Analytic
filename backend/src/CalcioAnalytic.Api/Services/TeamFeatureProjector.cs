using CalcioAnalytic.Domain.Matches;

namespace CalcioAnalytic.Api.Services;

/// <summary>
/// Computes point-in-time team features (recent form, rolling goal averages, and
/// a simple ELO-style strength rating) from already-loaded matches. This is the
/// TASK-031 baseline that lets similarity and pattern analysis rank on real
/// form/goals/rating signals instead of placeholder zeros.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purity.</b> This type performs no data access. The caller supplies the full
/// set of finished matches; every method derives its result purely from that
/// input, which makes the computation deterministic and independently testable.
/// </para>
/// <para>
/// <b>Point-in-time correctness (anti-leakage).</b> Every method takes a
/// <c>cutoffUtc</c> and considers <i>only</i> matches whose
/// <see cref="Match.KickoffUtc"/> is strictly earlier than that cutoff. No match
/// at or after the cutoff can influence the result, so features for a given match
/// never contain information from that match itself or any later one. This is the
/// whole point of a point-in-time feature store.
/// </para>
/// <para>
/// <b>What counts.</b> A match contributes to form/goals/rating only when it is
/// finished with a known score, i.e. its status is one of
/// <see cref="MatchStatus.Finished"/>, <see cref="MatchStatus.SettlementPending"/>,
/// <see cref="MatchStatus.Analyzed"/>, or <see cref="MatchStatus.Reconciled"/>, and
/// both <see cref="Match.HomeScore"/> and <see cref="Match.AwayScore"/> are set.
/// </para>
/// <para>
/// TODO (TASK-031, real feature store): a production feature store would
/// precompute and persist these values (indexed by team and cutoff) rather than
/// recomputing them from the full match set on every request. The current
/// implementation recomputes per call, which is fine for the small data set today
/// but is O(matches) per lookup and O(matches) for the rating replay.
/// </para>
/// </remarks>
public static class TeamFeatureProjector
{
    /// <summary>
    /// The point-in-time form and goal features for a single team as-of a cutoff.
    /// All values are zero when the team has no qualifying prior matches.
    /// </summary>
    /// <param name="FormPoints">
    /// Sum of points (win = 3, draw = 1, loss = 0) from the team's perspective over
    /// the most recent form window before the cutoff.
    /// </param>
    /// <param name="GoalsForAvg">Average goals scored by the team over the window.</param>
    /// <param name="GoalsAgainstAvg">Average goals conceded by the team over the window.</param>
    /// <param name="MatchesConsidered">
    /// The number of matches actually used (window size, capped by availability).
    /// </param>
    public sealed record TeamFeatures(
        double FormPoints,
        double GoalsForAvg,
        double GoalsAgainstAvg,
        int MatchesConsidered);

    /// <summary>
    /// Determines whether a match qualifies for feature computation: it must be
    /// finished (play complete) with both scores known.
    /// </summary>
    private static bool IsCounted(Match match) =>
        (match.Status is MatchStatus.Finished
            or MatchStatus.SettlementPending
            or MatchStatus.Analyzed
            or MatchStatus.Reconciled)
        && match.HomeScore is not null
        && match.AwayScore is not null;

    /// <summary>
    /// Computes the point-in-time <see cref="TeamFeatures"/> for a team as-of a
    /// cutoff. Considers only qualifying matches the team played in that kicked off
    /// strictly before <paramref name="cutoffUtc"/>, takes the most recent
    /// <paramref name="formWindow"/> of them (by kickoff, newest first), and
    /// aggregates form points and goal averages from the team's perspective.
    /// Returns all-zero features when there are no qualifying prior matches.
    /// </summary>
    /// <param name="teamId">The team whose features to compute.</param>
    /// <param name="cutoffUtc">
    /// The exclusive point-in-time cutoff; only matches before this are considered.
    /// </param>
    /// <param name="allFinishedMatches">The pool of finished matches to draw from.</param>
    /// <param name="formWindow">
    /// The maximum number of most-recent matches to include (default 5). Values
    /// less than 1 yield all-zero features.
    /// </param>
    public static TeamFeatures ComputeAsOf(
        Guid teamId,
        DateTime cutoffUtc,
        IReadOnlyList<Match> allFinishedMatches,
        int formWindow = 5)
    {
        ArgumentNullException.ThrowIfNull(allFinishedMatches);

        if (formWindow < 1)
        {
            return new TeamFeatures(0d, 0d, 0d, 0);
        }

        // Anti-leakage: strictly-before-cutoff, team played, and countable.
        var window = allFinishedMatches
            .Where(m => m.KickoffUtc < cutoffUtc
                && (m.HomeTeamId == teamId || m.AwayTeamId == teamId)
                && IsCounted(m))
            .OrderByDescending(m => m.KickoffUtc)
            .Take(formWindow)
            .ToList();

        if (window.Count == 0)
        {
            return new TeamFeatures(0d, 0d, 0d, 0);
        }

        var points = 0;
        var goalsFor = 0;
        var goalsAgainst = 0;

        foreach (var m in window)
        {
            var isHome = m.HomeTeamId == teamId;

            // Both scores are known here (IsCounted), so the casts are safe.
            var scored = isHome ? m.HomeScore!.Value : m.AwayScore!.Value;
            var conceded = isHome ? m.AwayScore!.Value : m.HomeScore!.Value;

            goalsFor += scored;
            goalsAgainst += conceded;

            if (scored > conceded)
            {
                points += 3;
            }
            else if (scored == conceded)
            {
                points += 1;
            }
        }

        var count = window.Count;

        return new TeamFeatures(
            FormPoints: points,
            GoalsForAvg: (double)goalsFor / count,
            GoalsAgainstAvg: (double)goalsAgainst / count,
            MatchesConsidered: count);
    }

    /// <summary>
    /// Computes a simple ELO-style strength rating for a team as-of a cutoff.
    /// Replays every qualifying match (both teams known, finished with a score,
    /// kickoff strictly before <paramref name="cutoffUtc"/>) in chronological
    /// order, maintaining a rating dictionary that starts every team at
    /// <paramref name="baseRating"/> and applying the standard ELO update after
    /// each match. Returns the target team's rating at the cutoff, or
    /// <paramref name="baseRating"/> when the team has no qualifying prior matches.
    /// </summary>
    /// <remarks>
    /// The expected score for a team is <c>1 / (1 + 10^((opp - self) / 400))</c> and
    /// the update is <c>rating += k * (actual - expected)</c>, with actual = 1 for a
    /// win, 0.5 for a draw, and 0 for a loss. The replay is deterministic given the
    /// same inputs. Home advantage is intentionally omitted for simplicity in this
    /// baseline; a later revision could add a home-field bonus to the expected score.
    /// </remarks>
    /// <param name="teamId">The team whose rating to return.</param>
    /// <param name="cutoffUtc">
    /// The exclusive point-in-time cutoff; only matches before this are replayed.
    /// </param>
    /// <param name="allFinishedMatches">The pool of finished matches to replay.</param>
    /// <param name="baseRating">The starting rating for every team (default 1500).</param>
    /// <param name="k">The ELO K-factor controlling update magnitude (default 20).</param>
    public static double ComputeRatingAsOf(
        Guid teamId,
        DateTime cutoffUtc,
        IReadOnlyList<Match> allFinishedMatches,
        double baseRating = 1500,
        double k = 20)
    {
        ArgumentNullException.ThrowIfNull(allFinishedMatches);

        // Anti-leakage: replay only matches that kicked off strictly before cutoff.
        var replay = allFinishedMatches
            .Where(m => m.KickoffUtc < cutoffUtc && IsCounted(m))
            .OrderBy(m => m.KickoffUtc)
            .ThenBy(m => m.Id) // stable, deterministic tie-break for equal kickoffs
            .ToList();

        var ratings = new Dictionary<Guid, double>();

        double RatingOf(Guid id) => ratings.TryGetValue(id, out var r) ? r : baseRating;

        foreach (var m in replay)
        {
            var home = RatingOf(m.HomeTeamId);
            var away = RatingOf(m.AwayTeamId);

            var expectedHome = 1d / (1d + Math.Pow(10d, (away - home) / 400d));
            var expectedAway = 1d - expectedHome;

            // Both scores known (IsCounted).
            var homeScore = m.HomeScore!.Value;
            var awayScore = m.AwayScore!.Value;

            double actualHome;
            if (homeScore > awayScore)
            {
                actualHome = 1d;
            }
            else if (homeScore < awayScore)
            {
                actualHome = 0d;
            }
            else
            {
                actualHome = 0.5d;
            }

            var actualAway = 1d - actualHome;

            ratings[m.HomeTeamId] = home + k * (actualHome - expectedHome);
            ratings[m.AwayTeamId] = away + k * (actualAway - expectedAway);
        }

        return RatingOf(teamId);
    }
}
