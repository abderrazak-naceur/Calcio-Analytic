using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Catalog;

/// <summary>
/// Maps an internal domain entity to a provider's external identifier,
/// enabling cross-provider identity reconciliation without leaking
/// provider-specific DTOs into the domain.
/// </summary>
public class ProviderEntityMap : Entity
{
    /// <summary>Foreign key to the <see cref="Provider"/> that owns the external id.</summary>
    public Guid ProviderId { get; set; }

    /// <summary>The internal entity type being mapped (e.g. "Team", "Match").</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Identifier of the internal domain entity.</summary>
    public Guid InternalId { get; set; }

    /// <summary>The provider's external identifier for the entity.</summary>
    public string ExternalId { get; set; } = string.Empty;
}
