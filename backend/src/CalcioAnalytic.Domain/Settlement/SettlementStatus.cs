namespace CalcioAnalytic.Domain.Settlement;

/// <summary>
/// Outcome of settling a selection on a market line for a match.
/// </summary>
public enum SettlementStatus
{
    /// <summary>The selection won.</summary>
    Won,

    /// <summary>The selection lost.</summary>
    Lost,

    /// <summary>The market was voided; stakes are returned.</summary>
    Void,

    /// <summary>The result was a push; stakes are returned.</summary>
    Push,

    /// <summary>The outcome could not be determined.</summary>
    Unknown
}
