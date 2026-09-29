namespace CalcioAnalytic.Workers;

/// <summary>
/// Configuration for the post-match processing worker, bound from the
/// <c>"Worker"</c> configuration section. All members carry sensible defaults so
/// the worker runs correctly even when the section is absent.
/// </summary>
public sealed class WorkerOptions
{
    /// <summary>The configuration section name these options bind from.</summary>
    public const string SectionName = "Worker";

    /// <summary>
    /// The delay, in seconds, between processing cycles. Defaults to 30 seconds.
    /// Values less than or equal to zero are coerced to the default at runtime.
    /// </summary>
    public int IntervalSeconds { get; set; } = 30;
}
