using CalcioAnalytic.Application.Analytics;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Application.Settlement;
using CalcioAnalytic.Ingestion.Providers;
using CalcioAnalytic.Ingestion.Providers.Mock;
using CalcioAnalytic.Ingestion.Providers.Sportmonks;
using CalcioAnalytic.Ingestion.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<ICatalogIngestionService, CatalogIngestionService>();
        services.AddScoped<IMatchIngestionService, MatchIngestionService>();
        services.AddScoped<IOddsIngestionService, OddsIngestionService>();
        services.AddScoped<IStatisticsIngestionService, StatisticsIngestionService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<IMatchAnalysisPersistenceService, MatchAnalysisPersistenceService>();
        services.AddScoped<IResultReconciliationService, ResultReconciliationService>();

        return services;
    }

    /// <summary>
    /// Registers the deterministic, embedded-resource backed <see cref="MockFileProvider"/>
    /// as an <see cref="IProviderAdapter"/> (provider code "mock"). Additive and idempotent:
    /// it is safe to call alongside <see cref="AddIngestion"/> and other provider registrations,
    /// and only registers the mock adapter once.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddMockProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var descriptor = ServiceDescriptor.Singleton<IProviderAdapter, MockFileProvider>();
        services.TryAddEnumerable(descriptor);

        return services;
    }

    /// <summary>
    /// Registers the Sportmonks Football API v3 adapter.
    /// The API token is read from the Sportmonks:ApiToken configuration key.
    /// </summary>
    public static IServiceCollection AddSportmonksProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient<SportmonksProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.sportmonks.com/v3/");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddSingleton<IProviderAdapter>(serviceProvider =>
            serviceProvider.GetRequiredService<SportmonksProvider>());

        return services;
    }
}
