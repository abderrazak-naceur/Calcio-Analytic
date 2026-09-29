namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Base marker interface implemented by every provider adapter.
/// Capabilities (fixtures, odds, statistics, ...) are expressed as
/// derived interfaces so the application can depend on capabilities
/// rather than a single vendor.
/// </summary>
public interface IProviderAdapter
{
    /// <summary>
    /// Gets the stable code that identifies this provider (e.g. "api-football").
    /// Used by the registry to resolve adapters by provider and capability.
    /// </summary>
    string ProviderCode { get; }
}
