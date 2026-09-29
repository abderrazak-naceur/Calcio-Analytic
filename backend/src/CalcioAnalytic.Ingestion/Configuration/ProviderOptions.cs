namespace CalcioAnalytic.Ingestion.Configuration;

/// <summary>
/// Options describing how each external provider is configured.
/// Keyed by provider code; secrets are referenced by environment variable
/// name and are never stored inline.
/// </summary>
public sealed class ProviderOptions
{
    /// <summary>
    /// The configuration section name conventionally bound to these options.
    /// </summary>
    public const string SectionName = "Providers";

    /// <summary>
    /// Gets the per-provider endpoint configuration, keyed by provider code.
    /// </summary>
    public IDictionary<string, ProviderEndpointOptions> Providers { get; } =
        new Dictionary<string, ProviderEndpointOptions>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Endpoint configuration for a single provider.
/// </summary>
public sealed class ProviderEndpointOptions
{
    /// <summary>
    /// Gets or sets the base URL for the provider's API.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the name of the environment variable that holds the API key.
    /// The secret value itself is never stored in configuration.
    /// </summary>
    public string? ApiKeyEnvVar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this provider is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
