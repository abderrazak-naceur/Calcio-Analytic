namespace CalcioAnalytic.Application.Settlement;

/// <summary>
/// Application-level service that settles all supported markets for a finished
/// match: it loads the match and its market lines from persistence, delegates
/// the outcome logic to the pure settlement engine, and persists the resulting
/// settlements.
/// </summary>
/// <remarks>
/// This interface intentionally only declares the contract. The concrete
/// implementation — which performs data access and orchestrates the engine —
/// belongs to a later phase and lives in the infrastructure/ingestion layer.
/// It is defined here so that layer can be wired up against a stable contract.
/// </remarks>
public interface ISettlementService
{
    /// <summary>
    /// Settles every supported market for the given match.
    /// </summary>
    /// <param name="matchId">The identifier of the match to settle.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>The number of selections that were settled.</returns>
    Task<int> SettleMatchAsync(Guid matchId, CancellationToken ct = default);
}
