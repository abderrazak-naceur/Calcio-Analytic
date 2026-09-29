using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IProviderEntityMapRepository"/>.</summary>
public sealed class ProviderEntityMapRepository : IProviderEntityMapRepository
{
    private readonly CalcioAnalyticDbContext _db;

    public ProviderEntityMapRepository(CalcioAnalyticDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task<ProviderEntityMap?> FindAsync(
        Guid providerId,
        string entityType,
        string externalId,
        CancellationToken ct = default)
        => _db.ProviderEntityMaps.FirstOrDefaultAsync(
            m => m.ProviderId == providerId
                 && m.EntityType == entityType
                 && m.ExternalId == externalId,
            ct);

    /// <inheritdoc />
    public async Task<Guid?> ResolveInternalIdAsync(
        Guid providerId,
        string entityType,
        string externalId,
        CancellationToken ct = default)
    {
        var map = await _db.ProviderEntityMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.ProviderId == providerId
                     && m.EntityType == entityType
                     && m.ExternalId == externalId,
                ct);

        return map?.InternalId;
    }

    /// <inheritdoc />
    public async Task AddAsync(ProviderEntityMap map, CancellationToken ct = default)
        => await _db.ProviderEntityMaps.AddAsync(map, ct);
}
