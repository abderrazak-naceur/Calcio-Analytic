using CalcioAnalytic.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class BookmakerConfiguration : IEntityTypeConfiguration<Bookmaker>
{
    public void Configure(EntityTypeBuilder<Bookmaker> builder)
    {
        builder.ToTable("bookmakers", CalcioAnalyticDbContext.CatalogSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
