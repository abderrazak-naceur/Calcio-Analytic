using CalcioAnalytic.Analytics.Match;
using CalcioAnalytic.Analytics.Settlement;
using Microsoft.Extensions.DependencyInjection;

namespace CalcioAnalytic.Analytics;

/// <summary>Registration entry point for the analytics layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the analytics engines, including the
    /// <see cref="IMatchAnalysisEngine"/> and the <see cref="ISettlementEngine"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddAnalytics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMatchAnalysisEngine, MatchAnalysisEngine>();
        services.AddSingleton<ISettlementEngine, SettlementEngine>();

        return services;
    }
}
