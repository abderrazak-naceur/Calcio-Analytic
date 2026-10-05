using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Tests;

public sealed class FeatureSnapshotPopulationTests
{
    [Fact]
    public async Task Populates_recent_form_strictly_before_kickoff()
    {
        var options = new DbContextOptionsBuilder<CalcioAnalyticDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new CalcioAnalyticDbContext(options);
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        db.Matches.Add(new Match
        {
            Id = Guid.NewGuid(),
            HomeTeamId = home,
            AwayTeamId = away,
            CompetitionId = Guid.NewGuid(),
            SeasonId = Guid.NewGuid(),
            KickoffUtc = DateTime.UtcNow.AddDays(-1),
            Status = MatchStatus.Finished,
            HomeScore = 2,
            AwayScore = 0,
        });
        var targetId = Guid.NewGuid();
        db.Matches.Add(new Match
        {
            Id = targetId,
            HomeTeamId = home,
            AwayTeamId = away,
            CompetitionId = Guid.NewGuid(),
            SeasonId = Guid.NewGuid(),
            KickoffUtc = DateTime.UtcNow.AddHours(-1),
            Status = MatchStatus.Finished,
            HomeScore = 1,
            AwayScore = 1,
        });
        await db.SaveChangesAsync();

        var service = new FeatureSnapshotPopulationService(db);
        var created = await service.PopulateMissingAsync(10);

        Assert.Equal(2, created);
        var snapshot = await db.MatchFeatureSnapshots.SingleAsync(x => x.MatchId == targetId);
        Assert.True(snapshot.FeatureTimestampUtc < snapshot.KickoffUtc);
        Assert.Equal(3, snapshot.HomeFormLast5Points);
        Assert.Equal(0, snapshot.AwayFormLast5Points);
    }
}
