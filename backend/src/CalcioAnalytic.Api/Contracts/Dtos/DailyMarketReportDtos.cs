namespace CalcioAnalytic.Api.Contracts.Dtos;

public sealed record DailyMarketReportRowDto(
    Guid MatchId,
    DateTime KickoffUtc,
    string CompetitionName,
    string HomeTeamName,
    string AwayTeamName,
    string FavoriteSelection,
    decimal FavoriteOdds,
    string? WinnerSelection,
    decimal? WinnerOdds,
    string Classification,
    bool IsUpset);

public sealed record DailyMarketReportDto(
    DateTime DateUtc,
    int SettledMarkets,
    int FavoriteHits,
    int FavoriteFailures,
    int Upsets,
    decimal? FavoriteFailureRatePercentage,
    decimal? RoiPercentage,
    int UpcomingMatches,
    IReadOnlyList<DailyMarketReportRowDto> BiggestUpsets);
