using CalcioAnalytic.Domain.Features;

namespace CalcioAnalytic.Application.Abstractions.Features;

public interface IPointInTimeFeatureStore
{
    Task<MatchFeatureSnapshot?> GetLatestBeforeKickoffAsync(
        Guid matchId,
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MatchFeatureSnapshot snapshot,
        CancellationToken cancellationToken = default);
}