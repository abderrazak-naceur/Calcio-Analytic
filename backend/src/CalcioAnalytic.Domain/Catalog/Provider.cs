using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// An external data provider (e.g. a feed or API source) that supplies
/// catalog, fixture, or odds data into the system.
/// </summary>
public class Provider : Entity
{
    /// <summary>Human-readable provider name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique short code identifying the provider.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Whether the provider is currently enabled for ingestion.</summary>
    public bool IsEnabled { get; set; }
}
