using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Odds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class OddsSnapshotConfiguration : IEntityTypeConfiguration<OddsSnapshot>
{
    public void Configure(EntityTypeBuilder<OddsSnapshot> builder)
    {
        builder.ToTable("odds_snapshots", CalcioAnalyticDbContext.OddsSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchId).IsRequired();
        builder.Property(x => x.BookmakerId).IsRequired();
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.MarketLineId).IsRequired();
        builder.Property(x => x.SelectionId).IsRequired();

        builder.Property(x => x.DecimalOdds).IsRequired();
        builder.Property(x => x.FractionalOdds).HasMaxLength(50);
        builder.Property(x => x.AmericanOdds);
        builder.Property(x => x.ImpliedProbability).IsRequired();
        builder.Property(x => x.IsLive).IsRequired();
        builder.Property(x => x.MatchMinute);
        builder.Property(x => x.Period).HasMaxLength(50);

        builder.Property(x => x.Kind)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.BookmakerTimestampUtc).IsRequired();
        builder.Property(x => x.ProviderTimestampUtc).IsRequired();
        builder.Property(x => x.IngestionTimestampUtc).IsRequired();

        builder.Property(x => x.PayloadHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Bookmaker>()
            .WithMany()
            .HasForeignKey(x => x.BookmakerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Provider>()
            .WithMany()
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MarketLine>()
            .WithMany()
            .HasForeignKey(x => x.MarketLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Selection>()
            .WithMany()
            .HasForeignKey(x => x.SelectionId)
            .OnDelete(DeleteBehavior.Restrict);

        // History query path: retrieve the price series for a selection over time.
        builder.HasIndex(x => new
        {
            x.MatchId,
            x.ProviderId,
            x.BookmakerId,
            x.MarketLineId,
            x.SelectionId,
            x.ProviderTimestampUtc
        });

        // Deduplicate identical snapshots to guarantee append-only reproducibility.
        builder.HasIndex(x => x.PayloadHash).IsUnique();
    }
}
