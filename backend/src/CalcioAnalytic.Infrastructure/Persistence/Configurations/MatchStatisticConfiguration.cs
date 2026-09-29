using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class MatchStatisticConfiguration : IEntityTypeConfiguration<MatchStatistic>
{
    public void Configure(EntityTypeBuilder<MatchStatistic> builder)
    {
        builder.ToTable("match_statistics", CalcioAnalyticDbContext.StatisticsSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchId).IsRequired();
        builder.Property(x => x.TeamId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Value).IsRequired();

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MatchId, x.TeamId });
    }
}
