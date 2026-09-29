using System.Text.Json;
using CalcioAnalytic.Analytics.Markets;
using CalcioAnalytic.Analytics.Odds;
using CalcioAnalytic.Domain.Odds;

namespace CalcioAnalytic.Analytics.Match;

/// <summary>
/// Default <see cref="IMatchAnalysisEngine"/>. Orchestrates the odds, market,
/// bookmaker, and statistics analyzers over a fully-populated
/// <see cref="MatchAnalysisInput"/> to produce an immutable
/// <see cref="MatchAnalysisReport"/>. The engine is pure and deterministic: for a
/// given input and generation timestamp it always produces the same report.
/// </summary>
public sealed class MatchAnalysisEngine : IMatchAnalysisEngine
{
    /// <summary>The methodology version stamped on every report this engine produces.</summary>
    public const string MethodologyVersion = "1.0.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
    };

    /// <inheritdoc />
    public MatchAnalysisReport Analyze(MatchAnalysisInput input, DateTime? generatedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        var result = BuildResultSummary(input);
        var markets = BuildMarketAnalyses(input);
        var movements = BuildMovements(input);
        var dispersions = BuildDispersions(input);
        var statistics = BuildStatistics(input);

        return new MatchAnalysisReport(
            result,
            markets,
            movements,
            dispersions,
            statistics,
            MethodologyVersion,
            generatedAtUtc ?? DateTime.UtcNow);
    }

    /// <inheritdoc />
    public MatchAnalysisOutput AnalyzeToJson(MatchAnalysisInput input, DateTime? generatedAtUtc = null)
    {
        var report = Analyze(input, generatedAtUtc);
        var json = JsonSerializer.Serialize(report, JsonOptions);
        return new MatchAnalysisOutput(report, json, MethodologyVersion);
    }

    private static MatchResultSummary BuildResultSummary(MatchAnalysisInput input)
    {
        var m = input.Match;
        var outcome = (m.HomeScore, m.AwayScore) switch
        {
            (int h, int a) when h > a => MatchOutcome.HomeWin,
            (int h, int a) when h < a => MatchOutcome.AwayWin,
            (int, int) => MatchOutcome.Draw,
            _ => MatchOutcome.Unknown,
        };

        return new MatchResultSummary(
            m.Id,
            m.HomeTeamId,
            m.AwayTeamId,
            m.HomeScore,
            m.AwayScore,
            outcome);
    }

    /// <summary>
    /// Builds one market analysis per market line, pricing each selection with its
    /// latest snapshot (by provider timestamp) across all bookmakers averaged into
    /// a single representative price. To keep it deterministic and simple we take
    /// the latest snapshot per selection regardless of bookmaker.
    /// </summary>
    private static IReadOnlyList<MarketAnalysisResult> BuildMarketAnalyses(MatchAnalysisInput input)
    {
        var selectionsByLine = input.Selections
            .GroupBy(s => s.MarketLineId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<MarketAnalysisResult>(selectionsByLine.Count);

        foreach (var line in input.MarketLines.OrderBy(l => l.Id))
        {
            if (!selectionsByLine.TryGetValue(line.Id, out var selections) || selections.Count == 0)
            {
                results.Add(MarketAnalysisResult.Empty(line.Id));
                continue;
            }

            var prices = new List<MarketSelectionPrice>(selections.Count);
            foreach (var selection in selections.OrderBy(s => s.Id))
            {
                var latest = input.OddsSnapshots
                    .Where(o => o.MarketLineId == line.Id && o.SelectionId == selection.Id)
                    .OrderBy(o => o.ProviderTimestampUtc)
                    .ThenBy(o => o.IngestionTimestampUtc)
                    .ThenBy(o => o.Id)
                    .LastOrDefault();

                if (latest is not null)
                {
                    prices.Add(new MarketSelectionPrice(selection.Id, selection.Name, latest.DecimalOdds));
                }
            }

            results.Add(MarketAnalyzer.Analyze(line.Id, prices));
        }

        return results;
    }

    private static IReadOnlyList<SelectionMovementSummary> BuildMovements(MatchAnalysisInput input)
    {
        var summaries = input.OddsSnapshots
            .GroupBy(o => (o.BookmakerId, o.MarketLineId, o.SelectionId))
            .OrderBy(g => g.Key.BookmakerId)
            .ThenBy(g => g.Key.MarketLineId)
            .ThenBy(g => g.Key.SelectionId)
            .Select(g => new SelectionMovementSummary(
                g.Key.BookmakerId,
                g.Key.MarketLineId,
                g.Key.SelectionId,
                OddsMovementAnalyzer.Analyze(g)))
            .ToList();

        return summaries;
    }

    private static IReadOnlyList<SelectionDispersionSummary> BuildDispersions(MatchAnalysisInput input)
    {
        var summaries = input.OddsSnapshots
            .GroupBy(o => (o.MarketLineId, o.SelectionId))
            .OrderBy(g => g.Key.MarketLineId)
            .ThenBy(g => g.Key.SelectionId)
            .Select(g => new SelectionDispersionSummary(
                g.Key.MarketLineId,
                g.Key.SelectionId,
                BookmakerAnalyzer.AnalyzeLatestPerBookmaker(g)))
            .ToList();

        return summaries;
    }

    private static StatisticsSummary BuildStatistics(MatchAnalysisInput input)
    {
        var stats = input.MatchStatistics
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new StatisticSummary(g.Key, g.Sum(s => s.Value)))
            .ToList();

        var eventCounts = input.MatchEvents
            .GroupBy(e => e.Type, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new EventTypeCount(g.Key, g.Count()))
            .ToList();

        return new StatisticsSummary(stats, eventCounts, input.MatchEvents.Count);
    }
}
