namespace CalcioAnalytic.Application.Abstractions.Clock;

/// <summary>
/// Abstraction over the system clock, allowing time-dependent logic to be
/// controlled deterministically in tests. Implemented by the Infrastructure layer.
/// </summary>
public interface IClock
{
    /// <summary>The current UTC time.</summary>
    DateTime UtcNow { get; }
}
