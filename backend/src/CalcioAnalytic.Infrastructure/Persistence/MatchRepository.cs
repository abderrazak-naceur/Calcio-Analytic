using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IMatchRepository"/>.</summary>
public sealed class MatchRepository : IMatchRepository
{
    private readonly CalcioAnalyticDbContext _db;

    public MatchRepository(CalcioAnalyticDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task<Match?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Matches.FirstOrDefaultAsync(m => m.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Match>> GetByStatusAsync(MatchStatus status, CancellationToken ct = default)
        => await _db.Matches.AsNoTracking().Where(m => m.Status == status).ToListAsync(ct);

    /// <inheritdoc />
    public async Task AddAsync(Match match, CancellationToken ct = default)
        => await _db.Matches.AddAsync(match, ct);

    /// <inheritdoc />
    public void Update(Match match) => _db.Matches.Update(match);
}
