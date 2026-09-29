using CalcioAnalytic.Application;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Infrastructure.Persistence;
using CalcioAnalytic.Ingestion;
using CalcioAnalytic.Ingestion.Providers.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Api.Tests;

/// <summary>
/// End-to-end verification of the ingestion vertical slice using the deterministic
/// mock provider and an EF Core in-memory database. Proves that a provider fixture
/// becomes one canonical <see cref="Match"/> and that ingestion is idempotent.
/// </summary>
public sealed class IngestionPipelineTests
{
    private const string ProviderCode = MockFileProvider.Code; // "mock"
    private const string CompetitionExternalId = "serie-a";
    private const string SeasonExternalId = "serie-a-2024-2025";
    private const string MatchExternalId = "match-001";

    private static ServiceProvider BuildServices(string dbName)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        var root = new InMemoryDatabaseRoot();
        services.AddDbContext<CalcioAnalyticDbContext>(o => o.UseInMemoryDatabase(dbName, root));

        // Application ports implemented by Infrastructure.
        services.AddApplication();
        services.AddScoped(typeof(Application.Abstractions.Persistence.IRepository<>), typeof(EfRepository<>));
        services.AddScoped<Application.Abstractions.Persistence.IMatchRepository, MatchRepository>();
        services.AddScoped<Application.Abstractions.Persistence.IProviderEntityMapRepository, ProviderEntityMapRepository>();
        services.AddScoped<Application.Abstractions.Persistence.IUnitOfWork, UnitOfWork>();
        services.AddSingleton<Application.Abstractions.Clock.IClock, Infrastructure.Time.SystemClock>();

        services.AddIngestion();
        services.AddMockProvider();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Ingests_mock_fixture_into_one_canonical_match()
    {
        await using var sp = BuildServices(nameof(Ingests_mock_fixture_into_one_canonical_match));

        using var scope = sp.CreateScope();
        var matchIngestion = scope.ServiceProvider.GetRequiredService<IMatchIngestionService>();

        var matchId = await matchIngestion.IngestFixtureAsync(ProviderCode, MatchExternalId);

        Assert.NotNull(matchId);

        var db = scope.ServiceProvider.GetRequiredService<CalcioAnalyticDbContext>();
        var match = await db.Matches.SingleAsync(m => m.Id == matchId!.Value);

        Assert.Equal(MatchStatus.Finished, match.Status);
        Assert.Equal(2, match.HomeScore);
        Assert.Equal(1, match.AwayScore);
        Assert.NotEqual(Guid.Empty, match.HomeTeamId);
        Assert.NotEqual(Guid.Empty, match.AwayTeamId);
        Assert.NotEqual(match.HomeTeamId, match.AwayTeamId);
    }

    [Fact]
    public async Task Ingestion_is_idempotent()
    {
        await using var sp = BuildServices(nameof(Ingestion_is_idempotent));

        // Run the same fixture ingestion twice.
        Guid? first;
        Guid? second;
        using (var scope = sp.CreateScope())
        {
            first = await scope.ServiceProvider
                .GetRequiredService<IMatchIngestionService>()
                .IngestFixtureAsync(ProviderCode, MatchExternalId);
        }

        using (var scope = sp.CreateScope())
        {
            second = await scope.ServiceProvider
                .GetRequiredService<IMatchIngestionService>()
                .IngestFixtureAsync(ProviderCode, MatchExternalId);
        }

        Assert.Equal(first, second);

        using var verify = sp.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<CalcioAnalyticDbContext>();

        // No duplicates were created on the second run: one provider, one match
        // mapping, one match, two teams, one competition.
        Assert.Equal(1, await db.Providers.CountAsync());
        Assert.Equal(1, await db.ProviderEntityMaps.CountAsync(m => m.EntityType == "Match"));
        Assert.Equal(1, await db.Matches.CountAsync());
        Assert.Equal(2, await db.Teams.CountAsync());
        Assert.Equal(1, await db.Competitions.CountAsync());
    }

    [Fact]
    public async Task Catalog_ingestion_populates_bookmakers_and_markets()
    {
        await using var sp = BuildServices(nameof(Catalog_ingestion_populates_bookmakers_and_markets));

        using var scope = sp.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogIngestionService>();

        var result = await catalog.IngestCatalogAsync(ProviderCode, CompetitionExternalId, SeasonExternalId);

        Assert.True(result.TeamsUpserted >= 2);
        Assert.Equal(2, result.BookmakersUpserted);
        Assert.Equal(2, result.MarketsUpserted);

        var db = scope.ServiceProvider.GetRequiredService<CalcioAnalyticDbContext>();
        Assert.Equal(2, await db.Bookmakers.CountAsync());
        Assert.Equal(2, await db.Markets.CountAsync());
    }
}
