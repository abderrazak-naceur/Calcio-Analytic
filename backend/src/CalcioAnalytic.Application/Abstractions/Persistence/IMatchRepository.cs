using CalcioAnalytic.Domain.Matches;

namespace CalcioAnalytic.Application.Abstractions.Persistence;

/// <summary>
/// Domain-specific persistence port for <see cref="Match"/> aggregates,
/// exposing query paths beyond the generic repository shape.
/// </summary>
public interface IMatchRepository
{
    /// <summary>Retrieves a match by its identity, or null if not found.</summary>
    /// <param name="id">The unique identifier of the match.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all matches currently in the given lifecycle status.</summary>
    /// <param name="status">The match status to filter by.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<Match>> GetByStatusAsync(MatchStatus status, CancellationToken ct = default);

    /// <summary>Stages a new match for insertion.</summary>
    /// <param name="match">The match to add.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task AddAsync(Match match, CancellationToken ct = default);

    /// <summary>Stages modifications to an existing match.</summary>
    /// <param name="match">The match to update.</param>
    void Update(Match match);
}
