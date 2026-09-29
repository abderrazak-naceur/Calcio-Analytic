namespace CalcioAnalytic.Analytics.DataQuality;

/// <summary>
/// The result of a single data-quality check run against a match. A check
/// describes a specific integrity concern; it is considered <em>failed</em> when
/// it flags a problem (its <see cref="Count"/> reflects how many items the check
/// touched, typically 1 for a boolean condition).
/// </summary>
/// <param name="Code">Stable machine-readable identifier for the check (e.g. "MissingResult").</param>
/// <param name="Severity">Severity classification: "Info", "Warning", or "Error".</param>
/// <param name="Message">Human-readable explanation of the check outcome.</param>
/// <param name="Count">Number of items the check flagged; 0 when the check passed.</param>
public sealed record DataQualityCheck(string Code, string Severity, string Message, int Count);

/// <summary>
/// An immutable, deterministic data-quality report for a single match. The
/// <see cref="Score"/> summarizes overall quality on a [0, 1] scale using the
/// severity-weighted scoring documented on <see cref="DataQualityEngine"/>.
/// </summary>
/// <param name="MatchId">The match the report describes, or null when not tied to a specific match.</param>
/// <param name="Score">Overall quality score in the range [0, 1]; 1 means no checks failed.</param>
/// <param name="TotalChecks">Total number of checks evaluated.</param>
/// <param name="Failed">Number of checks that flagged a problem.</param>
/// <param name="Checks">The individual check results, in a stable order.</param>
/// <param name="Methodology">Version identifier of the scoring methodology used.</param>
public sealed record DataQualityReport(
    Guid? MatchId,
    decimal Score,
    int TotalChecks,
    int Failed,
    IReadOnlyList<DataQualityCheck> Checks,
    string Methodology);

/// <summary>
/// A pure, self-contained snapshot of everything the data-quality checks need to
/// evaluate a single match. The engine performs no data access; the caller is
/// responsible for loading the match and computing the related counts and
/// aggregates (including <see cref="HasDuplicateOddsHash"/> and the min/max
/// decimal odds) so evaluation stays deterministic and side-effect free.
/// </summary>
/// <param name="Match">The match under evaluation.</param>
/// <param name="OddsSnapshotCount">Number of odds snapshots recorded for the match.</param>
/// <param name="MarketLineCount">Number of market lines recorded for the match.</param>
/// <param name="SelectionCount">Number of selections recorded for the match (across its market lines).</param>
/// <param name="StatisticCount">Number of match statistics recorded for the match.</param>
/// <param name="EventCount">Number of match events recorded for the match.</param>
/// <param name="SettlementCount">Number of market settlements recorded for the match.</param>
/// <param name="AnalysisCount">Number of stored analyses for the match.</param>
/// <param name="HasDuplicateOddsHash">
/// True when the match has two or more odds snapshots sharing the same payload
/// hash, i.e. the snapshot count exceeds the count of distinct payload hashes.
/// </param>
/// <param name="MinDecimalOdds">The lowest decimal odds across the match's snapshots, or null when none exist.</param>
/// <param name="MaxDecimalOdds">The highest decimal odds across the match's snapshots, or null when none exist.</param>
public sealed record MatchQualityInput(
    Domain.Matches.Match Match,
    int OddsSnapshotCount,
    int MarketLineCount,
    int SelectionCount,
    int StatisticCount,
    int EventCount,
    int SettlementCount,
    int AnalysisCount,
    bool HasDuplicateOddsHash,
    decimal? MinDecimalOdds,
    decimal? MaxDecimalOdds);
