using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Services;

/// <summary>
/// Loads finished matches together with their 1X2 "Home" odds snapshots from the
/// database (read-only) so controllers can project them via
/// <see cref="MatchFeatureProjector"/>. All reads use <c>AsNoTracking</c> and are
/// asynchronous. This isolates the persistence queries from the controllers,
/// keeping them thin.
/// </summary>
public sealed class MatchFeatureLoader
{
    private readonly CalcioAnalyticDbContext _db;

    public MatchFeatureLoader(CalcioAnalyticDbContext db) => _db = db;

    /// <summary>
    /// A finished match paired with its 1X2 "Home" odds snapshots (possibly empty).
    /// </summary>
    /// <param name="Match">The finished match.</param>
    /// <param name="HomeSnapshots">The 1X2 "Home" odds snapshots for the match.</param>
    public sealed record MatchWithHomeOdds(Match Match, IReadOnlyList<OddsSnapshot> HomeSnapshots);

    /// <summary>
    /// Loads every finished match and its 1X2 "Home" odds snapshots. A match is
    /// "finished" when its status is <see cref="MatchStatus.Finished"/> or a later
    /// lifecycle state (settlement/analysis/reconciliation), i.e. play is complete.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    public async Task<IReadOnlyList<MatchWithHomeOdds>> LoadFinishedAsync(CancellationToken ct)
    {
        var matches = await _db.Matches.AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished
                || m.Status == MatchStatus.SettlementPending
                || m.Status == MatchStatus.Analyzed
                || m.Status == MatchStatus.Reconciled)
            .ToListAsync(ct);

        if (matches.Count == 0)
        {
            return Array.Empty<MatchWithHomeOdds>();
        }

        var matchIds = matches.Select(m => m.Id).ToList();
        var byMatch = await LoadHomeSnapshotsByMatchAsync(matchIds, ct);

        return matches
            .Select(m => new MatchWithHomeOdds(
                m,
                byMatch.TryGetValue(m.Id, out var snaps) ? snaps : Array.Empty<OddsSnapshot>()))
            .ToList();
    }

    /// <summary>
    /// Loads a single match and its 1X2 "Home" odds snapshots, or null when the
    /// match does not exist.
    /// </summary>
    /// <param name="matchId">The match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    public async Task<MatchWithHomeOdds?> LoadByIdAsync(Guid matchId, CancellationToken ct)
    {
        var match = await _db.Matches.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match is null)
        {
            return null;
        }

        var byMatch = await LoadHomeSnapshotsByMatchAsync(new[] { matchId }, ct);
        var snaps = byMatch.TryGetValue(matchId, out var found) ? found : Array.Empty<OddsSnapshot>();

        return new MatchWithHomeOdds(match, snaps);
    }

    /// <summary>
    /// Loads the 1X2 "Home" odds snapshots for the given matches, grouped by match
    /// id. Resolves the 1X2 market by name/code and the "Home" selection by name
    /// (both case-insensitive), then joins snapshots on the resulting selection
    /// ids restricted to the requested matches.
    /// </summary>
    private async Task<Dictionary<Guid, IReadOnlyList<OddsSnapshot>>> LoadHomeSnapshotsByMatchAsync(
        IReadOnlyCollection<Guid> matchIds,
        CancellationToken ct)
    {
        // Market ids for the 1X2 market (by name or code).
        var oneXTwoMarketIds = await _db.Markets.AsNoTracking()
            .Where(m => m.Name == MatchFeatureProjector.OneXTwoMarketToken
                || m.Code == MatchFeatureProjector.OneXTwoMarketToken)
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (oneXTwoMarketIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<OddsSnapshot>>();
        }

        // Market lines for those markets, restricted to the requested matches.
        var lineIds = await _db.MarketLines.AsNoTracking()
            .Where(l => matchIds.Contains(l.MatchId) && oneXTwoMarketIds.Contains(l.MarketId))
            .Select(l => l.Id)
            .ToListAsync(ct);

        if (lineIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<OddsSnapshot>>();
        }

        // "Home" selections on those lines.
        var homeSelectionIds = await _db.Selections.AsNoTracking()
            .Where(sel => lineIds.Contains(sel.MarketLineId)
                && sel.Name.ToLower() == MatchFeatureProjector.HomeSelectionName.ToLower())
            .Select(sel => sel.Id)
            .ToListAsync(ct);

        if (homeSelectionIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<OddsSnapshot>>();
        }

        // Snapshots for those selections, restricted to the requested matches.
        var snapshots = await _db.OddsSnapshots.AsNoTracking()
            .Where(s => matchIds.Contains(s.MatchId) && homeSelectionIds.Contains(s.SelectionId))
            .ToListAsync(ct);

        return snapshots
            .GroupBy(s => s.MatchId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<OddsSnapshot>)g.ToList());
    }
}
