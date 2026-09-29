namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// Summary of the match upserts performed by a single fixture ingestion run.
/// Because ingestion is idempotent (keyed on provider external identifiers),
/// re-running the same source data updates existing canonical matches in place
/// rather than creating duplicates.
/// </summary>
/// <param name="MatchesUpserted">Number of matches created or updated.</param>
/// <param name="MatchIds">The canonical internal identifiers of the affected matches.</param>
public sealed record MatchIngestionResult(
    int MatchesUpserted,
    IReadOnlyList<Guid> MatchIds);
