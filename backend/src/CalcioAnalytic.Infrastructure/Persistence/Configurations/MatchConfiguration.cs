using CalcioAnalytic.Domain.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CalcioAnalytic.Infrastructure.Persistence.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches", CalcioAnalyticDbContext.MatchesSchema);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompetitionId).IsRequired();
        builder.Property(x => x.SeasonId).IsRequired();
        builder.Property(x => x.HomeTeamId).IsRequired();
        builder.Property(x => x.AwayTeamId).IsRequired();
        builder.Property(x => x.KickoffUtc).IsRequired();

        builder.Property(x => x.Venue).HasMaxLength(200);
        builder.Property(x => x.Round).HasMaxLength(100);
        builder.Property(x => x.Referee).HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(x => x.Competition)
            .WithMany()
            .HasForeignKey(x => x.CompetitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Season)
            .WithMany()
            .HasForeignKey(x => x.SeasonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.HomeTeam)
            .WithMany()
            .HasForeignKey(x => x.HomeTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AwayTeam)
            .WithMany()
            .HasForeignKey(x => x.AwayTeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.CompetitionId);
        builder.HasIndex(x => x.SeasonId);
        builder.HasIndex(x => x.HomeTeamId);
        builder.HasIndex(x => x.AwayTeamId);
        builder.HasIndex(x => x.KickoffUtc);
    }
}
