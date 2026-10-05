using CalcioAnalytic.Domain.Matches;

namespace CalcioAnalytic.Application.Abstractions.Persistence;

public sealed record HistoricalMatchCandidate(
    Guid MatchId,
    string ExternalId,
    MatchStatus Status);

public interface IHistoricalMatchQuery
{
    Task<IReadOnlyList<HistoricalMatchCandidate>> FindAsync(
        string providerCode,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);
}
