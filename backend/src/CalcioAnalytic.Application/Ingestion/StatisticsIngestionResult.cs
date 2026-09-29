namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// Summary of the work performed by a single statistics-and-events ingestion run
/// for one match.
/// </summary>
/// <remarks>
/// Ingestion is idempotent. A statistic is keyed by (match, team, name): an
/// existing row is updated in place rather than duplicated. An event is keyed by
/// (match, type, minute, team): an identical event is skipped so the timeline is
/// not duplicated on re-run. Counts reflect rows newly inserted during the run.
/// </remarks>
/// <param name="StatisticsInserted">Number of match statistics newly inserted (excludes in-place updates).</param>
/// <param name="EventsInserted">Number of match events newly inserted (excludes skipped duplicates).</param>
public sealed record StatisticsIngestionResult(
    int StatisticsInserted,
    int EventsInserted);
