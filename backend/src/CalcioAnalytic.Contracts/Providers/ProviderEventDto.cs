namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of an in-match event as delivered by an external data provider.
/// </summary>
/// <param name="MatchExternalId">The provider-native identifier of the match the event belongs to.</param>
/// <param name="Type">The event type (e.g. "goal", "card", "substitution", "var", "penalty", "corner").</param>
/// <param name="Minute">The optional match minute at which the event occurred.</param>
/// <param name="TeamExternalId">The optional provider-native identifier of the team involved.</param>
/// <param name="PlayerExternalId">The optional provider-native identifier of the player involved.</param>
/// <param name="Detail">An optional additional detail describing the event.</param>
public sealed record ProviderEventDto(
    string MatchExternalId,
    string Type,
    int? Minute = null,
    string? TeamExternalId = null,
    string? PlayerExternalId = null,
    string? Detail = null);
