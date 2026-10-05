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

    /// <summary>Enables the scheduled SportMonks synchronization loop.</summary>
    public bool SportmonksSyncEnabled { get; set; }

    /// <summary>Minimum delay between SportMonks HTTP-backed ingestion operations.</summary>
    public int SportmonksRequestDelayMilliseconds { get; set; } = 1000;

    /// <summary>Maximum number of matches synchronized in one cycle.</summary>
    public int SportmonksBatchSize { get; set; } = 25;

    /// <summary>How far ahead scheduled/pre-match fixtures are synchronized.</summary>
    public int SportmonksLookaheadHours { get; set; } = 24;

    /// <summary>How far back finished fixtures are rechecked for result corrections.</summary>
    public int SportmonksRecentHours { get; set; } = 12;
}
