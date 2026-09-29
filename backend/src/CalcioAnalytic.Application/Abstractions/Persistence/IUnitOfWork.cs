namespace CalcioAnalytic.Application.Abstractions.Persistence;

/// <summary>
/// Coordinates the atomic commit of staged repository changes as a single
/// transaction boundary. Implemented by the Infrastructure layer.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes and returns the number of state entries
    /// written to the underlying store.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
