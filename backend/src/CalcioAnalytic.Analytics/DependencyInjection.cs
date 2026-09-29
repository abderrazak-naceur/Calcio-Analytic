using CalcioAnalytic.Analytics.DataQuality;
using CalcioAnalytic.Analytics.Match;
using CalcioAnalytic.Analytics.Patterns;
using CalcioAnalytic.Analytics.Settlement;
using CalcioAnalytic.Analytics.Similarity;
using Microsoft.Extensions.DependencyInjection;

namespace CalcioAnalytic.Analytics;

/// <summary>Registration entry point for the analytics layer.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the analytics engines, including the
    /// <see cref="IMatchAnalysisEngine"/>, the <see cref="ISettlementEngine"/>,
    /// and the <see cref="IHistoricalPatternEngine"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddAnalytics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMatchAnalysisEngine, MatchAnalysisEngine>();
        services.AddSingleton<ISettlementEngine, SettlementEngine>();
        services.AddSingleton<IHistoricalPatternEngine, HistoricalPatternEngine>();
        services.AddSingleton<ISimilarMatchEngine, SimilarMatchEngine>();
        services.AddSingleton<IDataQualityEngine, DataQualityEngine>();

        return services;
    }
}
