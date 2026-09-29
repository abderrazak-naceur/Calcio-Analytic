using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Analytics;

/// <summary>
/// An immutable, versioned analysis report for a match. Each generation
/// produces a new row with an incremented <see cref="Version"/>; existing
/// versions are never mutated, preserving a full audit trail of analyses.
/// </summary>
public class MatchAnalysis : Entity
{
    /// <summary>Foreign key to the match this analysis describes.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Monotonically increasing version number for this match's analyses.</summary>
    public int Version { get; set; }

    /// <summary>UTC timestamp when the analysis was generated.</summary>
    public DateTime GeneratedAtUtc { get; set; }

    /// <summary>Immutable serialized report payload (JSON).</summary>
    public string AnalysisJson { get; set; } = string.Empty;

    /// <summary>Version identifier of the methodology used to generate the report.</summary>
    public string MethodologyVersion { get; set; } = string.Empty;
}
