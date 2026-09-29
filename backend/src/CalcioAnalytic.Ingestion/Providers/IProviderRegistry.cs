namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// Resolves provider adapters by provider code and requested capability,
/// so the application can depend on capabilities rather than a concrete vendor.
/// </summary>
public interface IProviderRegistry
{
    /// <summary>
    /// Gets the codes of all registered providers.
    /// </summary>
    IEnumerable<string> RegisteredProviderCodes { get; }

    /// <summary>
    /// Attempts to resolve the adapter for the given provider code that also
    /// implements the requested capability.
    /// </summary>
    /// <typeparam name="TCapability">The capability interface to resolve.</typeparam>
    /// <param name="providerCode">The provider code (matched case-insensitively).</param>
    /// <param name="adapter">The resolved adapter, or <c>null</c> when no match is found.</param>
    /// <returns><c>true</c> when a matching adapter was found; otherwise <c>false</c>.</returns>
    bool TryGet<TCapability>(string providerCode, out TCapability? adapter)
        where TCapability : class, IProviderAdapter;

    /// <summary>
    /// Resolves the adapter for the given provider code that also implements
    /// the requested capability.
    /// </summary>
    /// <typeparam name="TCapability">The capability interface to resolve.</typeparam>
    /// <param name="providerCode">The provider code (matched case-insensitively).</param>
    /// <returns>The resolved adapter.</returns>
    /// <exception cref="ProviderRegistrationException">
    /// Thrown when no adapter matches the provider code and capability.
    /// </exception>
    TCapability Get<TCapability>(string providerCode)
        where TCapability : class, IProviderAdapter;
}
