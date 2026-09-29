namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// Summary of the upserts performed by a single catalog ingestion run.
/// Counts reflect entities that were newly created or updated in place;
/// because ingestion is idempotent (keyed on provider external identifiers),
/// re-running the same source data produces update counts without creating
/// duplicate rows.
/// </summary>
/// <param name="CompetitionsUpserted">Number of competitions created or updated.</param>
/// <param name="SeasonsUpserted">Number of seasons created or updated.</param>
/// <param name="TeamsUpserted">Number of teams created or updated.</param>
/// <param name="BookmakersUpserted">Number of bookmakers created or updated.</param>
/// <param name="MarketsUpserted">Number of markets created or updated.</param>
public sealed record CatalogIngestionResult(
    int CompetitionsUpserted,
    int SeasonsUpserted,
    int TeamsUpserted,
    int BookmakersUpserted,
    int MarketsUpserted);
