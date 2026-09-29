using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// A betting market definition (e.g. "1X2", "Over/Under").
/// </summary>
public class Market : Entity
{
    /// <summary>Market name (e.g. "1X2", "Over/Under").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique code identifying the market.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Optional descriptive text for the market.</summary>
    public string? Description { get; set; }
}
