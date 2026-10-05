using CalcioAnalytic.Analytics.MarketIntelligence;
using CalcioAnalytic.Domain.Settlement;

namespace CalcioAnalytic.Api.Tests;

public sealed class MarketOutcomeAnalyzerTests
{
    [Fact]
    public void Favorite_wins_is_hit()
    {
        var result = MarketOutcomeAnalyzer.Analyze(
        [
            new("Home", 1.60m, SettlementStatus.Won),
            new("Draw", 4.20m, SettlementStatus.Lost),
            new("Away", 6.50m, SettlementStatus.Lost),
        ]);

        Assert.Equal("Home", result.FavoriteSelectionName);
        Assert.Equal(1.60m, result.FavoriteOdds);
        Assert.Equal(MarketOutcomeClassification.Hit, result.Classification);
        Assert.False(result.IsUpset);
        Assert.Equal("Home", result.WinningSelectionName);
    }

    [Fact]
    public void Favorite_failure_with_winner_at_6_50_is_miss_and_upset()
    {
        var result = MarketOutcomeAnalyzer.Analyze(
        [
            new("Home", 1.60m, SettlementStatus.Lost),
            new("Draw", 4.20m, SettlementStatus.Lost),
            new("Away", 6.50m, SettlementStatus.Won),
        ]);

        Assert.Equal(MarketOutcomeClassification.Miss, result.Classification);
        Assert.Equal(MarketOutcomeUpsetClassification.Upset, result.UpsetClassification);
        Assert.True(result.IsFavoriteFailure);
        Assert.True(result.IsUpset);
        Assert.Equal("Away", result.WinningSelectionName);
        Assert.Equal(6.50m, result.WinningOdds);
    }

    [Fact]
    public void Favorite_failure_below_threshold_is_miss_but_not_upset()
    {
        var result = MarketOutcomeAnalyzer.Analyze(
        [
            new("Home", 1.60m, SettlementStatus.Lost),
            new("Draw", 4.20m, SettlementStatus.Won),
            new("Away", 6.50m, SettlementStatus.Lost),
        ]);

        Assert.Equal(MarketOutcomeClassification.Miss, result.Classification);
        Assert.Equal(MarketOutcomeUpsetClassification.None, result.UpsetClassification);
        Assert.Equal("Draw", result.WinningSelectionName);
        Assert.Equal(4.20m, result.WinningOdds);
    }

    [Fact]
    public void Custom_threshold_is_respected()
    {
        var result = MarketOutcomeAnalyzer.Analyze(
        [
            new("Home", 1.60m, SettlementStatus.Lost),
            new("Draw", 4.20m, SettlementStatus.Lost),
            new("Away", 6.50m, SettlementStatus.Won),
        ],
        upsetThreshold: 7m);

        Assert.Equal(MarketOutcomeClassification.Miss, result.Classification);
        Assert.Equal(MarketOutcomeUpsetClassification.None, result.UpsetClassification);
        Assert.Equal(7m, result.UpsetThreshold);
    }

    [Fact]
    public void Tied_favorite_prices_are_unknown()
    {
        var result = MarketOutcomeAnalyzer.Analyze(
        [
            new("Home", 2.50m, SettlementStatus.Won),
            new("Draw", 2.50m, SettlementStatus.Lost),
            new("Away", 3.00m, SettlementStatus.Lost),
        ]);

        Assert.Equal(MarketOutcomeClassification.Unknown, result.Classification);
        Assert.Null(result.FavoriteSelectionName);
    }

    [Fact]
    public void Unsettled_market_is_unknown()
    {
        var result = MarketOutcomeAnalyzer.Analyze(
        [
            new("Home", 1.60m, SettlementStatus.Unknown),
            new("Draw", 4.20m, SettlementStatus.Unknown),
            new("Away", 6.50m, SettlementStatus.Unknown),
        ]);

        Assert.Equal(MarketOutcomeClassification.Unknown, result.Classification);
        Assert.False(result.IsFavoriteFailure);
    }

    [Fact]
    public void Push_favorite_does_not_count_as_hit_or_miss()
    {
        var result = MarketOutcomeAnalyzer.Analyze(
        [
            new("Over", 1.50m, SettlementStatus.Push),
            new("Under", 2.60m, SettlementStatus.Push),
        ]);

        Assert.Equal(MarketOutcomeClassification.Unknown, result.Classification);
    }
}
