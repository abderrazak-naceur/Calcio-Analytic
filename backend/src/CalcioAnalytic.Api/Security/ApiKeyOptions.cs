namespace CalcioAnalytic.Api.Security;

/// <summary>
/// Options controlling API-key authentication for write endpoints. Bound from
/// the configuration section <c>ApiKey</c>. Disabled by default so local
/// development and integration tests are unaffected unless explicitly enabled.
/// </summary>
public sealed class ApiKeyOptions
{
    /// <summary>The configuration section these options bind from.</summary>
    public const string SectionName = "ApiKey";

    /// <summary>
    /// When <c>true</c>, protected endpoints require a valid API key header.
    /// Defaults to <c>false</c> so reads, health checks and tests stay anonymous.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>The request header carrying the API key. Defaults to <c>X-Api-Key</c>.</summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>The set of accepted API keys. Empty by default.</summary>
    public string[] Keys { get; set; } = Array.Empty<string>();
}
