using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Domain.Settlement;
using CalcioAnalytic.Domain.Statistics;
using DomainMatch = CalcioAnalytic.Domain.Matches.Match;

namespace CalcioAnalytic.Analytics.Match;

/// <summary>
/// A fully-populated, in-memory input model for match analysis. The caller is
/// responsible for loading every collection from persistence; the engine
/// performs no data access. This keeps the engine pure, deterministic, and
/// unit-testable.
/// </summary>
/// <param name="Match">The match being analyzed.</param>
/// <param name="OddsSnapshots">All odds snapshots captured for the match.</param>
/// <param name="MarketLines">All market lines for the match.</param>
/// <param name="Selections">All selections across the match's market lines.</param>
/// <param name="BookmakerIds">The distinct bookmakers that priced the match.</param>
/// <param name="MatchStatistics">All statistics recorded for the match.</param>
/// <param name="MatchEvents">All in-match events recorded for the match.</param>
/// <param name="MarketSettlements">All market settlements for the match.</param>
public sealed record MatchAnalysisInput(
    DomainMatch Match,
    IReadOnlyList<OddsSnapshot> OddsSnapshots,
    IReadOnlyList<MarketLine> MarketLines,
    IReadOnlyList<Selection> Selections,
    IReadOnlyList<Guid> BookmakerIds,
    IReadOnlyList<MatchStatistic> MatchStatistics,
    IReadOnlyList<MatchEvent> MatchEvents,
    IReadOnlyList<MarketSettlement> MarketSettlements);

/// <summary>
/// The result of running the analysis engine: the structured report together
/// with its serialized JSON form (suitable for persisting as
/// <see cref="CalcioAnalytic.Domain.Analytics.MatchAnalysis.AnalysisJson"/>) and
/// the methodology version.
/// </summary>
/// <param name="Report">The structured analysis report.</param>
/// <param name="AnalysisJson">The report serialized to JSON.</param>
/// <param name="MethodologyVersion">The methodology version used.</param>
public sealed record MatchAnalysisOutput(
    MatchAnalysisReport Report,
    string AnalysisJson,
    string MethodologyVersion);

/// <summary>
/// Orchestrates the odds, market, bookmaker, and statistics analyzers to produce
/// a complete <see cref="MatchAnalysisReport"/> for a match. Implementations must
/// be pure and deterministic: the same input (and same generation timestamp)
/// yields the same output.
/// </summary>
public interface IMatchAnalysisEngine
{
    /// <summary>
    /// Analyzes a match and returns the structured report only.
    /// </summary>
    /// <param name="input">The fully-populated input model.</param>
    /// <param name="generatedAtUtc">
    /// Optional generation timestamp for reproducibility. When null, the current
    /// UTC time is used.
    /// </param>
    /// <returns>The analysis report.</returns>
    MatchAnalysisReport Analyze(MatchAnalysisInput input, DateTime? generatedAtUtc = null);

    /// <summary>
    /// Analyzes a match and returns both the report and its serialized JSON form.
    /// </summary>
    /// <param name="input">The fully-populated input model.</param>
    /// <param name="generatedAtUtc">
    /// Optional generation timestamp for reproducibility. When null, the current
    /// UTC time is used.
    /// </param>
    /// <returns>The report, its JSON, and the methodology version.</returns>
    MatchAnalysisOutput AnalyzeToJson(MatchAnalysisInput input, DateTime? generatedAtUtc = null);
}
