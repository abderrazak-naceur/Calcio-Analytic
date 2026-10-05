using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Domain.Features;

/// <summary>
/// Immutable point-in-time feature vector captured before a match kickoff.
/// Every value must be derivable from information available strictly before
/// <see cref="MatchFeatureSnapshot.KickoffUtc"/>.
/// </summary>
public class MatchFeatureSnapshot : Entity
{
    public Guid MatchId { get; set; }
    public DateTime KickoffUtc { get; set; }
    public DateTime FeatureTimestampUtc { get; set; }

    public decimal? HomeElo { get; set; }
    public decimal? AwayElo { get; set; }
    public decimal? EloDifference { get; set; }

    public int? HomeFormLast5Points { get; set; }
    public int? AwayFormLast5Points { get; set; }
    public decimal? HomeGoalsForLast5 { get; set; }
    public decimal? HomeGoalsAgainstLast5 { get; set; }
    public decimal? AwayGoalsForLast5 { get; set; }
    public decimal? AwayGoalsAgainstLast5 { get; set; }

    public decimal? PoissonHomeLambda { get; set; }
    public decimal? PoissonAwayLambda { get; set; }
    public decimal? PoissonHomeProbability { get; set; }
    public decimal? PoissonDrawProbability { get; set; }
    public decimal? PoissonAwayProbability { get; set; }

    public decimal? DixonColesHomeProbability { get; set; }
    public decimal? DixonColesDrawProbability { get; set; }
    public decimal? DixonColesAwayProbability { get; set; }

    public decimal? MarketHomeProbability { get; set; }
    public decimal? MarketDrawProbability { get; set; }
    public decimal? MarketAwayProbability { get; set; }

    public decimal? HomeClosingOdds { get; set; }
    public decimal? DrawClosingOdds { get; set; }
    public decimal? AwayClosingOdds { get; set; }

    public string FeatureSetVersion { get; set; } = "fts-1.0.0";
    public string MethodologyVersion { get; set; } = "pit-1.0.0";
}