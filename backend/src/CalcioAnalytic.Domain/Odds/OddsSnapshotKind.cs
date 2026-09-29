namespace CalcioAnalytic.Domain.Odds;

/// <summary>
/// Classifies an <see cref="OddsSnapshot"/> by its position in the odds
/// lifecycle for a match.
/// </summary>
public enum OddsSnapshotKind
{
    /// <summary>The opening price offered before the market matured.</summary>
    Opening,

    /// <summary>A price captured while the match is in play.</summary>
    InPlay,

    /// <summary>The closing price just before the market was suspended.</summary>
    Closing,

    /// <summary>Any other snapshot that does not fit the standard lifecycle points.</summary>
    Other
}
