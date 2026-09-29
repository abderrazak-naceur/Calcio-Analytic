namespace CalcioAnalytic.Api.Contracts.Dtos;

/// <summary>
/// Request body for the historical pattern query endpoint. Mirrors the fields of
/// <see cref="CalcioAnalytic.Analytics.Patterns.PatternQuery"/> one-for-one so the
/// wire contract stays aligned with the reproducible query definition. Every
/// field is optional; an absent field applies no filter for that dimension.
/// </summary>
/// <param name="CompetitionId">When set, restricts to matches in this competition.</param>
/// <param name="SeasonId">When set, restricts to matches in this season.</param>
/// <param name="FromUtc">When set, restricts to matches kicking off at or after this UTC instant.</param>
/// <param name="ToUtc">When set, restricts to matches kicking off at or before this UTC instant.</param>
/// <param name="HomeOrAway">Reserved perspective filter: "Home", "Away", or null for both.</param>
/// <param name="MinClosingHomeOdds">When set, restricts to matches with closing home odds at or above this value.</param>
/// <param name="MaxClosingHomeOdds">When set, restricts to matches with closing home odds at or below this value.</param>
/// <param name="MinMovementPercentage">When set, restricts to matches with movement percentage at or above this value.</param>
/// <param name="MaxMovementPercentage">When set, restricts to matches with movement percentage at or below this value.</param>
public sealed record PatternQueryRequest(
    Guid? CompetitionId,
    Guid? SeasonId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? HomeOrAway,
    decimal? MinClosingHomeOdds,
    decimal? MaxClosingHomeOdds,
    decimal? MinMovementPercentage,
    decimal? MaxMovementPercentage);

/// <summary>
/// A small error payload returned when the backtest proxy cannot reach or read a
/// response from the downstream Python analytics service.
/// </summary>
/// <param name="Error">A short machine-readable error code.</param>
/// <param name="Message">A human-readable description of what went wrong.</param>
/// <param name="Target">The downstream URL the proxy attempted to call.</param>
public sealed record BacktestProxyErrorDto(string Error, string Message, string Target);
