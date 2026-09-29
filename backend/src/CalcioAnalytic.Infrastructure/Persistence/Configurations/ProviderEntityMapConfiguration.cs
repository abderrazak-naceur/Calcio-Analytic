using CalcioAnalytic.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class ProviderEntityMapConfiguration : IEntityTypeConfiguration<ProviderEntityMap>
{
    public void Configure(EntityTypeBuilder<ProviderEntityMap> builder)
    {
        builder.ToTable("provider_entity_maps", CalcioAnalyticDbContext.CatalogSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderId)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.InternalId)
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProviderId, x.EntityType, x.ExternalId })
            .IsUnique();
    }
}
