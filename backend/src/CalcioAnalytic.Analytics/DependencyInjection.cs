using CalcioAnalytic.Analytics.Match;
using Microsoft.Extensions.DependencyInjection;

namespace CalcioAnalytic.Analytics;

/// <summary>Registration entry point for the analytics layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the analytics engines, including the
    /// <see cref="IMatchAnalysisEngine"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddAnalytics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMatchAnalysisEngine, MatchAnalysisEngine>();

        return services;
    }
}
