using CalcioAnalytic.Analytics;
using CalcioAnalytic.Analytics.Match;
using CalcioAnalytic.Analytics.Odds;
using CalcioAnalytic.Application;
using CalcioAnalytic.Application.Analytics;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Application.Settlement;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Domain.Settlement;
using CalcioAnalytic.Infrastructure.Persistence;
using CalcioAnalytic.Ingestion;
using CalcioAnalytic.Ingestion.Providers.Mock;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Api.Tests;

/// <summary>
/// Verifies the odds-movement (TASK-027) and bookmaker-dispersion (TASK-028)
/// analyzers both as pure-engine unit tests and through the AnalyticsController
/// integration path using the deterministic mock provider.
/// </summary>
public sealed class OddsMovementAndBookmakerTests
{
    private readonly Guid _matchId = Guid.NewGuid();
    private readonly Guid _bookmakerA = Guid.NewGuid();
    private readonly Guid _bookmakerB = Guid.NewGuid();
    private readonly Guid _marketLineId = Guid.NewGuid();
    private readonly Guid _selectionId = Guid.NewGuid();

    private static OddsSnapshot Snapshot(
        Guid matchId, Guid bookmakerId, Guid marketLineId, Guid selectionId,
        decimal odds, DateTime providerTs, DateTime ingestionTs) => new()
    {
        Id = Guid.NewGuid(),
        MatchId = matchId,
        BookmakerId = bookmakerId,
        MarketLineId = marketLineId,
        SelectionId = selectionId,
        DecimalOdds = odds,
        ImpliedProbability = 1m / odds,
        ProviderTimestampUtc = providerTs,
        IngestionTimestampUtc = ingestionTs,
        Kind = OddsSnapshotKind.Closing,
        BookmakerTimestampUtc = providerTs,
        PayloadHash = Guid.NewGuid().ToString(),
    };

    #region OddsMovementAnalyzer unit tests (TASK-027)

    [Fact]
    public void Movement_analyzes_single_snapshot_as_no_change()
    {
        var snap = Snapshot(_matchId, _bookmakerA, _marketLineId, _selectionId,
            2.00m, new DateTime(2024, 10, 27, 19, 45, 0, DateTimeKind.Utc),
            new DateTime(2024, 10, 27, 19, 45, 1, DateTimeKind.Utc));

        var result = OddsMovementAnalyzer.Analyze([snap]);

        Assert.Equal(1, result.SampleCount);
        Assert.Equal(2.00m, result.OpeningOdds);
        Assert.Equal(2.00m, result.ClosingOdds);
        Assert.Equal(2.00m, result.MinOdds);
        Assert.Equal(2.00m, result.MaxOdds);
        Assert.Equal(0, result.NumberOfChanges);
        Assert.Equal(0m, result.MovementAbsolute);
        Assert.Equal(0m, result.MovementPercentage);
        Assert.Equal(0m, result.Volatility);
    }

    [Fact]
    public void Movement_handles_empty_sequence()
    {
        var result = OddsMovementAnalyzer.Analyze([]);

        Assert.Equal(0, result.SampleCount);
        Assert.Equal(0m, result.OpeningOdds);
        Assert.Equal(0m, result.ClosingOdds);
        Assert.Equal(0, result.NumberOfChanges);
    }

    [Fact]
    public void Movement_handles_null_input()
    {
        var result = OddsMovementAnalyzer.Analyze(null!);

        Assert.Equal(0, result.SampleCount);
        Assert.Equal(0m, result.OpeningOdds);
    }

    [Fact]
    public void Movement_computes_open_close_and_change_count()
    {
        var t0 = new DateTime(2024, 10, 25, 19, 45, 5, DateTimeKind.Utc);
        var t1 = new DateTime(2024, 10, 27, 17, 45, 4, DateTimeKind.Utc);
        var t2 = new DateTime(2024, 10, 27, 19, 40, 3, DateTimeKind.Utc);

        var snaps = new[]
        {
            Snapshot(_matchId, _bookmakerA, _marketLineId, _selectionId, 2.10m, t0, t0),
            Snapshot(_matchId, _bookmakerA, _marketLineId, _selectionId, 1.95m, t1, t1),
            Snapshot(_matchId, _bookmakerA, _marketLineId, _selectionId, 1.85m, t2, t2),
        };

        var result = OddsMovementAnalyzer.Analyze(snaps);

        Assert.Equal(3, result.SampleCount);
        Assert.Equal(2.10m, result.OpeningOdds);
        Assert.Equal(1.85m, result.ClosingOdds);
        Assert.Equal(1.85m, result.MinOdds);
        Assert.Equal(2.10m, result.MaxOdds);
        Assert.Equal(2, result.NumberOfChanges);
        Assert.Equal(-0.25m, result.MovementAbsolute);
        // (1.85 - 2.10) / 2.10 = -0.119047...
        Assert.Equal(Math.Round((1.85m - 2.10m) / 2.10m, 5), Math.Round(result.MovementPercentage, 5));
        Assert.True(result.Volatility > 0);
    }

    [Fact]
    public void Movement_orders_snapshots_defensively_by_provider_timestamp()
    {
        // Deliberately out of order; analyzer should sort by provider timestamp.
        var t0 = new DateTime(2024, 10, 25, 19, 45, 5, DateTimeKind.Utc);
        var t1 = new DateTime(2024, 10, 27, 17, 45, 4, DateTimeKind.Utc);

        var snaps = new[]
        {
            Snapshot(_matchId, _bookmakerA, _marketLineId, _selectionId, 1.95m, t1, t1),
            Snapshot(_matchId, _bookmakerA, _marketLineId, _selectionId, 2.10m, t0, t0),
        };

        var result = OddsMovementAnalyzer.Analyze(snaps);

        Assert.Equal(2.10m, result.OpeningOdds);
        Assert.Equal(1.95m, result.ClosingOdds);
        Assert.Equal(-0.15m, result.MovementAbsolute);
    }

    #endregion

    #region BookmakerAnalyzer unit tests (TASK-028)

    [Fact]
    public void Dispersion_uses_latest_per_bookmaker_and_computes_spread()
    {
        var t0 = new DateTime(2024, 10, 25, 19, 45, 5, DateTimeKind.Utc);
        var t1 = new DateTime(2024, 10, 27, 19, 40, 3, DateTimeKind.Utc);

        // Bookmaker A: latest = 1.85
        // Bookmaker B: latest = 1.83
        var snaps = new[]
        {
            Snapshot(_matchId, _bookmakerA, _marketLineId, _selectionId, 2.10m, t0, t0),
            Snapshot(_matchId, _bookmakerA, _marketLineId, _selectionId, 1.85m, t1, t1),
            Snapshot(_matchId, _bookmakerB, _marketLineId, _selectionId, 2.05m, t0, t0),
            Snapshot(_matchId, _bookmakerB, _marketLineId, _selectionId, 1.83m, t1, t1),
        };

        var result = BookmakerAnalyzer.AnalyzeLatestPerBookmaker(snaps);

        Assert.Equal(2, result.BookmakerCount);
        Assert.Equal(1.85m, result.BestOdds);
        Assert.Equal(_bookmakerA, result.BestBookmakerId);
        Assert.Equal(1.83m, result.WorstOdds);
        Assert.Equal(_bookmakerB, result.WorstBookmakerId);
        Assert.Equal(1.84m, result.AverageOdds);
        Assert.True(result.Dispersion > 0);
    }

    [Fact]
    public void Dispersion_handles_empty_input()
    {
        var result = BookmakerAnalyzer.AnalyzeLatestPerBookmaker([]);

        Assert.Equal(0, result.BookmakerCount);
        Assert.Equal(0m, result.BestOdds);
        Assert.Equal(0m, result.WorstOdds);
    }

    [Fact]
    public void Dispersion_handles_null_input()
    {
        var result = BookmakerAnalyzer.AnalyzeLatestPerBookmaker(null!);

        Assert.Equal(0, result.BookmakerCount);
        Assert.Equal(0m, result.BestOdds);
    }

    #endregion

    #region AnalyticsController integration tests (TASK-027 + TASK-028)

    [Fact]
    public async Task AnalyticsController_GetMovement_returns_per_selection_movement()
    {
        await using var sp = BuildServices(nameof(AnalyticsController_GetMovement_returns_per_selection_movement));

        var matchId = await IngestFullMatchAsync(sp);
        var controller = CreateController(sp);

        var result = await controller.GetMovement(matchId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var movements = Assert.IsAssignableFrom<IEnumerable<OddsMovementDto>>(ok.Value);

        Assert.NotEmpty(movements);
        var movementList = movements.ToList();

        // The mock data has bet365 and williamhill, each with 1X2 (3 selections)
        // + O/U 2.5 (2 selections) + BTTS (2 selections) = 7 selections per bookmaker, 14 total.
        Assert.Equal(14, movementList.Count);

        // Every movement has at least a sample count of 1 and non-null opening/closing.
        Assert.All(movementList, m =>
        {
            Assert.True(m.Movement.SampleCount >= 1);
            Assert.True(m.Movement.OpeningOdds > 0);
            Assert.True(m.Movement.ClosingOdds > 0);
        });
    }

    [Fact]
    public async Task AnalyticsController_GetBookmakers_returns_cross_bookmaker_dispersion()
    {
        await using var sp = BuildServices(nameof(AnalyticsController_GetBookmakers_returns_cross_bookmaker_dispersion));

        var matchId = await IngestFullMatchAsync(sp);
        var controller = CreateController(sp);

        var result = await controller.GetBookmakers(matchId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dispersions = Assert.IsAssignableFrom<IReadOnlyList<BookmakerDispersionDto>>(ok.Value);

        Assert.NotEmpty(dispersions);

        // 7 selections across 1X2 (3) + O/U 2.5 (2) + BTTS (2) = 7 market-line/selection groups.
        Assert.Equal(7, dispersions.Count);

        // Each group should have both bookmakers contributing (count = 2).
        Assert.All(dispersions, d =>
        {
            Assert.Equal(2, d.Dispersion.BookmakerCount);
            Assert.True(d.Dispersion.BestOdds > 0);
            Assert.True(d.Dispersion.WorstOdds > 0);
        });
    }

    [Fact]
    public async Task AnalyticsController_GetMovement_returns_empty_for_unknown_match()
    {
        await using var sp = BuildServices(nameof(AnalyticsController_GetMovement_returns_empty_for_unknown_match));

        var controller = CreateController(sp);
        var bogusId = Guid.NewGuid();

        var result = await controller.GetMovement(bogusId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var movements = Assert.IsAssignableFrom<IReadOnlyList<OddsMovementDto>>(ok.Value);

        Assert.Empty(movements);
    }

    [Fact]
    public async Task AnalyticsController_GetBookmakers_returns_empty_for_unknown_match()
    {
        await using var sp = BuildServices(nameof(AnalyticsController_GetBookmakers_returns_empty_for_unknown_match));

        var controller = CreateController(sp);
        var bogusId = Guid.NewGuid();

        var result = await controller.GetBookmakers(bogusId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dispersions = Assert.IsAssignableFrom<IReadOnlyList<BookmakerDispersionDto>>(ok.Value);

        Assert.Empty(dispersions);
    }

    #endregion

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
        services.AddScoped<Application.Abstractions.Persistence.IOddsLifecycleStore, OddsLifecycleStore>();
        services.AddSingleton<Application.Abstractions.Clock.IClock, Infrastructure.Time.SystemClock>();

        services.AddIngestion();
        services.AddMockProvider();
        services.AddAnalytics();

        return services.BuildServiceProvider();
    }

    private static readonly string ProviderCode = MockFileProvider.Code;
    private const string CompetitionExternalId = "serie-a";
    private const string SeasonExternalId = "serie-a-2024-2025";
    private const string MatchExternalId = "match-001";

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

    private static AnalyticsController CreateController(IServiceProvider sp)
    {
        var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CalcioAnalyticDbContext>();
        var engine = scope.ServiceProvider.GetRequiredService<IMatchAnalysisEngine>();
        var controller = new AnalyticsController(db, engine);
        return controller;
    }
}
