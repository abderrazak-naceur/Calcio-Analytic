namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// Application service that ingests provider odds snapshots for a match into
/// canonical <c>MarketLine</c>, <c>Selection</c> and append-only
/// <c>OddsSnapshot</c> records.
/// </summary>
/// <remarks>
/// Ingestion is idempotent. Snapshots are historical and append-only: each is
/// deduplicated on its payload hash, so running the same source data twice never
/// double-inserts. Market lines and selections are reconciled by their natural
/// keys (match/market/line/period and market-line/name respectively), created
/// once and reused thereafter.
/// </remarks>
public interface IOddsIngestionService
{
    /// <summary>
    /// Ingests the odds snapshots the provider exposes for a single match.
    /// </summary>
    /// <param name="providerCode">The provider code identifying the source adapter.</param>
    /// <param name="matchExternalId">The provider-native match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A summary of the snapshots, market lines and selections processed.</returns>
    Task<OddsIngestionResult> IngestOddsAsync(
        string providerCode,
        string matchExternalId,
        CancellationToken ct = default);
}
