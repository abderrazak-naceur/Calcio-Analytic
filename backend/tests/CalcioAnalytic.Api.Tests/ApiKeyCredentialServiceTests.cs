using CalcioAnalytic.Api.Security;

namespace CalcioAnalytic.Api.Tests;

public sealed class ApiKeyCredentialServiceTests
{
    [Fact]
    public void Create_returns_secret_but_record_contains_only_hash()
    {
        var service = new ApiKeyCredentialService();

        var (record, secret) = service.Create(new[] { "ingestion:write", "analytics:read" });

        Assert.False(string.IsNullOrWhiteSpace(secret));
        Assert.NotEqual(secret, record.KeyHash);
        Assert.Equal(2, record.Scopes.Length);
        Assert.False(record.IsRevoked);
        Assert.True(service.Matches(record, secret));
    }

    [Fact]
    public void Different_secrets_do_not_match()
    {
        var service = new ApiKeyCredentialService();
        var (record, secret) = service.Create();

        Assert.False(service.Matches(record, secret + "x"));
    }

    [Fact]
    public void Revoked_key_is_rejected()
    {
        var service = new ApiKeyCredentialService();
        var (record, secret) = service.Create();
        record.IsRevoked = true;
        record.RevokedAtUtc = DateTime.UtcNow;

        Assert.False(service.Matches(record, secret));
    }
}
