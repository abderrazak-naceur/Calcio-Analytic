using CalcioAnalytic.Domain.Features;
using CalcioAnalytic.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public sealed class MatchFeatureSnapshotConfiguration : IEntityTypeConfiguration<MatchFeatureSnapshot>
{
    public void Configure(EntityTypeBuilder<MatchFeatureSnapshot> builder)
    {
        builder.ToTable("match_feature_snapshots", CalcioAnalyticDbContext.AnalyticsSchema);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FeatureTimestampUtc).IsRequired();
        builder.Property(x => x.KickoffUtc).IsRequired();
        builder.Property(x => x.FeatureSetVersion).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MethodologyVersion).IsRequired().HasMaxLength(50);

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MatchId, x.FeatureTimestampUtc }).IsUnique();
        builder.HasIndex(x => new { x.KickoffUtc, x.FeatureTimestampUtc });
        builder.HasIndex(x => x.FeatureSetVersion);

        // Database-level guard against accidental look-ahead leakage.
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_match_feature_snapshot_pre_kickoff",
            "\"FeatureTimestampUtc\" < \"KickoffUtc\""));
    }
}