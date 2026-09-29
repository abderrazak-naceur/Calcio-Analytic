using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Domain.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class MatchEventConfiguration : IEntityTypeConfiguration<MatchEvent>
{
    public void Configure(EntityTypeBuilder<MatchEvent> builder)
    {
        builder.ToTable("match_events", CalcioAnalyticDbContext.StatisticsSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchId).IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Minute);
        builder.Property(x => x.TeamId);
        builder.Property(x => x.PlayerId);
        builder.Property(x => x.Detail).HasMaxLength(1000);

        builder.HasOne<Match>()
            .WithMany()
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MatchId);
    }
}
