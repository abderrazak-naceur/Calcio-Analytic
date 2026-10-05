namespace CalcioAnalytic.Api.Contracts.Dtos;

/// <summary>
/// Request body for catalog and fixtures ingestion, identifying a provider
/// competition and season by their provider-native identifiers.
/// </summary>
public sealed record CatalogIngestionRequest(
    string ProviderCode,
    string CompetitionExternalId,
    string SeasonExternalId);

/// <summary>
/// Request body for fixtures ingestion, identifying a provider competition and
/// season by their provider-native identifiers.
/// </summary>
public sealed record FixturesIngestionRequest(
    string ProviderCode,
    string CompetitionExternalId,
    string SeasonExternalId);

/// <summary>
/// Request body for single-fixture, odds, and statistics ingestion, identifying
/// a provider match by its provider-native identifier.
/// </summary>
public sealed record MatchIngestionRequest(
    string ProviderCode,
    string MatchExternalId);

/// <summary>
/// Response for a single-fixture ingestion, carrying the canonical internal
/// match identifier that was created or updated.
/// </summary>
public sealed record FixtureIngestionResponse(Guid MatchId);

/// <summary>
/// Response for an on-demand post-match processing batch (settlement +
/// versioned analysis for finished matches).
/// </summary>
public sealed record PostMatchProcessResponse(
    int Processed,
    int SettledSelections,
    int Failed,
    int RemainingFinished);
