using CalcioAnalytic.Infrastructure.Persistence;
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

        return services;
    }
}
