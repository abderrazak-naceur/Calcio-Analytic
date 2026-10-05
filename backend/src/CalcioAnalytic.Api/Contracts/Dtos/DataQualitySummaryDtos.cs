namespace CalcioAnalytic.Api.Contracts.Dtos;

public sealed record DataQualitySummaryDto(
    int TotalMatches,
    int FinishedMatches,
    int MissingOddsMatches,
    int MissingResults,
    int MissingSettlements,
    int DuplicateOddsMatches,
    int StaleScheduledMatches,
    decimal QualityScorePercentage,
    DateTime GeneratedAtUtc);
