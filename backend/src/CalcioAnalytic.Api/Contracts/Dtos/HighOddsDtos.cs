namespace CalcioAnalytic.Api.Contracts.Dtos;

public sealed record HighOddsQueryDto(
    DateTime? FromUtc,
    DateTime? ToUtc,
    decimal MinOdds,
    Guid? BookmakerId,
    string? Result,
    int Page,
    int PageSize);

public sealed record HighOddsSelectionDto(
    Guid MatchId,
    DateTime KickoffUtc,
    Guid CompetitionId,
    string CompetitionName,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    string Selection,
    string Result,
    bool Won,
    decimal Odds,
    decimal ImpliedProbability,
    Guid BookmakerId,
    string BookmakerName,
    decimal? OpeningOdds,
    decimal? ClosingOdds,
    decimal? MovementPercentage,
    decimal ProfitUnits);

public sealed record HighOddsRangeStatsDto(
    string Range,
    int Selections,
    int Wins,
    decimal WinRatePercentage,
    decimal AverageOdds,
    decimal ProfitUnits,
    decimal RoiPercentage);

public sealed record HighOddsGroupStatsDto(
    string Group,
    int Selections,
    int Wins,
    decimal WinRatePercentage,
    decimal AverageOdds,
    decimal ProfitUnits,
    decimal RoiPercentage);

public sealed record HighOddsSummaryDto(
    int UniqueMatches,
    int QualifyingSelections,
    int Wins,
    decimal WinRatePercentage,
    decimal AverageOdds,
    decimal AverageImpliedProbabilityPercentage,
    decimal StakeUnits,
    decimal ReturnUnits,
    decimal ProfitUnits,
    decimal RoiPercentage,
    decimal MaxDrawdownUnits,
    int MaxWinningStreak,
    int MaxLosingStreak);

public sealed record HighOddsAnalyticsResponseDto(
    HighOddsQueryDto Query,
    HighOddsSummaryDto Summary,
    IReadOnlyList<HighOddsRangeStatsDto> ByOddsRange,
    IReadOnlyList<HighOddsGroupStatsDto> BySelection,
    IReadOnlyList<HighOddsGroupStatsDto> ByBookmaker,
    IReadOnlyList<HighOddsSelectionDto> Results,
    int TotalResults,
    int Page,
    int PageSize);

public sealed record HighOddsExportRowDto(
    DateTime KickoffUtc,
    string Competition,
    string HomeTeam,
    string AwayTeam,
    string Selection,
    decimal Odds,
    string Bookmaker,
    string Result,
    bool Won,
    decimal ProfitUnits,
    decimal? OpeningOdds,
    decimal? ClosingOdds,
    decimal? MovementPercentage);
