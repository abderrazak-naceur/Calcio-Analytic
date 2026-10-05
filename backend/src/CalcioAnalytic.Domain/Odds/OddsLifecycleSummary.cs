using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Odds;

/// <summary>
/// Mutable read-model summary of an immutable odds snapshot series.
/// The summary never replaces the source snapshots; it only materializes
/// lifecycle values for fast historical and dashboard queries.
/// </summary>
public sealed class OddsLifecycleSummary : Entity
{
    public Guid MatchId { get; set; }
    public Guid ProviderId { get; set; }
    public Guid BookmakerId { get; set; }
    public Guid MarketLineId { get; set; }
    public Guid SelectionId { get; set; }

    public decimal? OpeningOdds { get; set; }
    public DateTime? OpeningTimestampUtc { get; set; }
    public decimal? CurrentOdds { get; set; }
    public DateTime? CurrentTimestampUtc { get; set; }
    public decimal? PreKickoffOdds { get; set; }
    public DateTime? PreKickoffTimestampUtc { get; set; }
    public decimal? ClosingOdds { get; set; }
    public DateTime? ClosingTimestampUtc { get; set; }
    public decimal? MinOdds { get; set; }
    public decimal? MaxOdds { get; set; }
}
