namespace CalcioAnalytic.Application.Ingestion;

/// <summary>
/// Summary of the work performed by a single odds ingestion run for one match.
/// </summary>
/// <remarks>
/// Odds snapshots are append-only historical records. Ingestion is idempotent:
/// each snapshot is deduplicated on its payload hash, so re-running the same
/// source data reports the repeats under <see cref="SnapshotsSkippedDuplicate"/>
/// instead of inserting duplicate rows. Market lines and selections are keyed
/// reconciled (created once, then reused), so their counts reflect rows created
/// or matched during the run.
/// </remarks>
/// <param name="SnapshotsInserted">Number of new odds snapshots appended.</param>
/// <param name="SnapshotsSkippedDuplicate">Number of snapshots skipped because an identical payload hash already existed.</param>
/// <param name="MarketLinesUpserted">Number of market lines created or reused during the run.</param>
/// <param name="SelectionsUpserted">Number of selections created or reused during the run.</param>
public sealed record OddsIngestionResult(
    int SnapshotsInserted,
    int SnapshotsSkippedDuplicate,
    int MarketLinesUpserted,
    int SelectionsUpserted);
