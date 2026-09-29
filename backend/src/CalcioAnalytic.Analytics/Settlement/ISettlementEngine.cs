namespace CalcioAnalytic.Analytics.Settlement;

/// <summary>
/// Determines the settlement status of every selection on a match's supported
/// market lines given the match's final score. Implementations must be pure and
/// deterministic: the same <see cref="SettlementInput"/> yields the same result,
/// with no data access or side effects.
/// </summary>
/// <remarks>
/// Only explicitly supported markets are settled (1X2 / Match Winner and
/// Over/Under totals). Any market that cannot be recognized from its selection
/// names and line is reported as <see cref="Domain.Settlement.SettlementStatus.Unknown"/>
/// for all of its selections; the engine never guesses.
/// </remarks>
public interface ISettlementEngine
{
    /// <summary>
    /// Settles all selections across the input's market lines.
    /// </summary>
    /// <param name="input">The match and its market lines to settle.</param>
    /// <returns>
    /// One <see cref="SelectionSettlement"/> per selection, in input order.
    /// </returns>
    IReadOnlyList<SelectionSettlement> Settle(SettlementInput input);
}
