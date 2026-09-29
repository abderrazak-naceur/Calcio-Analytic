using CalcioAnalytic.Domain.Common;

namespace CalcioAnalytic.Application.Abstractions.Persistence;

/// <summary>
/// Generic persistence port for a domain <see cref="Entity"/> aggregate.
/// Implemented by the Infrastructure layer; the Application layer depends
/// only on this abstraction.
/// </summary>
/// <typeparam name="T">The domain entity type managed by the repository.</typeparam>
public interface IRepository<T>
    where T : Entity
{
    /// <summary>Retrieves an entity by its identity, or null if not found.</summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all entities of type <typeparamref name="T"/>.</summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);

    /// <summary>Stages a new entity for insertion.</summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task AddAsync(T entity, CancellationToken ct = default);

    /// <summary>Stages modifications to an existing entity.</summary>
    /// <param name="entity">The entity to update.</param>
    void Update(T entity);

    /// <summary>Stages an entity for removal.</summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(T entity);
}
