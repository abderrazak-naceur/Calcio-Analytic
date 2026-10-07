namespace CalcioAnalytic.Api.Contracts.Dtos;

/// <summary>
/// A summary projection of a match for list responses. Never exposes EF
/// navigation properties; only scalar identifiers and result fields. The
/// lifecycle status is projected as its string name.
/// </summary>
public sealed record MatchSummaryDto(
    Guid Id,
    Guid CompetitionId,
    Guid SeasonId,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    DateTime KickoffUtc,
    string Status,
    int? HomeScore,
    int? AwayScore);

/// <summary>
/// A detailed projection of a single match, adding venue, round, referee, and
/// half-time scores to the summary fields. The lifecycle status is projected as
/// its string name.
/// </summary>
public sealed record MatchDetailDto(
    Guid Id,
    Guid CompetitionId,
    Guid SeasonId,
    Guid HomeTeamId,
    Guid AwayTeamId,
    DateTime KickoffUtc,
    string Status,
    int? HomeScore,
    int? AwayScore,
    string? Venue,
    string? Round,
    string? Referee,
    int? HomeScoreHalfTime,
    int? AwayScoreHalfTime);
