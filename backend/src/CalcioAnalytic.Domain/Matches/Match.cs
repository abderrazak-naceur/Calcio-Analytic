using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Matches;

/// <summary>
/// A single football match between two teams within a competition season,
/// tracking scheduling, lifecycle status, and scores.
/// </summary>
public class Match : Entity
{
    /// <summary>Foreign key to the <see cref="Catalog.Competition"/>.</summary>
    public Guid CompetitionId { get; set; }

    /// <summary>Navigation to the competition.</summary>
    public Competition? Competition { get; set; }

    /// <summary>Foreign key to the <see cref="Catalog.Season"/>.</summary>
    public Guid SeasonId { get; set; }

    /// <summary>Navigation to the season.</summary>
    public Season? Season { get; set; }

    /// <summary>Foreign key to the home <see cref="Team"/>.</summary>
    public Guid HomeTeamId { get; set; }

    /// <summary>Navigation to the home team.</summary>
    public Team? HomeTeam { get; set; }

    /// <summary>Foreign key to the away <see cref="Team"/>.</summary>
    public Guid AwayTeamId { get; set; }

    /// <summary>Navigation to the away team.</summary>
    public Team? AwayTeam { get; set; }

    /// <summary>Scheduled kickoff time in UTC.</summary>
    public DateTime KickoffUtc { get; set; }

    /// <summary>Optional venue name.</summary>
    public string? Venue { get; set; }

    /// <summary>Optional round / matchday label.</summary>
    public string? Round { get; set; }

    /// <summary>Optional referee name.</summary>
    public string? Referee { get; set; }

    /// <summary>Current lifecycle status of the match.</summary>
    public MatchStatus Status { get; set; }

    /// <summary>Final home score, or null if not yet available.</summary>
    public int? HomeScore { get; set; }

    /// <summary>Final away score, or null if not yet available.</summary>
    public int? AwayScore { get; set; }

    /// <summary>Half-time home score, or null if not yet available.</summary>
    public int? HomeScoreHalfTime { get; set; }

    /// <summary>Half-time away score, or null if not yet available.</summary>
    public int? AwayScoreHalfTime { get; set; }
}
