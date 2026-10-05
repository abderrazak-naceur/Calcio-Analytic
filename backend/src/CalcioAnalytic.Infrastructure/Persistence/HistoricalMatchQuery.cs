using CalcioAnalytic.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Infrastructure.Persistence;

public sealed class HistoricalMatchQuery : IHistoricalMatchQuery
{
    private readonly CalcioAnalyticDbContext _db;

    public HistoricalMatchQuery(CalcioAnalyticDbContext db) => _db = db;

    public async Task<IReadOnlyList<HistoricalMatchCandidate>> FindAsync(
        string providerCode,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        var providerId = await _db.Providers.AsNoTracking()
            .Where(x => x.Code == providerCode)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(ct);

        if (providerId is null)
            return Array.Empty<HistoricalMatchCandidate>();

        return await (
            from map in _db.ProviderEntityMaps.AsNoTracking()
            join match in _db.Matches.AsNoTracking() on map.InternalId equals match.Id
            where map.ProviderId == providerId.Value
                && map.EntityType == "Match"
                && match.KickoffUtc >= fromUtc
                && match.KickoffUtc < toUtc
            orderby match.KickoffUtc
            select new HistoricalMatchCandidate(match.Id, map.ExternalId, match.Status))
            .ToListAsync(ct);
    }
}
