namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Immutable record of a raw provider payload captured verbatim for provenance and auditing.
/// Preserves the exact vendor content so it can be re-parsed or verified later (Phase 4/7).
/// </summary>
/// <param name="ProviderCode">The code identifying the provider the payload came from.</param>
/// <param name="ResourceType">The type of resource the payload represents (e.g. "match", "odds").</param>
/// <param name="ExternalId">The provider-native identifier of the resource the payload describes.</param>
/// <param name="CapturedAtUtc">The timestamp at which the payload was captured.</param>
/// <param name="ContentType">The MIME content type of the payload (e.g. "application/json").</param>
/// <param name="Content">The raw payload content exactly as received.</param>
/// <param name="Sha256Hash">The SHA-256 hash of the content for integrity and de-duplication.</param>
public sealed record RawProviderPayload(
    string ProviderCode,
    string ResourceType,
    string ExternalId,
    DateTimeOffset CapturedAtUtc,
    string ContentType,
    string Content,
    string Sha256Hash);
