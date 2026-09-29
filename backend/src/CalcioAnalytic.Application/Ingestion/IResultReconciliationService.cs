namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// The outcome of reconciling a match's stored final result against the results
/// reported by its external data providers.
/// </summary>
/// <remarks>
/// <para>
/// Reconciliation is a lifecycle step that runs after a match has finished
/// (and typically after settlement/analysis). It confirms that the canonical,
/// stored score agrees with what every available provider currently reports,
/// and only then advances the match to <see cref="Domain.Matches.MatchStatus.Reconciled"/>.
/// </para>
/// <para>
/// Because the system may currently have a single provider, "cross-provider"
/// reconciliation is structurally supported but degrades gracefully to
/// single-source confirmation. The service never silently overwrites a stored
/// score: when sources disagree it reports a conflict and leaves the match
/// unchanged.
/// </para>
/// </remarks>
/// <param name="MatchId">The canonical internal identifier of the reconciled match.</param>
/// <param name="Confirmed">
/// <c>true</c> when the stored result was confirmed (and the match is now, or was
/// already, <see cref="Domain.Matches.MatchStatus.Reconciled"/>); otherwise <c>false</c>.
/// </param>
/// <param name="Status">
/// A stable, machine-readable outcome code. One of:
/// <list type="bullet">
///   <item><term>Reconciled</term><description>Every available source agreed with the stored score and the match was advanced (or was already reconciled).</description></item>
///   <item><term>Unconfirmed</term><description>No provider sources were available to verify the stored score; nothing was changed.</description></item>
///   <item><term>Conflict</term><description>At least one source disagreed with the stored score; the match was left unchanged.</description></item>
///   <item><term>NotFinished</term><description>The match has not reached a reconcilable lifecycle state.</description></item>
///   <item><term>NotFound</term><description>No match exists for the supplied identifier.</description></item>
/// </list>
/// </param>
/// <param name="Discrepancies">
/// Human-readable descriptions of any mismatches found between a provider's
/// reported result and the stored result. Empty when there are none.
/// </param>
public sealed record ReconciliationResult(
    Guid MatchId,
    bool Confirmed,
    string Status,
    IReadOnlyList<string> Discrepancies);

/// <summary>
/// Application service that reconciles a finished match's stored result against
/// the results reported by its external data providers, advancing the match to
/// <see cref="Domain.Matches.MatchStatus.Reconciled"/> only when every available
/// source agrees.
/// </summary>
/// <remarks>
/// Reconciliation is honest by design: it confirms what it can verify and flags
/// what it cannot. When no sources are available the result is left
/// <em>Unconfirmed</em>; when sources conflict with the stored score the result
/// is a <em>Conflict</em> and the match is never mutated. The operation is
/// idempotent: reconciling an already-reconciled match succeeds without change.
/// </remarks>
public interface IResultReconciliationService
{
    /// <summary>
    /// Reconciles the stored final result of a single match against its provider
    /// sources.
    /// </summary>
    /// <param name="matchId">The canonical internal identifier of the match.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A <see cref="ReconciliationResult"/> describing the outcome.</returns>
    Task<ReconciliationResult> ReconcileMatchAsync(Guid matchId, CancellationToken ct = default);
}
