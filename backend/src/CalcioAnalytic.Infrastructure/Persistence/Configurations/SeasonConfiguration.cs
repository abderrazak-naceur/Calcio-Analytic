using CalcioAnalytic.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("seasons", CalcioAnalyticDbContext.CatalogSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompetitionId)
            .IsRequired();

        builder.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.StartDate)
            .HasColumnType("date");

        builder.Property(x => x.EndDate)
            .HasColumnType("date");

        builder.HasOne(x => x.Competition)
            .WithMany()
            .HasForeignKey(x => x.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CompetitionId);
    }
}
