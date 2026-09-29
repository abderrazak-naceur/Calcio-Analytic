using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CalcioAnalytic.Infrastructure;

/// <summary>
/// Design-time factory so EF Core tooling (e.g. <c>dotnet ef migrations add</c>)
/// can construct the context without the API host running. Reads the connection
/// string from the <c>ConnectionStrings__Postgres</c> environment variable and
/// falls back to a localhost default.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CalcioAnalyticDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=calcio_analytic;Username=postgres;Password=postgres";

    public CalcioAnalyticDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<CalcioAnalyticDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CalcioAnalyticDbContext(options);
    }
}
