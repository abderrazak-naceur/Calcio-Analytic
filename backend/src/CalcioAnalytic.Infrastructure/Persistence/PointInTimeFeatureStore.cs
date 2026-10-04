using CalcioAnalytic.Application.Abstractions.Features;
using CalcioAnalytic.Domain.Features;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Infrastructure.Persistence;

public sealed class PointInTimeFeatureStore : IPointInTimeFeatureStore
{
    private readonly CalcioAnalyticDbContext _db;

    public PointInTimeFeatureStore(CalcioAnalyticDbContext db) => _db = db;

    public Task<MatchFeatureSnapshot?> GetLatestBeforeKickoffAsync(
        Guid matchId,
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        return _db.Set<MatchFeatureSnapshot>()
            .AsNoTracking()
            .Where(x => x.MatchId == matchId && x.FeatureTimestampUtc < cutoffUtc)
            .OrderByDescending(x => x.FeatureTimestampUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(
        MatchFeatureSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        if (snapshot.FeatureTimestampUtc >= snapshot.KickoffUtc)
        {
            throw new ArgumentException(
                "Point-in-time features must be captured strictly before kickoff.",
                nameof(snapshot));
        }

        _db.Set<MatchFeatureSnapshot>().Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);
    }
}