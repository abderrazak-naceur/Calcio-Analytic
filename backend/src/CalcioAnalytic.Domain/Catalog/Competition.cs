using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// A football competition (league or cup) belonging to a country.
/// </summary>
public class Competition : Entity
{
    /// <summary>Competition name (e.g. "Serie A").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Foreign key to the owning <see cref="Country"/>.</summary>
    public Guid CountryId { get; set; }

    /// <summary>Navigation to the owning country.</summary>
    public Country? Country { get; set; }

    /// <summary>Optional competition tier / division level.</summary>
    public int? Tier { get; set; }
}
