using CalcioAnalytic.Domain.Analytics;
using CalcioAnalytic.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class MatchAnalysisConfiguration : IEntityTypeConfiguration<MatchAnalysis>
{
    public void Configure(EntityTypeBuilder<MatchAnalysis> builder)
    {
        builder.ToTable("match_analyses", CalcioAnalyticDbContext.AnalyticsSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchId).IsRequired();
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.GeneratedAtUtc).IsRequired();

        builder.Property(x => x.AnalysisJson)
            .IsRequired();

        builder.Property(x => x.MethodologyVersion)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Each version of an analysis for a match is unique and immutable.
        builder.HasIndex(x => new { x.MatchId, x.Version }).IsUnique();
    }
}
