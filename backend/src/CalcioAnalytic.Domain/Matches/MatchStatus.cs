namespace CalcioAnalytic.Domain.Matches;

/// <summary>
/// Canonical lifecycle states of a match, ordered from scheduling through
/// settlement, analysis, and reconciliation.
/// </summary>
public enum MatchStatus
{
    /// <summary>The match is scheduled but not yet imminent.</summary>
    Scheduled,

    /// <summary>The match is approaching and pre-match data is being collected.</summary>
    PreMatch,

    /// <summary>The match is currently in progress.</summary>
    Live,

    /// <summary>The match has finished play.</summary>
    Finished,

    /// <summary>The match has finished and awaits settlement of markets.</summary>
    SettlementPending,

    /// <summary>The match has been analyzed.</summary>
    Analyzed,

    /// <summary>The match data has been reconciled across providers.</summary>
    Reconciled
}
