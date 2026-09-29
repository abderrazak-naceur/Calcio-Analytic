using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Statistics;

/// <summary>
/// A single named statistical measure for a team within a match, such as
/// possession, shots, or expected goals.
/// </summary>
public class MatchStatistic : Entity
{
    /// <summary>Foreign key to the match the statistic belongs to.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Foreign key to the team the statistic describes.</summary>
    public Guid TeamId { get; set; }

    /// <summary>Name of the statistic (e.g. "Possession", "Shots", "xG").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Numeric value of the statistic.</summary>
    public decimal Value { get; set; }
}
