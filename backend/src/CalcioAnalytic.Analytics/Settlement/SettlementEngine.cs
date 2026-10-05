using CalcioAnalytic.Domain.Settlement;

namespace CalcioAnalytic.Analytics.Settlement;

/// <summary>
/// Pure, deterministic implementation of <see cref="ISettlementEngine"/>.
/// </summary>
/// <remarks>
/// The engine settles only markets it can recognize from their selection names
/// and line; everything else is <see cref="SettlementStatus.Unknown"/>.
///
/// Supported markets:
/// <list type="bullet">
/// <item>
/// 1X2 / Match Winner: a line whose selections are named "Home", "Draw",
/// and/or "Away" (case-insensitive). The winning side is <c>Won</c>; the
/// others are <c>Lost</c>.
/// </item>
/// <item>
/// BTTS: a line with selections named "Yes" and/or "No". Yes wins when both
/// teams score at least one goal; No wins otherwise.
/// </item>
/// <item>
/// Over/Under: a line with a numeric <see cref="SettlementMarketLine.Line"/>
/// whose selections are named "Over"/"Under". Compare total goals to the line:
/// total &gt; line =&gt; Over Won / Under Lost; total &lt; line =&gt; Over Lost /
/// Under Won; total == line =&gt; both Push.
/// </item>
/// </list>
///
/// Worked example — final score 2-1 (total goals = 3):
/// <list type="bullet">
/// <item>Over/Under 2.5: total 3 &gt; 2.5 =&gt; Over =&gt; Won, Under =&gt; Lost.</item>
/// <item>1X2: HomeScore 2 &gt; AwayScore 1 =&gt; Home =&gt; Won, Draw =&gt; Lost, Away =&gt; Lost.</item>
/// </list>
/// </remarks>
public sealed class SettlementEngine : ISettlementEngine
{
    private const string Home = "Home";
    private const string Draw = "Draw";
    private const string Away = "Away";
    private const string Over = "Over";
    private const string Under = "Under";
    private const string Yes = "Yes";
    private const string No = "No";

    /// <inheritdoc />
    public IReadOnlyList<SelectionSettlement> Settle(SettlementInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var results = new List<SelectionSettlement>();

        // Without a final score, nothing can be determined.
        var hasScore = input.Match.HomeScore is not null && input.Match.AwayScore is not null;
        if (!hasScore)
        {
            foreach (var line in input.MarketLines)
            {
                AddAll(results, line, SettlementStatus.Unknown);
            }

            return results;
        }

        var homeScore = input.Match.HomeScore!.Value;
        var awayScore = input.Match.AwayScore!.Value;
        var totalGoals = homeScore + awayScore;

        foreach (var line in input.MarketLines)
        {
            if (IsMatchWinner(line))
            {
                SettleMatchWinner(results, line, homeScore, awayScore);
            }
            else if (IsOverUnder(line))
            {
                SettleOverUnder(results, line, totalGoals, line.Line!.Value);
            }
            else if (IsBtts(line))
            {
                SettleBtts(results, line, homeScore, awayScore);
            }
            else
            {
                AddAll(results, line, SettlementStatus.Unknown);
            }
        }

        return results;
    }

    /// <summary>
    /// A line is 1X2 when it carries at least one of Home/Draw/Away and every
    /// selection is one of those names.
    /// </summary>
    private static bool IsMatchWinner(SettlementMarketLine line)
    {
        if (line.Selections.Count == 0)
        {
            return false;
        }

        var any = false;
        foreach (var selection in line.Selections)
        {
            if (IsName(selection, Home) || IsName(selection, Draw) || IsName(selection, Away))
            {
                any = true;
            }
            else
            {
                return false;
            }
        }

        return any;
    }

    /// <summary>
    /// A line is Over/Under when it has a numeric line and every selection is
    /// named Over or Under (with at least one present).
    /// </summary>
    private static bool IsOverUnder(SettlementMarketLine line)
    {
        if (line.Line is null || line.Selections.Count == 0)
        {
            return false;
        }

        var any = false;
        foreach (var selection in line.Selections)
        {
            if (IsName(selection, Over) || IsName(selection, Under))
            {
                any = true;
            }
            else
            {
                return false;
            }
        }

        return any;
    }

    private static void SettleMatchWinner(
        List<SelectionSettlement> results,
        SettlementMarketLine line,
        int homeScore,
        int awayScore)
    {
        foreach (var selection in line.Selections)
        {
            var status = SettlementStatus.Lost;

            if (IsName(selection, Home) && homeScore > awayScore)
            {
                status = SettlementStatus.Won;
            }
            else if (IsName(selection, Away) && awayScore > homeScore)
            {
                status = SettlementStatus.Won;
            }
            else if (IsName(selection, Draw) && homeScore == awayScore)
            {
                status = SettlementStatus.Won;
            }

            results.Add(new SelectionSettlement(line.MarketLineId, selection.SelectionId, status));
        }
    }

    private static bool IsBtts(SettlementMarketLine line)
    {
        if (line.Selections.Count == 0)
        {
            return false;
        }

        var any = false;
        foreach (var selection in line.Selections)
        {
            if (IsName(selection, Yes) || IsName(selection, No))
            {
                any = true;
            }
            else
            {
                return false;
            }
        }

        return any;
    }

    private static void SettleBtts(
        List<SelectionSettlement> results,
        SettlementMarketLine line,
        int homeScore,
        int awayScore)
    {
        var bothTeamsScored = homeScore > 0 && awayScore > 0;

        foreach (var selection in line.Selections)
        {
            var status = IsName(selection, Yes)
                ? (bothTeamsScored ? SettlementStatus.Won : SettlementStatus.Lost)
                : (bothTeamsScored ? SettlementStatus.Lost : SettlementStatus.Won);

            results.Add(new SelectionSettlement(line.MarketLineId, selection.SelectionId, status));
        }
    }

    private static void SettleOverUnder(
        List<SelectionSettlement> results,
        SettlementMarketLine line,
        int totalGoals,
        decimal lineValue)
    {
        foreach (var selection in line.Selections)
        {
            SettlementStatus status;

            if (totalGoals == lineValue)
            {
                // Exact match on an integer line: stakes returned for both sides.
                status = SettlementStatus.Push;
            }
            else if (IsName(selection, Over))
            {
                status = totalGoals > lineValue ? SettlementStatus.Won : SettlementStatus.Lost;
            }
            else
            {
                // Under.
                status = totalGoals < lineValue ? SettlementStatus.Won : SettlementStatus.Lost;
            }

            results.Add(new SelectionSettlement(line.MarketLineId, selection.SelectionId, status));
        }
    }

    private static void AddAll(
        List<SelectionSettlement> results,
        SettlementMarketLine line,
        SettlementStatus status)
    {
        foreach (var selection in line.Selections)
        {
            results.Add(new SelectionSettlement(line.MarketLineId, selection.SelectionId, status));
        }
    }

    private static bool IsName(SettlementSelection selection, string name) =>
        string.Equals(selection.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase);
}
