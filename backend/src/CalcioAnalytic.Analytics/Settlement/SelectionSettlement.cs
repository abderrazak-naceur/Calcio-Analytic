using CalcioAnalytic.Domain.Settlement;

namespace CalcioAnalytic.Analytics.Settlement;

/// <summary>
/// The settled outcome of a single selection on a market line, produced by the
/// <see cref="ISettlementEngine"/>.
/// </summary>
/// <param name="MarketLineId">The market line the selection belongs to.</param>
/// <param name="SelectionId">The selection that was settled.</param>
/// <param name="Status">The determined settlement outcome.</param>
public sealed record SelectionSettlement(
    Guid MarketLineId,
    Guid SelectionId,
    SettlementStatus Status);
