namespace CalcioAnalytic.Api.Contracts.Dtos;

public sealed record MarketOutcomeQueryDto(
    Guid? MatchId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string MarketCode,
    decimal? MinFavoriteOdds,
    decimal? MaxFavoriteOdds,
    decimal UpsetThreshold,
    Guid? BookmakerId,
    string? Classification,
    int Page,
    int PageSize);

public sealed record MarketOutcomeRowDto(
    Guid MatchId,
    DateTime KickoffUtc,
    Guid CompetitionId,
    string CompetitionName,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    Guid BookmakerId,
    string BookmakerName,
    Guid MarketLineId,
    string MarketCode,
    decimal? Line,
    string FavoriteSelection,
    decimal FavoriteOdds,
    string FavoriteStatus,
    string Classification,
    bool IsUpset,
    string? WinnerSelection,
    decimal? WinnerOdds);

public sealed record MarketOutcomeThresholdStatDto(
    decimal Threshold,
    int WinnerCount,
    decimal? PercentageOfFavoriteFailures);

public sealed record MarketFailureOddsRangeDto(
    string Range,
    int SampleSize,
    int HitCount,
    int MissCount,
    int UpsetCount,
    decimal? FailureRatePercentage,
    decimal? ImpliedProbabilityPercentage,
    decimal? ActualProbabilityPercentage,
    decimal ProfitUnits,
    decimal RoiPercentage);

public sealed record MarketOutcomeSummaryDto(
    int TotalMarkets,
    int HitCount,
    int MissCount,
    int UpsetCount,
    int UnknownCount,
    decimal? FavoriteFailureRatePercentage,
    IReadOnlyList<MarketOutcomeThresholdStatDto> UpsetThresholds,
    IReadOnlyList<MarketFailureOddsRangeDto> FavoriteOddsRanges,
    decimal? ImpliedProbabilityPercentage,
    decimal? ActualProbabilityPercentage,
    decimal ProfitUnits,
    decimal RoiPercentage);

public sealed record MarketOutcomeAnalyticsResponseDto(
    MarketOutcomeQueryDto Query,
    MarketOutcomeSummaryDto Summary,
    IReadOnlyList<MarketOutcomeRowDto> Results,
    int TotalResults,
    int Page,
    int PageSize);
