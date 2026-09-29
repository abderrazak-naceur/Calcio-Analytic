namespace CalcioAnalytic.Analytics.Patterns;

/// <summary>
/// A flattened, analysis-ready view of a single finished historical match that
/// the <see cref="IHistoricalPatternEngine"/> reasons over. This is a plain data
/// carrier: the caller is responsible for producing point-in-time-correct,
/// already-loaded records from persistence. The engine performs no data access.
/// </summary>
/// <param name="MatchId">The unique identifier of the match.</param>
/// <param name="CompetitionId">The competition the match belongs to.</param>
/// <param name="SeasonId">The season the match belongs to.</param>
/// <param name="HomeTeamId">The home team identifier.</param>
/// <param name="AwayTeamId">The away team identifier.</param>
/// <param name="KickoffUtc">The kickoff time in UTC.</param>
/// <param name="Outcome">
/// The full-time result from the home team's perspective: "Home", "Draw",
/// "Away", or "Unknown" when the outcome cannot be determined.
/// </param>
/// <param name="HomeScore">The full-time home score, when known.</param>
/// <param name="AwayScore">The full-time away score, when known.</param>
/// <param name="TotalGoals">The total goals scored in the match, when known.</param>
/// <param name="OpeningHomeOdds">The opening decimal odds for a home win, when known.</param>
/// <param name="ClosingHomeOdds">The closing decimal odds for a home win, when known.</param>
/// <param name="OpeningOverUnderLine">The opening over/under goals line, when known.</param>
/// <param name="MovementPercentage">
/// The percentage movement of the home price from open to close, when known.
/// </param>
/// <param name="BookmakerId">The bookmaker that priced the match, when known.</param>
public sealed record PatternMatchRecord(
    Guid MatchId,
    Guid CompetitionId,
    Guid SeasonId,
    Guid HomeTeamId,
    Guid AwayTeamId,
    DateTime KickoffUtc,
    string Outcome,
    int? HomeScore,
    int? AwayScore,
    int? TotalGoals,
    decimal? OpeningHomeOdds,
    decimal? ClosingHomeOdds,
    decimal? OpeningOverUnderLine,
    decimal? MovementPercentage,
    Guid? BookmakerId);
