using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Odds;

/// <summary>
/// An append-only historical record of odds offered by a bookmaker for a given
/// selection at a point in time. Snapshots are never overwritten; each capture
/// produces a new row so the full price history remains reproducible.
/// </summary>
public class OddsSnapshot : Entity
{
    /// <summary>Foreign key to the match these odds relate to.</summary>
    public Guid MatchId { get; set; }

    /// <summary>Foreign key to the bookmaker offering the price.</summary>
    public Guid BookmakerId { get; set; }

    /// <summary>Foreign key to the market line being priced.</summary>
    public Guid MarketLineId { get; set; }

    /// <summary>Foreign key to the selection being priced.</summary>
    public Guid SelectionId { get; set; }

    /// <summary>Decimal (European) representation of the odds.</summary>
    public decimal DecimalOdds { get; set; }

    /// <summary>Optional fractional (UK) representation of the odds (e.g. "5/2").</summary>
    public string? FractionalOdds { get; set; }

    /// <summary>Optional American (moneyline) representation of the odds.</summary>
    public int? AmericanOdds { get; set; }

    /// <summary>Implied probability derived from the odds, in the range [0, 1].</summary>
    public decimal ImpliedProbability { get; set; }

    /// <summary>Whether the price was captured during live (in-play) betting.</summary>
    public bool IsLive { get; set; }

    /// <summary>Optional match minute at capture time when live.</summary>
    public int? MatchMinute { get; set; }

    /// <summary>Optional period the price applies to (e.g. "FullTime").</summary>
    public string? Period { get; set; }

    /// <summary>Classifies the snapshot within the odds lifecycle.</summary>
    public OddsSnapshotKind Kind { get; set; }

    /// <summary>UTC timestamp reported by the bookmaker for the price.</summary>
    public DateTime BookmakerTimestampUtc { get; set; }

    /// <summary>UTC timestamp reported by the data provider for the price.</summary>
    public DateTime ProviderTimestampUtc { get; set; }

    /// <summary>UTC timestamp when this snapshot was ingested into the system.</summary>
    public DateTime IngestionTimestampUtc { get; set; }

    /// <summary>
    /// Hash of the source payload used to deduplicate identical snapshots and
    /// guarantee reproducibility of the ingested price.
    /// </summary>
    public string PayloadHash { get; set; } = string.Empty;
}
