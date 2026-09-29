using CalcioAnalytic.Analytics.Markets;
using CalcioAnalytic.Analytics.Odds;

namespace CalcioAnalytic.Analytics.Match;

/// <summary>
/// The 1X2 outcome of a match derived from the final score.
/// </summary>
public enum MatchOutcome
{
    /// <summary>Outcome is not yet known (no final score).</summary>
    Unknown,

    /// <summary>The home team won ("1").</summary>
    HomeWin,

    /// <summary>The match was drawn ("X").</summary>
    Draw,

    /// <summary>The away team won ("2").</summary>
    AwayWin
}

/// <summary>
/// The result summary of a match: identity, final score, and the derived 1X2
/// outcome.
/// </summary>
/// <param name="MatchId">The match analyzed.</param>
/// <param name="HomeTeamId">The home team.</param>
/// <param name="AwayTeamId">The away team.</param>
/// <param name="HomeScore">Final home score, or null if unavailable.</param>
/// <param name="AwayScore">Final away score, or null if unavailable.</param>
/// <param name="Outcome">Derived 1X2 outcome.</param>
public sealed record MatchResultSummary(
    Guid MatchId,
    Guid HomeTeamId,
    Guid AwayTeamId,
    int? HomeScore,
    int? AwayScore,
    MatchOutcome Outcome);

/// <summary>
/// The odds movement summary for a single selection within the match, keyed by
/// the tuple that scopes the movement.
/// </summary>
/// <param name="BookmakerId">The bookmaker.</param>
/// <param name="MarketLineId">The market line.</param>
/// <param name="SelectionId">The selection.</param>
/// <param name="Movement">The computed movement result.</param>
public sealed record SelectionMovementSummary(
    Guid BookmakerId,
    Guid MarketLineId,
    Guid SelectionId,
    OddsMovementResult Movement);

/// <summary>
/// The cross-bookmaker dispersion summary for a single selection within the match.
/// </summary>
/// <param name="MarketLineId">The market line.</param>
/// <param name="SelectionId">The selection.</param>
/// <param name="Dispersion">The computed dispersion result.</param>
public sealed record SelectionDispersionSummary(
    Guid MarketLineId,
    Guid SelectionId,
    BookmakerDispersionResult Dispersion);

/// <summary>
/// A single aggregated statistic across the match (summed over teams / values),
/// included in the report's statistics summary.
/// </summary>
/// <param name="Name">The statistic name (e.g. "Shots").</param>
/// <param name="TotalValue">Sum of the statistic's values across the match.</param>
public sealed record StatisticSummary(string Name, decimal TotalValue);

/// <summary>
/// A count of match events grouped by type, included in the statistics summary.
/// </summary>
/// <param name="Type">The event type (e.g. "goal", "card").</param>
/// <param name="Count">Number of events of that type.</param>
public sealed record EventTypeCount(string Type, int Count);

/// <summary>
/// A high-level summary of a match's statistics and events.
/// </summary>
/// <param name="Statistics">Aggregated named statistics.</param>
/// <param name="EventCounts">Event counts grouped by type.</param>
/// <param name="TotalEvents">Total number of events considered.</param>
public sealed record StatisticsSummary(
    IReadOnlyList<StatisticSummary> Statistics,
    IReadOnlyList<EventTypeCount> EventCounts,
    int TotalEvents);

/// <summary>
/// The complete, immutable analysis of a single match. This record is the
/// canonical shape serialized to
/// <see cref="CalcioAnalytic.Domain.Analytics.MatchAnalysis.AnalysisJson"/>.
/// It aggregates the outputs of the odds, market, bookmaker, and statistics
/// analyzers.
/// </summary>
/// <param name="Result">The 1X2 result summary.</param>
/// <param name="Markets">Per-market-line analysis (overround, probabilities).</param>
/// <param name="OddsMovements">Per-selection odds movement summaries.</param>
/// <param name="BookmakerDispersions">Per-selection cross-bookmaker dispersion.</param>
/// <param name="Statistics">Aggregated statistics and event summary.</param>
/// <param name="MethodologyVersion">Version of the analysis methodology used.</param>
/// <param name="GeneratedAtUtc">UTC timestamp the report was generated.</param>
public sealed record MatchAnalysisReport(
    MatchResultSummary Result,
    IReadOnlyList<MarketAnalysisResult> Markets,
    IReadOnlyList<SelectionMovementSummary> OddsMovements,
    IReadOnlyList<SelectionDispersionSummary> BookmakerDispersions,
    StatisticsSummary Statistics,
    string MethodologyVersion,
    DateTime GeneratedAtUtc);
