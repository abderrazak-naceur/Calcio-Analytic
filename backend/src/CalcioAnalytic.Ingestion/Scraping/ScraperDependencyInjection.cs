using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CalcioAnalytic.Ingestion.Scraping;

public static class ScraperDependencyInjection
{
    public static IServiceCollection AddScraping(
        this IServiceCollection services,
        Action<ScraperOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null)
            services.Configure(configure);

        services.Configure<FootballDataCsvOptions>(_ => { });

        services.AddHttpClient<FootballDataCsvSource>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FootballDataCsvOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
        });

        services.AddHttpClient<IScraperSource, HttpScraperSource>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ScraperOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.RequestTimeoutSeconds));
        });

        return services;
    }
}
