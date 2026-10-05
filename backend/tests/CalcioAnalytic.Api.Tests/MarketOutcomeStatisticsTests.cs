using CalcioAnalytic.Analytics.MarketIntelligence;

namespace CalcioAnalytic.Api.Tests;

public sealed class MarketOutcomeStatisticsTests
{
    [Fact]
    public void Calculates_probability_pl_and_roi_for_flat_stake()
    {
        var result = MarketOutcomeStatisticsCalculator.Calculate([
            (2m, (bool?)true),
            (3m, (bool?)false),
            (4m, (bool?)true),
        ]);

        Assert.Equal(3, result.SampleSize);
        Assert.Equal(2, result.Wins);
        Assert.Equal(1, result.Losses);
        Assert.Equal(66.67m, result.ActualProbabilityPercentage);
        Assert.Equal(36.11m, result.ImpliedProbabilityPercentage);
        Assert.Equal(3m, result.ProfitUnits);
        Assert.Equal(100m, result.RoiPercentage);
    }

    [Fact]
    public void Ignores_unknown_and_invalid_odds()
    {
        var result = MarketOutcomeStatisticsCalculator.Calculate([
            (2m, (bool?)true),
            (0m, (bool?)true),
            (3m, null),
        ]);

        Assert.Equal(1, result.SampleSize);
        Assert.Equal(1, result.Wins);
        Assert.Equal(1m, result.ProfitUnits);
        Assert.Equal(100m, result.RoiPercentage);
    }
}
