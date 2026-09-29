namespace CalcioAnalytic.Domain.Common;

/// <summary>
/// Abstract base type for all persistent domain entities. Provides a unique
/// identity and UTC audit timestamps. All timestamps are stored in UTC.
/// </summary>
public abstract class Entity
{
    /// <summary>Unique identifier for the entity.</summary>
    public Guid Id { get; set; }

    /// <summary>UTC timestamp captured when the entity was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp of the most recent update, or null if never updated.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}
