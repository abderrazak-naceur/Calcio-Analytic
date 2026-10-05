namespace CalcioAnalytic.Analytics.MarketIntelligence;

public sealed record MarketOutcomeStatistics(
    int SampleSize,
    int Wins,
    int Losses,
    decimal HitRatePercentage,
    decimal? ImpliedProbabilityPercentage,
    decimal? ActualProbabilityPercentage,
    decimal ProfitUnits,
    decimal RoiPercentage);

public static class MarketOutcomeStatisticsCalculator
{
    public static MarketOutcomeStatistics Calculate(
        IEnumerable<(decimal Odds, bool? Won)> observations)
    {
        var rows = observations.Where(x => x.Odds > 1m && x.Won.HasValue).ToList();
        var sample = rows.Count;
        if (sample == 0)
            return new(0, 0, 0, 0m, null, null, 0m, 0m);

        var wins = rows.Count(x => x.Won == true);
        var losses = sample - wins;
        var profit = rows.Sum(x => x.Won == true ? x.Odds - 1m : -1m);
        var implied = rows.Average(x => 1m / x.Odds) * 100m;
        var actual = wins * 100m / sample;

        return new(
            sample,
            wins,
            losses,
            decimal.Round(actual, 2),
            decimal.Round(implied, 2),
            decimal.Round(actual, 2),
            decimal.Round(profit, 2),
            decimal.Round(profit / sample * 100m, 2));
    }
}
