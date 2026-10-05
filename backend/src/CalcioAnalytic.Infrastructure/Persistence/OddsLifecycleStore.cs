using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Domain.Odds;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Infrastructure.Persistence;

public sealed class OddsLifecycleStore : IOddsLifecycleStore
{
    private readonly CalcioAnalyticDbContext _db;

    public OddsLifecycleStore(CalcioAnalyticDbContext db) => _db = db;

    public async Task RefreshAsync(Guid matchId, CancellationToken ct = default)
    {
        var match = await _db.Matches.AsNoTracking()
            .Where(x => x.Id == matchId)
            .Select(x => new { x.Id, x.KickoffUtc })
            .SingleOrDefaultAsync(ct);

        if (match is null)
        {
            return;
        }

        var snapshots = await _db.OddsSnapshots.AsNoTracking()
            .Where(x => x.MatchId == matchId)
            .OrderBy(x => x.ProviderTimestampUtc)
            .ToListAsync(ct);

        var existing = await _db.Set<OddsLifecycleSummary>()
            .Where(x => x.MatchId == matchId)
            .ToListAsync(ct);
        var existingByKey = existing.ToDictionary(
            x => (x.ProviderId, x.BookmakerId, x.MarketLineId, x.SelectionId));

        foreach (var group in snapshots.GroupBy(x => (x.ProviderId, x.BookmakerId, x.MarketLineId, x.SelectionId)))
        {
            var ordered = group.OrderBy(x => x.ProviderTimestampUtc).ToArray();
            var preKickoff = ordered
                .Where(x => !x.IsLive && x.ProviderTimestampUtc < match.KickoffUtc)
                .ToArray();
            var current = ordered[^1];
            var opening = preKickoff.FirstOrDefault() ?? ordered[0];
            var latestPreKickoff = preKickoff.LastOrDefault();

            var summary = existingByKey.GetValueOrDefault(group.Key);
            if (summary is null)
            {
                summary = new OddsLifecycleSummary
                {
                    Id = Guid.NewGuid(),
                    MatchId = matchId,
                    ProviderId = group.Key.ProviderId,
                    BookmakerId = group.Key.BookmakerId,
                    MarketLineId = group.Key.MarketLineId,
                    SelectionId = group.Key.SelectionId
                };
                await _db.Set<OddsLifecycleSummary>().AddAsync(summary, ct);
            }

            summary.OpeningOdds = opening.DecimalOdds;
            summary.OpeningTimestampUtc = opening.ProviderTimestampUtc;
            summary.CurrentOdds = current.DecimalOdds;
            summary.CurrentTimestampUtc = current.ProviderTimestampUtc;
            summary.PreKickoffOdds = latestPreKickoff?.DecimalOdds;
            summary.PreKickoffTimestampUtc = latestPreKickoff?.ProviderTimestampUtc;
            summary.ClosingOdds = latestPreKickoff?.DecimalOdds;
            summary.ClosingTimestampUtc = latestPreKickoff?.ProviderTimestampUtc;
            summary.MinOdds = preKickoff.Length == 0 ? null : preKickoff.Min(x => x.DecimalOdds);
            summary.MaxOdds = preKickoff.Length == 0 ? null : preKickoff.Max(x => x.DecimalOdds);
        }

        await _db.SaveChangesAsync(ct);
    }
}
