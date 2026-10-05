namespace CalcioAnalytic.Api.Contracts.Dtos;

public sealed record UpcomingAnalysisQueryDto(
    string Window,
    DateTime FromUtc,
    DateTime ToUtc,
    string MarketCode,
    int MinimumComparableSamples);

public sealed record UpcomingComparableStatsDto(
    int SampleSize,
    int HitCount,
    int MissCount,
    int UpsetCount,
    decimal? HitRatePercentage,
    decimal? FailureRatePercentage,
    decimal? AverageFavoriteOdds,
    decimal? AverageWinnerOdds,
    decimal? ImpliedProbabilityPercentage,
    decimal? ActualProbabilityPercentage,
    decimal? RoiPercentage);

public sealed record UpcomingAnalysisRowDto(
    Guid MatchId,
    DateTime KickoffUtc,
    string CompetitionName,
    string HomeTeamName,
    string AwayTeamName,
    Guid BookmakerId,
    string BookmakerName,
    string MarketCode,
    string FavoriteSelection,
    decimal FavoriteOdds,
    int ComparableSampleSize,
    UpcomingComparableStatsDto ComparableHistory);

public sealed record UpcomingAnalysisResponseDto(
    UpcomingAnalysisQueryDto Query,
    IReadOnlyList<UpcomingAnalysisRowDto> Results,
    int TotalResults);
