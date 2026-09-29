namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic snapshot of a bookmaker's odds for a match at a point in time
/// as delivered by an external data provider.
/// </summary>
/// <param name="BookmakerExternalId">The provider-native identifier of the bookmaker.</param>
/// <param name="MatchExternalId">The provider-native identifier of the match.</param>
/// <param name="IsLive">Whether the snapshot was captured while the match was in play.</param>
/// <param name="MatchMinute">The optional match minute at which the snapshot was captured.</param>
/// <param name="BookmakerTimestamp">The timestamp the bookmaker attached to the odds.</param>
/// <param name="ProviderTimestamp">The timestamp the provider attached when relaying the snapshot.</param>
/// <param name="Markets">The market lines contained in this snapshot.</param>
/// <param name="RawPayloadHash">An optional hash of the raw provider payload for provenance.</param>
public sealed record ProviderOddsSnapshotDto(
    string BookmakerExternalId,
    string MatchExternalId,
    bool IsLive,
    int? MatchMinute,
    DateTimeOffset BookmakerTimestamp,
    DateTimeOffset ProviderTimestamp,
    IReadOnlyList<ProviderMarketLineDto> Markets,
    string? RawPayloadHash = null);
