using CalcioAnalytic.Ingestion.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace CalcioAnalytic.Ingestion;

/// <summary>
/// Dependency injection helpers for the ingestion layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the ingestion services, including the provider registry.
    /// Concrete provider adapters are registered separately as
    /// <see cref="IProviderAdapter"/> implementations.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddIngestion(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IProviderRegistry, ProviderRegistry>();

        return services;
    }
}
