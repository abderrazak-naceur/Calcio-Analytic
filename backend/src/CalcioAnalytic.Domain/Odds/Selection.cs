using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Odds;

/// <summary>
/// A selectable outcome within a <see cref="MarketLine"/>, such as "Home",
/// "Draw", "Over", or "Under".
/// </summary>
public class Selection : Entity
{
    /// <summary>Foreign key to the owning <see cref="MarketLine"/>.</summary>
    public Guid MarketLineId { get; set; }

    /// <summary>Human-readable name of the selection (e.g. "Home", "Over").</summary>
    public string Name { get; set; } = string.Empty;
}
