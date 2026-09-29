using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// A country used to scope competitions, teams, and players.
/// </summary>
public class Country : Entity
{
    /// <summary>Country name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional ISO country code (e.g. "IT", "GB").</summary>
    public string? IsoCode { get; set; }
}
