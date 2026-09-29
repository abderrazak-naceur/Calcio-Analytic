using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Settlement;

/// <summary>
/// Records the settled outcome of a selection on a market line for a match.
/// </summary>
public class MarketSettlement : Entity
{
    /// <summary>Foreign key to the match being settled.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Foreign key to the market line being settled.</summary>
    public Guid MarketLineId { get; set; }

    /// <summary>Foreign key to the selection being settled.</summary>
    public Guid SelectionId { get; set; }

    /// <summary>Settlement outcome for the selection.</summary>
    public SettlementStatus Status { get; set; }

    /// <summary>UTC timestamp when settlement occurred, or null if not yet settled.</summary>
    public DateTime? SettledAtUtc { get; set; }
}
