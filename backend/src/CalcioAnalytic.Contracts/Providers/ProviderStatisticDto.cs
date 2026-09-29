namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a per-team match statistic as delivered by an external data provider.
/// </summary>
/// <param name="MatchExternalId">The provider-native identifier of the match.</param>
/// <param name="TeamExternalId">The provider-native identifier of the team the statistic applies to.</param>
/// <param name="Name">The statistic name (e.g. "possession", "shots").</param>
/// <param name="Value">The statistic value.</param>
public sealed record ProviderStatisticDto(
    string MatchExternalId,
    string TeamExternalId,
    string Name,
    decimal Value);
