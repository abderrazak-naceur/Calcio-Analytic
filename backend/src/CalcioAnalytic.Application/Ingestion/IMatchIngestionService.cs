namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// Application service that ingests provider match fixtures into canonical
/// <see cref="CalcioAnalytic.Domain.Matches.Match"/> aggregates.
/// </summary>
/// <remarks>
/// Ingestion is idempotent: each fixture is reconciled against its external
/// identifier through the provider entity map, so running the same source data
/// more than once updates the existing canonical match in place instead of
/// creating a duplicate. Referenced catalog entities (competition, season and
/// teams) are resolved-or-created on demand so foreign keys are always valid.
/// </remarks>
public interface IMatchIngestionService
{
    /// <summary>
    /// Ingests all fixtures for a provider competition and season.
    /// </summary>
    /// <param name="providerCode">The provider code identifying the source adapter.</param>
    /// <param name="competitionExternalId">The provider-native competition identifier.</param>
    /// <param name="seasonExternalId">The provider-native season identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>A summary of the matches created or updated.</returns>
    Task<MatchIngestionResult> IngestFixturesAsync(
        string providerCode,
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default);

    /// <summary>
    /// Ingests a single fixture by its provider-native identifier.
    /// </summary>
    /// <param name="providerCode">The provider code identifying the source adapter.</param>
    /// <param name="matchExternalId">The provider-native match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    /// <returns>
    /// The canonical internal <see cref="System.Guid"/> of the match, or
    /// <c>null</c> when the provider has no such fixture.
    /// </returns>
    Task<Guid?> IngestFixtureAsync(
        string providerCode,
        string matchExternalId,
        CancellationToken ct = default);
}
