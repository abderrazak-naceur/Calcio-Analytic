using CalcioAnalytic.Domain.Odds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class SelectionConfiguration : IEntityTypeConfiguration<Selection>
{
    public void Configure(EntityTypeBuilder<Selection> builder)
    {
        builder.ToTable("selections", CalcioAnalyticDbContext.OddsSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MarketLineId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne<MarketLine>()
            .WithMany()
            .HasForeignKey(x => x.MarketLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MarketLineId);
    }
}
