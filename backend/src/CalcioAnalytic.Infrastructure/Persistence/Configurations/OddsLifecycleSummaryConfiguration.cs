using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public sealed class OddsLifecycleSummaryConfiguration : IEntityTypeConfiguration<OddsLifecycleSummary>
{
    public void Configure(EntityTypeBuilder<OddsLifecycleSummary> builder)
    {
        builder.ToTable("odds_lifecycle_summaries", CalcioAnalyticDbContext.OddsSchema);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OpeningOdds);
        builder.Property(x => x.CurrentOdds);
        builder.Property(x => x.PreKickoffOdds);
        builder.Property(x => x.ClosingOdds);
        builder.Property(x => x.MinOdds);
        builder.Property(x => x.MaxOdds);

        builder.HasOne<Match>().WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Provider>().WithMany().HasForeignKey(x => x.ProviderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Bookmaker>().WithMany().HasForeignKey(x => x.BookmakerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MarketLine>().WithMany().HasForeignKey(x => x.MarketLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Selection>().WithMany().HasForeignKey(x => x.SelectionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.MatchId,
            x.ProviderId,
            x.BookmakerId,
            x.MarketLineId,
            x.SelectionId
        }).IsUnique();

        builder.HasIndex(x => new { x.MatchId, x.BookmakerId, x.MarketLineId });
    }
}
