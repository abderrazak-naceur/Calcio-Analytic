using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Domain.Settlement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class MarketSettlementConfiguration : IEntityTypeConfiguration<MarketSettlement>
{
    public void Configure(EntityTypeBuilder<MarketSettlement> builder)
    {
        builder.ToTable("market_settlements", CalcioAnalyticDbContext.OddsSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchId).IsRequired();
        builder.Property(x => x.MarketLineId).IsRequired();
        builder.Property(x => x.SelectionId).IsRequired();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.SettledAtUtc);

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MarketLine>()
            .WithMany()
            .HasForeignKey(x => x.MarketLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Selection>()
            .WithMany()
            .HasForeignKey(x => x.SelectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MatchId, x.MarketLineId, x.SelectionId });
    }
}
