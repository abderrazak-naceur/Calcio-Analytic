using CalcioAnalytic.Analytics;
using CalcioAnalytic.Application;
using CalcioAnalytic.Application.Analytics;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Application.Settlement;
using CalcioAnalytic.Domain.Settlement;
using CalcioAnalytic.Infrastructure.Persistence;
using CalcioAnalytic.Ingestion;
using CalcioAnalytic.Ingestion.Providers.Mock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Api.Tests;

/// <summary>
/// End-to-end verification of the full vertical slice: mock provider ->
/// ingestion (fixture, odds, statistics) -> settlement -> immutable versioned
/// analysis, over an EF Core in-memory database.
/// </summary>
public sealed class FullSliceTests
{
    private const string ProviderCode = MockFileProvider.Code;
    private const string CompetitionExternalId = "serie-a";
    private const string SeasonExternalId = "serie-a-2024-2025";
    private const string MatchExternalId = "match-001";

    private static ServiceProvider BuildServices(string dbName)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));

        var root = new InMemoryDatabaseRoot();
        services.AddDbContext<CalcioAnalyticDbContext>(o => o.UseInMemoryDatabase(dbName, root));

        services.AddApplication();
        services.AddScoped(typeof(Application.Abstractions.Persistence.IRepository<>), typeof(EfRepository<>));
        services.AddScoped<Application.Abstractions.Persistence.IMatchRepository, MatchRepository>();
        services.AddScoped<Application.Abstractions.Persistence.IProviderEntityMapRepository, ProviderEntityMapRepository>();
        services.AddScoped<Application.Abstractions.Persistence.IUnitOfWork, UnitOfWork>();
        services.AddSingleton<Application.Abstractions.Clock.IClock, Infrastructure.Time.SystemClock>();

        services.AddIngestion();
        services.AddMockProvider();
        services.AddAnalytics();

        return services.BuildServiceProvider();
    }

    private static async Task<Guid> IngestFullMatchAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var s = scope.ServiceProvider;

        await s.GetRequiredService<ICatalogIngestionService>()
            .IngestCatalogAsync(ProviderCode, CompetitionExternalId, SeasonExternalId);
        var matchId = await s.GetRequiredService<IMatchIngestionService>()
            .IngestFixtureAsync(ProviderCode, MatchExternalId);
        await s.GetRequiredService<IOddsIngestionService>()
            .IngestOddsAsync(ProviderCode, MatchExternalId);
        await s.GetRequiredService<IStatisticsIngestionService>()
            .IngestStatisticsAsync(ProviderCode, MatchExternalId);

        return matchId!.Value;
    }

    [Fact]
    public async Task Odds_snapshots_are_ingested_and_deduplicated()
    {
        await using var sp = BuildServices(nameof(Odds_snapshots_are_ingested_and_deduplicated));

        var matchId = await IngestFullMatchAsync(sp);

        // Re-run odds ingestion: append-only, so no new snapshots on the second pass.
        using (var scope = sp.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IOddsIngestionService>()
                .IngestOddsAsync(ProviderCode, MatchExternalId);
        }

        using var verify = sp.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<CalcioAnalyticDbContext>();
        // The mock provides 6 snapshots x (3 1X2 + 2 O/U) selections = 30 selection-level snapshots.
        var count = await db.OddsSnapshots.CountAsync(s => s.MatchId == matchId);
        Assert.True(count > 0);

        // A second identical odds run must not double the snapshot count.
        var distinctHashes = await db.OddsSnapshots
            .Where(s => s.MatchId == matchId)
            .Select(s => s.PayloadHash)
            .Distinct()
            .CountAsync();
        Assert.Equal(count, distinctHashes);
    }

    [Fact]
    public async Task Settlement_settles_supported_markets_for_finished_match()
    {
        await using var sp = BuildServices(nameof(Settlement_settles_supported_markets_for_finished_match));

        var matchId = await IngestFullMatchAsync(sp);

        int settled;
        using (var scope = sp.CreateScope())
        {
            settled = await scope.ServiceProvider.GetRequiredService<ISettlementService>()
                .SettleMatchAsync(matchId);
        }

        Assert.True(settled > 0);

        using var verify = sp.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<CalcioAnalyticDbContext>();

        // Match is Inter 2-1 Juventus (home win, 3 goals).
        // 1X2: exactly one Won among Home/Draw/Away. Over/Under 2.5: Over Won. BTTS Yes also wins.
        var settlements = await db.MarketSettlements
            .Where(s => s.MatchId == matchId)
            .ToListAsync();

        Assert.NotEmpty(settlements);
        Assert.Contains(settlements, s => s.Status == SettlementStatus.Won);
        Assert.Contains(settlements, s => s.Status == SettlementStatus.Lost);
        // No selection should be left Unknown for these supported markets.
        Assert.DoesNotContain(settlements, s => s.Status == SettlementStatus.Unknown);

        var bttsLine = await (
            from line in db.MarketLines
            join market in db.Markets on line.MarketId equals market.Id
            where line.MatchId == matchId && market.Code == "BTTS"
            select line.Id).SingleAsync();
        var btts = settlements.Where(s => s.MarketLineId == bttsLine).ToList();
        Assert.Equal(2, btts.Count);
        Assert.Contains(btts, s => s.Status == SettlementStatus.Won);
        Assert.Contains(btts, s => s.Status == SettlementStatus.Lost);
    }

    [Fact]
    public async Task Analysis_is_versioned_and_immutable()
    {
        await using var sp = BuildServices(nameof(Analysis_is_versioned_and_immutable));

        var matchId = await IngestFullMatchAsync(sp);

        int v1;
        int v2;
        using (var scope = sp.CreateScope())
        {
            v1 = await scope.ServiceProvider.GetRequiredService<IMatchAnalysisPersistenceService>()
                .GenerateAndStoreAsync(matchId);
        }

        using (var scope = sp.CreateScope())
        {
            v2 = await scope.ServiceProvider.GetRequiredService<IMatchAnalysisPersistenceService>()
                .GenerateAndStoreAsync(matchId);
        }

        Assert.Equal(1, v1);
        Assert.Equal(2, v2);

        using var verify = sp.CreateScope();
        var db = verify.ServiceProvider.GetRequiredService<CalcioAnalyticDbContext>();
        var analyses = await db.MatchAnalyses
            .Where(a => a.MatchId == matchId)
            .OrderBy(a => a.Version)
            .ToListAsync();

        // Two immutable versions exist; both carry non-empty JSON.
        Assert.Equal(2, analyses.Count);
        Assert.Equal(new[] { 1, 2 }, analyses.Select(a => a.Version).ToArray());
        Assert.All(analyses, a => Assert.False(string.IsNullOrWhiteSpace(a.AnalysisJson)));
    }
}
