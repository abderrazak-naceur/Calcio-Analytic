using DomainMatch = CalcioAnalytic.Domain.Matches.Match;

namespace CalcioAnalytic.Analytics.Settlement;

/// <summary>
/// A single selection on a market line, reduced to the fields the settlement
/// engine needs: the selection identity and its display name (used to detect
/// the market type, e.g. "Home", "Over").
/// </summary>
/// <param name="SelectionId">The selection's unique identifier.</param>
/// <param name="Name">The selection name, such as "Home", "Draw", "Over".</param>
public sealed record SettlementSelection(Guid SelectionId, string Name);

/// <summary>
/// A market line together with the selections that belong to it, reduced to the
/// fields the settlement engine needs. The market type is inferred from the
/// selection names and the presence of <paramref name="Line"/>.
/// </summary>
/// <param name="MarketLineId">The market line's unique identifier.</param>
/// <param name="Line">Optional numeric line (e.g. 2.5 for a total), or null.</param>
/// <param name="Period">Optional period the line applies to (e.g. "FullTime").</param>
/// <param name="Selections">The selections offered on this line.</param>
public sealed record SettlementMarketLine(
    Guid MarketLineId,
    decimal? Line,
    string? Period,
    IReadOnlyList<SettlementSelection> Selections);

/// <summary>
/// A fully-populated, in-memory input model for settling a match's markets. The
/// caller loads the match and its market lines from persistence; the engine
/// performs no data access, keeping it pure, deterministic, and unit-testable.
/// </summary>
/// <param name="Match">The finished match whose final score drives settlement.</param>
/// <param name="MarketLines">The market lines (with selections) to settle.</param>
public sealed record SettlementInput(
    DomainMatch Match,
    IReadOnlyList<SettlementMarketLine> MarketLines);
