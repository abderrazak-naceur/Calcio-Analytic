namespace CalcioAnalytic.Analytics.Patterns;

/// <summary>
/// The default, pure, deterministic implementation of
/// <see cref="IHistoricalPatternEngine"/>. All computation is performed over the
/// in-memory records the caller supplies; there is no data access.
/// </summary>
/// <remarks>
/// Worked example: given three records with outcomes [Home, Home, Away], the
/// <see cref="PatternResult.ResultDistribution"/> is {Home:2, Draw:0, Away:1} and
/// the Home entry of <see cref="PatternResult.ResultPercentages"/> is
/// 2 / 3 ≈ 0.667.
/// </remarks>
public sealed class HistoricalPatternEngine : IHistoricalPatternEngine
{
    /// <summary>The methodology version stamped onto every result.</summary>
    public const string MethodologyVersion = "patterns-1.0.0";

    private const string Home = "Home";
    private const string Draw = "Draw";
    private const string Away = "Away";

    /// <summary>The over/under goals threshold used for the over/under distribution.</summary>
    private const decimal OverUnderThreshold = 2.5m;

    /// <summary>The z-score for a 95% Wilson score confidence interval.</summary>
    private const decimal WilsonZ = 1.959963984540054m;

    /// <summary>The minimum sample size required to report a home-win confidence interval.</summary>
    private const int MinSampleForConfidence = 20;

    /// <inheritdoc />
    public PatternResult Analyze(PatternQuery query, IEnumerable<PatternMatchRecord> records)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(records);

        var queryHash = query.QueryHash;

        // Deterministic filtering and ordering by kickoff then match id.
        var filtered = records
            .Where(record => Matches(query, record))
            .OrderBy(record => record.KickoffUtc)
            .ThenBy(record => record.MatchId)
            .ToList();

        var sampleSize = filtered.Count;

        if (sampleSize == 0)
        {
            return new PatternResult(
                SampleSize: 0,
                ResultDistribution: new Dictionary<string, int> { [Home] = 0, [Draw] = 0, [Away] = 0 },
                ResultPercentages: new Dictionary<string, decimal> { [Home] = 0m, [Draw] = 0m, [Away] = 0m },
                AverageTotalGoals: null,
                OverUnderDistribution: new Dictionary<string, int> { ["Over2.5"] = 0, ["Under2.5"] = 0 },
                AverageClosingHomeOdds: null,
                AverageMovementPercentage: null,
                DataCompleteness: 0m,
                HomeWinConfidenceInterval: null,
                Methodology: MethodologyVersion,
                QueryHash: queryHash);
        }

        var homeWins = filtered.Count(record => record.Outcome == Home);
        var draws = filtered.Count(record => record.Outcome == Draw);
        var aways = filtered.Count(record => record.Outcome == Away);

        var resultDistribution = new Dictionary<string, int>
        {
            [Home] = homeWins,
            [Draw] = draws,
            [Away] = aways,
        };

        var resultPercentages = new Dictionary<string, decimal>
        {
            [Home] = (decimal)homeWins / sampleSize,
            [Draw] = (decimal)draws / sampleSize,
            [Away] = (decimal)aways / sampleSize,
        };

        var totals = filtered.Where(r => r.TotalGoals.HasValue).Select(r => r.TotalGoals!.Value).ToList();
        decimal? averageTotalGoals = totals.Count > 0 ? (decimal)totals.Sum() / totals.Count : null;

        var overUnderDistribution = new Dictionary<string, int>
        {
            ["Over2.5"] = totals.Count(t => t > OverUnderThreshold),
            ["Under2.5"] = totals.Count(t => t < OverUnderThreshold),
        };

        var closingOdds = filtered.Where(r => r.ClosingHomeOdds.HasValue).Select(r => r.ClosingHomeOdds!.Value).ToList();
        decimal? averageClosingHomeOdds = closingOdds.Count > 0 ? closingOdds.Sum() / closingOdds.Count : null;

        var movements = filtered.Where(r => r.MovementPercentage.HasValue).Select(r => r.MovementPercentage!.Value).ToList();
        decimal? averageMovementPercentage = movements.Count > 0 ? movements.Sum() / movements.Count : null;

        var dataCompleteness = (decimal)closingOdds.Count / sampleSize;

        ConfidenceInterval? homeWinConfidenceInterval = sampleSize >= MinSampleForConfidence
            ? WilsonScoreInterval(homeWins, sampleSize)
            : null;

        return new PatternResult(
            SampleSize: sampleSize,
            ResultDistribution: resultDistribution,
            ResultPercentages: resultPercentages,
            AverageTotalGoals: averageTotalGoals,
            OverUnderDistribution: overUnderDistribution,
            AverageClosingHomeOdds: averageClosingHomeOdds,
            AverageMovementPercentage: averageMovementPercentage,
            DataCompleteness: dataCompleteness,
            HomeWinConfidenceInterval: homeWinConfidenceInterval,
            Methodology: MethodologyVersion,
            QueryHash: queryHash);
    }

    /// <summary>
    /// Determines whether a record satisfies every non-null filter on the query.
    /// </summary>
    private static bool Matches(PatternQuery query, PatternMatchRecord record)
    {
        if (query.CompetitionId is { } competitionId && record.CompetitionId != competitionId)
        {
            return false;
        }

        if (query.SeasonId is { } seasonId && record.SeasonId != seasonId)
        {
            return false;
        }

        if (query.FromUtc is { } fromUtc && record.KickoffUtc < fromUtc)
        {
            return false;
        }

        if (query.ToUtc is { } toUtc && record.KickoffUtc > toUtc)
        {
            return false;
        }

        if (query.MinClosingHomeOdds is { } minOdds && !(record.ClosingHomeOdds >= minOdds))
        {
            return false;
        }

        if (query.MaxClosingHomeOdds is { } maxOdds && !(record.ClosingHomeOdds <= maxOdds))
        {
            return false;
        }

        if (query.MinMovementPercentage is { } minMovement && !(record.MovementPercentage >= minMovement))
        {
            return false;
        }

        if (query.MaxMovementPercentage is { } maxMovement && !(record.MovementPercentage <= maxMovement))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Computes the Wilson score confidence interval for a binomial proportion.
    /// The double-precision arithmetic is bounded to [0, 1] and rounded to a fixed
    /// scale so the result is stable and deterministic.
    /// </summary>
    /// <param name="successes">The number of successes (e.g. home wins).</param>
    /// <param name="total">The total sample size; must be positive.</param>
    /// <returns>The lower and upper bounds as decimals in the range 0..1.</returns>
    internal static ConfidenceInterval WilsonScoreInterval(int successes, int total)
    {
        if (total <= 0)
        {
            return new ConfidenceInterval(0m, 0m);
        }

        var z = (double)WilsonZ;
        double n = total;
        var pHat = successes / n;

        var denominator = 1d + (z * z / n);
        var centre = pHat + (z * z / (2d * n));
        var margin = z * Math.Sqrt((pHat * (1d - pHat) / n) + (z * z / (4d * n * n)));

        var lower = (centre - margin) / denominator;
        var upper = (centre + margin) / denominator;

        return new ConfidenceInterval(Clamp(lower), Clamp(upper));
    }

    /// <summary>
    /// Clamps a double proportion to [0, 1] and rounds it to a stable decimal scale.
    /// </summary>
    private static decimal Clamp(double value)
    {
        var clamped = Math.Clamp(value, 0d, 1d);
        return Math.Round((decimal)clamped, 6, MidpointRounding.ToEven);
    }
}
