using CalcioAnalytic.Application.Abstractions.Features;
using CalcioAnalytic.Domain.Features;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Infrastructure.Persistence;

/// <summary>
/// Populates point-in-time snapshots exclusively from records timestamped
/// before kickoff. The implementation is intentionally conservative: market
/// probabilities and recent-form features are populated when enough source
/// data exists, while unavailable advanced model values remain null.
/// </summary>
public sealed class FeatureSnapshotPopulationService : IFeatureSnapshotPopulationService
{
    private readonly CalcioAnalyticDbContext _db;

    public FeatureSnapshotPopulationService(CalcioAnalyticDbContext db) => _db = db;

    public async Task<int> PopulateMissingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(batchSize, 1, 1000);
        var matches = await _db.Matches.AsNoTracking()
            .Where(m => m.KickoffUtc <= DateTime.UtcNow)
            .Where(m => !_db.MatchFeatureSnapshots.Any(f => f.MatchId == m.Id))
            .OrderBy(m => m.KickoffUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var match in matches)
        {
            var timestamp = match.KickoffUtc.AddSeconds(-1);
            var recent = await _db.Matches.AsNoTracking()
                .Where(m => m.KickoffUtc < match.KickoffUtc
                    && (m.HomeTeamId == match.HomeTeamId || m.AwayTeamId == match.HomeTeamId
                        || m.HomeTeamId == match.AwayTeamId || m.AwayTeamId == match.AwayTeamId)
                    && m.HomeScore != null && m.AwayScore != null)
                .OrderByDescending(m => m.KickoffUtc)
                .Take(10)
                .ToListAsync(cancellationToken);

            var homeForm = recent.Take(5).Where(m => m.HomeTeamId == match.HomeTeamId || m.AwayTeamId == match.HomeTeamId)
                .Select(m => PointsForTeam(m, match.HomeTeamId)).ToArray();
            var awayForm = recent.Take(5).Where(m => m.HomeTeamId == match.AwayTeamId || m.AwayTeamId == match.AwayTeamId)
                .Select(m => PointsForTeam(m, match.AwayTeamId)).ToArray();

            var market = await BuildMarketProbabilitiesAsync(match.Id, timestamp, cancellationToken);
            var snapshot = new MatchFeatureSnapshot
            {
                Id = Guid.NewGuid(),
                MatchId = match.Id,
                KickoffUtc = match.KickoffUtc,
                FeatureTimestampUtc = timestamp,
                HomeFormLast5Points = homeForm.Length == 0 ? null : homeForm.Sum(),
                AwayFormLast5Points = awayForm.Length == 0 ? null : awayForm.Sum(),
                MarketHomeProbability = market.Home,
                MarketDrawProbability = market.Draw,
                MarketAwayProbability = market.Away,
                HomeClosingOdds = market.HomeOdds,
                DrawClosingOdds = market.DrawOdds,
                AwayClosingOdds = market.AwayOdds,
            };

            _db.MatchFeatureSnapshots.Add(snapshot);
            created++;
        }

        if (created > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return created;
    }

    private async Task<(decimal? Home, decimal? Draw, decimal? Away, decimal? HomeOdds, decimal? DrawOdds, decimal? AwayOdds)>
        BuildMarketProbabilitiesAsync(Guid matchId, DateTime cutoff, CancellationToken ct)
    {
        var rows = await (
            from snapshot in _db.OddsSnapshots.AsNoTracking()
            join line in _db.MarketLines.AsNoTracking() on snapshot.MarketLineId equals line.Id
            join market in _db.Markets.AsNoTracking() on line.MarketId equals market.Id
            join selection in _db.Selections.AsNoTracking() on snapshot.SelectionId equals selection.Id
            where snapshot.MatchId == matchId
                && snapshot.ProviderTimestampUtc < cutoff
                && !snapshot.IsLive
                && market.Code == "1X2"
                && line.Period == "FullTime"
            orderby snapshot.ProviderTimestampUtc descending
            select new { selection.Name, snapshot.DecimalOdds })
            .Take(30)
            .ToListAsync(ct);

        decimal? homeOdds = Find(rows, "home", "1");
        decimal? drawOdds = Find(rows, "draw", "x");
        decimal? awayOdds = Find(rows, "away", "2");
        var implied = new[] { homeOdds, drawOdds, awayOdds }
            .Where(x => x is > 1m)
            .Select(x => 1m / x!.Value)
            .ToArray();
        var total = implied.Sum();
        return total <= 0
            ? (null, null, null, homeOdds, drawOdds, awayOdds)
            : (homeOdds is > 1m ? 1m / homeOdds.Value / total : null,
               drawOdds is > 1m ? 1m / drawOdds.Value / total : null,
               awayOdds is > 1m ? 1m / awayOdds.Value / total : null,
               homeOdds, drawOdds, awayOdds);
    }

    private static decimal? Find<T>(IEnumerable<T> rows, string first, string second)
    {
        var name = typeof(T).GetProperty("Name");
        var odds = typeof(T).GetProperty("DecimalOdds");
        var row = rows.FirstOrDefault(x =>
            string.Equals(name?.GetValue(x)?.ToString(), first, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name?.GetValue(x)?.ToString(), second, StringComparison.OrdinalIgnoreCase));
        return row is null ? null : (decimal?)odds?.GetValue(row);
    }

    private static int PointsForTeam(Match match, Guid teamId)
    {
        var home = match.HomeScore!.Value;
        var away = match.AwayScore!.Value;
        if (home == away) return 1;
        var won = match.HomeTeamId == teamId ? home > away : away > home;
        return won ? 3 : 0;
    }
}
