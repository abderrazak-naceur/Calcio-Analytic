using System.Security.Cryptography;
using System.Text;

namespace CalcioAnalytic.Api.Security;

/// <summary>
/// Generates and validates API-key secrets without retaining plaintext secrets.
/// </summary>
public sealed class ApiKeyCredentialService
{
    private const int SecretBytes = 32;

    public (ApiKeyRecord Record, string PlaintextKey) Create(
        IEnumerable<string>? scopes = null)
    {
        var secretBytes = RandomNumberGenerator.GetBytes(SecretBytes);
        var secret = Convert.ToHexString(secretBytes).ToLowerInvariant();
        var prefix = secret[..12];

        var record = new ApiKeyRecord
        {
            KeyPrefix = prefix,
            KeyHash = Hash(secret),
            Scopes = scopes?.Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>()
        };

        return (record, secret);
    }

    public bool Matches(ApiKeyRecord record, string presentedSecret)
    {
        if (record.IsRevoked || string.IsNullOrWhiteSpace(presentedSecret))
            return false;

        var expected = Convert.FromHexString(record.KeyHash);
        var actual = Convert.FromHexString(Hash(presentedSecret));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static string Hash(string secret)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
    }
}
