namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// Application service that ingests provider catalog data (competitions, seasons,
/// teams, bookmakers and markets) into canonical domain entities.
/// </summary>
/// <remarks>
/// Ingestion is idempotent: each provider entity is reconciled against its
/// external identifier through the provider entity map, so running the same
/// source data more than once updates existing rows in place instead of
/// creating duplicates.
/// </remarks>
public interface ICatalogIngestionService
{
    /// <summary>
    /// Ingests the catalog for a provider competition and season, upserting the
    /// competition, its seasons, participating teams, and the provider's
    /// bookmakers and markets.
    /// </summary>
    /// <param name="providerCode">The provider code identifying the source adapter.</param>
    /// <param name="competitionExternalId">The provider-native competition identifier.</param>
    /// <param name="seasonExternalId">The provider-native season identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A summary of the entities created or updated.</returns>
    Task<CatalogIngestionResult> IngestCatalogAsync(
        string providerCode,
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default);
}
