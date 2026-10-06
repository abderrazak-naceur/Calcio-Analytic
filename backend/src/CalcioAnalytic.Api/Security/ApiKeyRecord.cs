namespace CalcioAnalytic.Api.Security;

/// <summary>Persisted-safe representation of a customer API key.</summary>
public sealed class ApiKeyRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string KeyPrefix { get; init; }
    public required string KeyHash { get; init; }
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
}
