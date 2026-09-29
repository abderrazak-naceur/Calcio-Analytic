namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Provider-agnostic representation of a single row of a competition standings table.
/// </summary>
/// <param name="TeamExternalId">The provider-native identifier of the team.</param>
/// <param name="Position">The team's position in the table (1-based).</param>
/// <param name="Played">The number of matches played.</param>
/// <param name="Points">The number of points accrued.</param>
public sealed record ProviderStandingRowDto(
    string TeamExternalId,
    int Position,
    int Played,
    int Points);
