using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// A football team / club.
/// </summary>
public class Team : Entity
{
    /// <summary>Full team name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional short or abbreviated name.</summary>
    public string? ShortName { get; set; }

    /// <summary>Optional foreign key to the team's <see cref="Country"/>.</summary>
    public Guid? CountryId { get; set; }

    /// <summary>Navigation to the team's country.</summary>
    public Country? Country { get; set; }
}
