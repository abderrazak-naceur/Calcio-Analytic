using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Odds;

/// <summary>
/// A specific line of a betting market for a given match, such as a particular
/// handicap or total. A market may have several lines (e.g. Over/Under 2.5, 3.5).
/// </summary>
public class MarketLine : Entity
{
    /// <summary>Foreign key to the match this line belongs to.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Foreign key to the catalog market this line specializes.</summary>
    public Guid MarketId { get; set; }

    /// <summary>Optional numeric line value (e.g. 2.5 for a total), or null when not applicable.</summary>
    public decimal? Line { get; set; }

    /// <summary>Optional period the line applies to (e.g. "FullTime", "FirstHalf").</summary>
    public string? Period { get; set; }
}
