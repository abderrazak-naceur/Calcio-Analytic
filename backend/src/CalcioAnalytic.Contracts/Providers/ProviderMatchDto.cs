namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a match as delivered by an external data provider.
/// </summary>
/// <param name="ExternalId">The provider-native identifier for the match (provider-defined format).</param>
/// <param name="CompetitionExternalId">The provider-native identifier of the competition.</param>
/// <param name="SeasonExternalId">The optional provider-native identifier of the season.</param>
/// <param name="HomeTeamExternalId">The provider-native identifier of the home team.</param>
/// <param name="AwayTeamExternalId">The provider-native identifier of the away team.</param>
/// <param name="KickoffUtc">The scheduled kickoff time.</param>
/// <param name="Venue">The optional venue name.</param>
/// <param name="Round">The optional round or matchday label.</param>
/// <param name="Referee">The optional referee name.</param>
/// <param name="Status">The match status (e.g. "scheduled", "live", "finished").</param>
/// <param name="HomeScore">The optional current or final home score.</param>
/// <param name="AwayScore">The optional current or final away score.</param>
/// <param name="HomeScoreHalfTime">The optional home score at half time.</param>
/// <param name="AwayScoreHalfTime">The optional away score at half time.</param>
public sealed record ProviderMatchDto(
    string ExternalId,
    string CompetitionExternalId,
    string? SeasonExternalId,
    string HomeTeamExternalId,
    string AwayTeamExternalId,
    DateTimeOffset KickoffUtc,
    string? Venue,
    string? Round,
    string? Referee,
    string Status,
    int? HomeScore = null,
    int? AwayScore = null,
    int? HomeScoreHalfTime = null,
    int? AwayScoreHalfTime = null);
