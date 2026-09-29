using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// A single season of a competition (e.g. "2024/2025").
/// </summary>
public class Season : Entity
{
    /// <summary>Foreign key to the owning <see cref="Competition"/>.</summary>
    public Guid CompetitionId { get; set; }

    /// <summary>Navigation to the owning competition.</summary>
    public Competition? Competition { get; set; }

    /// <summary>Display label for the season (e.g. "2024/2025").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Optional season start date.</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Optional season end date.</summary>
    public DateOnly? EndDate { get; set; }
}
