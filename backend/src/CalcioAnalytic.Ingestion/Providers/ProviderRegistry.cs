namespace CalcioAnalytic.Ingestion.Providers;

/// <summary>
/// In-memory <see cref="IProviderRegistry"/> backed by the set of registered
/// <see cref="IProviderAdapter"/> instances. Adapters are matched by provider
/// code (case-insensitive) and the requested capability interface.
/// </summary>
public sealed class ProviderRegistry : IProviderRegistry
{
    private readonly IReadOnlyList<IProviderAdapter> _adapters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRegistry"/> class.
    /// </summary>
    /// <param name="adapters">The provider adapters to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="adapters"/> is <c>null</c>.</exception>
    public ProviderRegistry(IEnumerable<IProviderAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        _adapters = adapters.ToList();
    }

    /// <inheritdoc />
    public IEnumerable<string> RegisteredProviderCodes =>
        _adapters
            .Select(a => a.ProviderCode)
            .Distinct(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool TryGet<TCapability>(string providerCode, out TCapability? adapter)
        where TCapability : class, IProviderAdapter
    {
        adapter = _adapters
            .Where(a => string.Equals(a.ProviderCode, providerCode, StringComparison.OrdinalIgnoreCase))
            .OfType<TCapability>()
            .FirstOrDefault();

        return adapter is not null;
    }

    /// <inheritdoc />
    public TCapability Get<TCapability>(string providerCode)
        where TCapability : class, IProviderAdapter
    {
        if (TryGet<TCapability>(providerCode, out var adapter) && adapter is not null)
        {
            return adapter;
        }

        var known = RegisteredProviderCodes.Any()
            ? string.Join(", ", RegisteredProviderCodes)
            : "(none)";

        throw new ProviderRegistrationException(
            $"No provider adapter registered for code '{providerCode}' implementing capability " +
            $"'{typeof(TCapability).Name}'. Registered provider codes: {known}.");
    }
}
