namespace CalcioAnalytic.Application.Abstractions.Persistence;

/// <summary>
/// Rebuilds the persisted odds lifecycle read model for one match from its
/// append-only snapshots.
/// </summary>
public interface IOddsLifecycleStore
{
    Task RefreshAsync(Guid matchId, CancellationToken ct = default);
}
