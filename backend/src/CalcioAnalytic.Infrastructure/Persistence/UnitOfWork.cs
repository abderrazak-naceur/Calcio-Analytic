using CalcioAnalytic.Application.Abstractions.Persistence;

namespace CalcioAnalytic.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>.</summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly CalcioAnalyticDbContext _db;

    public UnitOfWork(CalcioAnalyticDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
