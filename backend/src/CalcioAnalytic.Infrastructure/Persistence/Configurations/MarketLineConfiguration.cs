using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class MarketLineConfiguration : IEntityTypeConfiguration<MarketLine>
{
    public void Configure(EntityTypeBuilder<MarketLine> builder)
    {
        builder.ToTable("market_lines", CalcioAnalyticDbContext.OddsSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchId).IsRequired();
        builder.Property(x => x.MarketId).IsRequired();
        builder.Property(x => x.Line);
        builder.Property(x => x.Period).HasMaxLength(50);

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Market>()
            .WithMany()
            .HasForeignKey(x => x.MarketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MatchId, x.MarketId });
    }
}
