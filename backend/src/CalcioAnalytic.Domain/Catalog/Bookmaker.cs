using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// A bookmaker that provides odds for markets.
/// </summary>
public class Bookmaker : Entity
{
    /// <summary>Human-readable bookmaker name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique short code identifying the bookmaker.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Whether the bookmaker is currently enabled.</summary>
    public bool IsEnabled { get; set; }
}
