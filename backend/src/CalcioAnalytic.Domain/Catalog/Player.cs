using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// A football player, optionally associated with a team.
/// </summary>
public class Player : Entity
{
    /// <summary>Player's full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Optional foreign key to the player's current <see cref="Team"/>.</summary>
    public Guid? TeamId { get; set; }

    /// <summary>Navigation to the player's team.</summary>
    public Team? Team { get; set; }

    /// <summary>Optional playing position.</summary>
    public string? Position { get; set; }

    /// <summary>Optional date of birth.</summary>
    public DateOnly? BirthDate { get; set; }
}
