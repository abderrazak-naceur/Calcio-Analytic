using CalcioAnalytic.Application.Abstractions.Clock;
using CalcioAnalytic.Application.Abstractions.Features;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Infrastructure.Persistence;
using CalcioAnalytic.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CalcioAnalytic.Infrastructure;

/// <summary>
/// Registration entry point for the persistence/infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the <see cref="CalcioAnalyticDbContext"/> configured to use
    /// PostgreSQL via the "Postgres" connection string.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        services.AddDbContext<CalcioAnalyticDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IPointInTimeFeatureStore, PointInTimeFeatureStore>();
        services.AddScoped<IOddsLifecycleStore, OddsLifecycleStore>();
        services.AddScoped<IProviderEntityMapRepository, ProviderEntityMapRepository>();
        services.AddScoped<IHistoricalMatchQuery, HistoricalMatchQuery>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
