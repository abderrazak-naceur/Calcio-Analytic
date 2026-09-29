using CalcioAnalytic.Domain.Catalog;

namespace CalcioAnalytic.Application.Abstractions.Persistence;

/// <summary>
/// Persistence port for reconciling internal domain entities against a
/// provider's external identifiers. Enables cross-provider identity mapping
/// without leaking provider-specific concerns into the domain.
/// </summary>
public interface IProviderEntityMapRepository
{
    /// <summary>
    /// Finds the mapping for a given provider, entity type, and external id,
    /// or null if no mapping exists.
    /// </summary>
    /// <param name="providerId">Identifier of the owning provider.</param>
    /// <param name="entityType">The internal entity type (e.g. "Team", "Match").</param>
    /// <param name="externalId">The provider's external identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<ProviderEntityMap?> FindAsync(Guid providerId, string entityType, string externalId, CancellationToken ct = default);

    /// <summary>
    /// Resolves the internal domain entity identifier for a provider's external
    /// identifier, or null if no mapping exists.
    /// </summary>
    /// <param name="providerId">Identifier of the owning provider.</param>
    /// <param name="entityType">The internal entity type (e.g. "Team", "Match").</param>
    /// <param name="externalId">The provider's external identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<Guid?> ResolveInternalIdAsync(Guid providerId, string entityType, string externalId, CancellationToken ct = default);

    /// <summary>Stages a new provider-to-internal identity mapping for insertion.</summary>
    /// <param name="map">The mapping to add.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task AddAsync(ProviderEntityMap map, CancellationToken ct = default);
}
