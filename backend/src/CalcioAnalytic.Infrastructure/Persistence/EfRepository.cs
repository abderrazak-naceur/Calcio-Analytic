using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Infrastructure.Persistence;

/// <summary>
/// Generic EF Core implementation of <see cref="IRepository{T}"/>. Staging
/// methods do not persist; call <see cref="IUnitOfWork.SaveChangesAsync"/> to
/// commit.
/// </summary>
/// <typeparam name="T">The domain entity type managed by the repository.</typeparam>
public class EfRepository<T> : IRepository<T>
    where T : Entity
{
    protected readonly CalcioAnalyticDbContext Db;

    public EfRepository(CalcioAnalyticDbContext db)
    {
        Db = db;
    }

    /// <inheritdoc />
    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Db.Set<T>().FirstOrDefaultAsync(e => e.Id == id, ct);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default)
        => await Db.Set<T>().AsNoTracking().ToListAsync(ct);

    /// <inheritdoc />
    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await Db.Set<T>().AddAsync(entity, ct);

    /// <inheritdoc />
    public virtual void Update(T entity) => Db.Set<T>().Update(entity);

    /// <inheritdoc />
    public virtual void Remove(T entity) => Db.Set<T>().Remove(entity);
}
