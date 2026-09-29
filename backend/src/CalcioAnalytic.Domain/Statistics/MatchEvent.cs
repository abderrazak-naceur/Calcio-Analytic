using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Statistics;

/// <summary>
/// A discrete in-match event, such as a goal, card, substitution, VAR review,
/// penalty, or corner.
/// </summary>
public class MatchEvent : Entity
{
    /// <summary>Foreign key to the match the event occurred in.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Type of event (e.g. "goal", "card", "substitution", "var", "penalty", "corner").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Optional match minute at which the event occurred.</summary>
    public int? Minute { get; set; }

    /// <summary>Optional foreign key to the team associated with the event.</summary>
    public Guid? TeamId { get; set; }

    /// <summary>Optional foreign key to the player associated with the event.</summary>
    public Guid? PlayerId { get; set; }

    /// <summary>Optional free-text detail describing the event.</summary>
    public string? Detail { get; set; }
}
