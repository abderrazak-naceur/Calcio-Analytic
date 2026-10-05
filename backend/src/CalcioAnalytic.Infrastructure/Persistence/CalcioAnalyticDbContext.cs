using CalcioAnalytic.Domain.Analytics;
using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Common;
using CalcioAnalytic.Domain.Features;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Domain.Settlement;
using CalcioAnalytic.Domain.Statistics;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for Calcio-Analytic. Maps catalog
/// entities to the "catalog" PostgreSQL schema and matches to the "matches"
/// schema, and auto-populates UTC audit timestamps on save.
/// </summary>
public class CalcioAnalyticDbContext : DbContext
{
    /// <summary>Schema used for catalog reference entities.</summary>
    public const string CatalogSchema = "catalog";

    /// <summary>Schema used for match entities.</summary>
    public const string MatchesSchema = "matches";

    /// <summary>Schema used for odds, market lines, selections, and settlement entities.</summary>
    public const string OddsSchema = "odds";

    /// <summary>Schema used for match statistics and events.</summary>
    public const string StatisticsSchema = "statistics";

    /// <summary>Schema used for versioned match analyses.</summary>
    public const string AnalyticsSchema = "analytics";

    public CalcioAnalyticDbContext(DbContextOptions<CalcioAnalyticDbContext> options)
        : base(options)
    {
    }

    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Bookmaker> Bookmakers => Set<Bookmaker>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<ProviderEntityMap> ProviderEntityMaps => Set<ProviderEntityMap>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MarketLine> MarketLines => Set<MarketLine>();
    public DbSet<Selection> Selections => Set<Selection>();
    public DbSet<OddsSnapshot> OddsSnapshots => Set<OddsSnapshot>();
    public DbSet<MarketSettlement> MarketSettlements => Set<MarketSettlement>();
    public DbSet<MatchStatistic> MatchStatistics => Set<MatchStatistic>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<MatchAnalysis> MatchAnalyses => Set<MatchAnalysis>();
    public DbSet<MatchFeatureSnapshot> MatchFeatureSnapshots => Set<MatchFeatureSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CalcioAnalyticDbContext).Assembly);
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Sets <see cref="Entity.CreatedAtUtc"/> on added entities and
    /// <see cref="Entity.UpdatedAtUtc"/> on modified entities, using UTC.
    /// </summary>
    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    break;
            }
        }
    }
}
