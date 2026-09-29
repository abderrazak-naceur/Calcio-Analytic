namespace CalcioAnalytic.Application.Analytics;

/// <summary>
/// Application-level service that generates a fresh analysis for a match and
/// persists it as a new, immutable versioned row. It loads the match and all of
/// its related data (odds snapshots, market lines, selections, statistics,
/// events, and settlements) from persistence, delegates the analysis to the pure
/// analysis engine, and stores the result.
/// </summary>
/// <remarks>
/// Each successful call produces a brand-new analysis row with a
/// monotonically incremented version; previously stored versions are never
/// mutated, preserving a full audit trail of a match's analyses.
/// </remarks>
public interface IMatchAnalysisPersistenceService
{
    /// <summary>
    /// Generates a new analysis for the given match and stores it as a new
    /// version.
    /// </summary>
    /// <param name="matchId">The identifier of the match to analyze.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// The version number assigned to the newly stored analysis. The first
    /// analysis for a match is version 1, and each subsequent analysis increments
    /// the version by one.
    /// </returns>
    Task<int> GenerateAndStoreAsync(Guid matchId, CancellationToken ct = default);
}
