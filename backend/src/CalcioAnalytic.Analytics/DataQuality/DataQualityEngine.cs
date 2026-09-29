using CalcioAnalytic.Domain.Matches;

namespace CalcioAnalytic.Analytics.DataQuality;

/// <summary>
/// Default <see cref="IDataQualityEngine"/>. Runs a fixed battery of integrity
/// checks over a pre-loaded <see cref="MatchQualityInput"/> and rolls the results
/// up into a single severity-weighted quality score. The engine is pure and
/// deterministic: identical input and <c>nowUtc</c> always yield an identical
/// report, and the checks always appear in the same order.
/// </summary>
/// <remarks>
/// <para><b>Scoring.</b> Each check carries a severity weight:
/// <c>Error = 1.0</c>, <c>Warning = 0.5</c>, <c>Info = 0.1</c>. The total weight is
/// the sum of the weights of <em>all</em> checks; the weighted failure is the sum
/// of the weights of the checks that flagged a problem. The score is
/// <c>1 - (weightedFailures / totalWeight)</c>, clamped to the range [0, 1]. When
/// no checks exist the score defaults to <c>1</c>. A report with no failures
/// scores <c>1</c>; a report where every check fails scores <c>0</c>.</para>
/// </remarks>
public sealed class DataQualityEngine : IDataQualityEngine
{
    /// <summary>The scoring methodology version stamped on every report.</summary>
    public const string Methodology = "dq-1.0.0";

    private const decimal ErrorWeight = 1.0m;
    private const decimal WarningWeight = 0.5m;
    private const decimal InfoWeight = 0.1m;

    private const string SeverityInfo = "Info";
    private const string SeverityWarning = "Warning";
    private const string SeverityError = "Error";

    /// <summary>How far in the past a scheduled kickoff may be before it is stale.</summary>
    private static readonly TimeSpan StaleScheduledThreshold = TimeSpan.FromDays(2);

    /// <inheritdoc />
    public DataQualityReport EvaluateMatch(MatchQualityInput input, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(input);

        var match = input.Match;
        ArgumentNullException.ThrowIfNull(match);

        var finished = IsFinished(match.Status);

        var checks = new List<DataQualityCheck>(8)
        {
            // MissingResult (Error): a finished match must have both final scores.
            MakeCheck(
                "MissingResult",
                SeverityError,
                failed: finished && (match.HomeScore is null || match.AwayScore is null),
                failMessage: "Finished match is missing a home and/or away score.",
                passMessage: "Match result is present or the match is not finished."),

            // NoOdds (Warning): a finished match with no captured odds snapshots.
            MakeCheck(
                "NoOdds",
                SeverityWarning,
                failed: finished && input.OddsSnapshotCount == 0,
                failMessage: "Finished match has no odds snapshots.",
                passMessage: "Match has odds snapshots or is not finished."),

            // ImpossibleOdds (Error): decimal odds must be strictly greater than 1.
            MakeCheck(
                "ImpossibleOdds",
                SeverityError,
                failed: input.MinDecimalOdds is decimal min && min <= 1.0m,
                failMessage: "Match has decimal odds less than or equal to 1.0, which is impossible.",
                passMessage: "All decimal odds are greater than 1.0 (or no odds exist)."),

            // DuplicateOdds (Error): duplicate payload hashes across snapshots.
            MakeCheck(
                "DuplicateOdds",
                SeverityError,
                failed: input.HasDuplicateOddsHash,
                failMessage: "Match has duplicate odds snapshots (repeated payload hash).",
                passMessage: "No duplicate odds snapshots detected."),

            // NoStatistics (Info): the match has no recorded statistics.
            MakeCheck(
                "NoStatistics",
                SeverityInfo,
                failed: input.StatisticCount == 0,
                failMessage: "Match has no statistics recorded.",
                passMessage: "Match has statistics recorded."),

            // NotSettled (Warning): a finished match without any settlements.
            MakeCheck(
                "NotSettled",
                SeverityWarning,
                failed: finished && input.SettlementCount == 0,
                failMessage: "Finished match has not been settled.",
                passMessage: "Match has settlements or is not finished."),

            // NotAnalyzed (Info): a finished match without any analyses.
            MakeCheck(
                "NotAnalyzed",
                SeverityInfo,
                failed: finished && input.AnalysisCount == 0,
                failMessage: "Finished match has not been analyzed.",
                passMessage: "Match has an analysis or is not finished."),

            // StaleScheduled (Info): still Scheduled but kickoff is well in the past.
            MakeCheck(
                "StaleScheduled",
                SeverityInfo,
                failed: match.Status == MatchStatus.Scheduled
                    && match.KickoffUtc < nowUtc - StaleScheduledThreshold,
                failMessage: "Match is still Scheduled but its kickoff is more than two days in the past.",
                passMessage: "Match schedule is current or the match is not Scheduled."),
        };

        var failed = 0;
        var totalWeight = 0m;
        var weightedFailures = 0m;

        foreach (var check in checks)
        {
            var weight = WeightFor(check.Severity);
            totalWeight += weight;

            if (check.Count > 0)
            {
                failed++;
                weightedFailures += weight;
            }
        }

        var score = totalWeight == 0m
            ? 1m
            : Clamp01(1m - (weightedFailures / totalWeight));

        return new DataQualityReport(
            match.Id,
            score,
            checks.Count,
            failed,
            checks,
            Methodology);
    }

    /// <summary>
    /// Builds a <see cref="DataQualityCheck"/> from a boolean condition, using
    /// <paramref name="failed"/> to pick the message and set the count (1 when the
    /// check flags a problem, otherwise 0).
    /// </summary>
    private static DataQualityCheck MakeCheck(
        string code,
        string severity,
        bool failed,
        string failMessage,
        string passMessage)
        => new(code, severity, failed ? failMessage : passMessage, failed ? 1 : 0);

    /// <summary>Maps a severity label to its scoring weight.</summary>
    private static decimal WeightFor(string severity) => severity switch
    {
        SeverityError => ErrorWeight,
        SeverityWarning => WarningWeight,
        SeverityInfo => InfoWeight,
        _ => 0m,
    };

    /// <summary>
    /// A match is "finished" once play is complete: status
    /// <see cref="MatchStatus.Finished"/> or any later lifecycle state
    /// (settlement, analysis, reconciliation).
    /// </summary>
    private static bool IsFinished(MatchStatus status) => status
        is MatchStatus.Finished
        or MatchStatus.SettlementPending
        or MatchStatus.Analyzed
        or MatchStatus.Reconciled;

    /// <summary>Clamps a value to the range [0, 1].</summary>
    private static decimal Clamp01(decimal value) => value < 0m ? 0m : value > 1m ? 1m : value;
}
