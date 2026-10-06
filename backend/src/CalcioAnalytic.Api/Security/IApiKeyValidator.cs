namespace CalcioAnalytic.Api.Security;

public interface IApiKeyValidator
{
    ValueTask<ApiKeyRecord?> ValidateAsync(string presentedSecret, CancellationToken cancellationToken = default);
}

public sealed class ApiKeyValidator : IApiKeyValidator
{
    private readonly ApiKeyCredentialService _credentials;
    private readonly ApiKeyOptions _options;

    public ApiKeyValidator(
        ApiKeyCredentialService credentials,
        Microsoft.Extensions.Options.IOptions<ApiKeyOptions> options)
    {
        _credentials = credentials;
        _options = options.Value;
    }

    public ValueTask<ApiKeyRecord?> ValidateAsync(
        string presentedSecret,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(presentedSecret))
            return ValueTask.FromResult<ApiKeyRecord?>(null);

        // Legacy configuration remains supported for existing local installs.
        // Persisted customer keys must use the hashed credential service.
        foreach (var configuredKey in _options.Keys ?? Array.Empty<string>())
        {
            if (string.Equals(configuredKey, presentedSecret, StringComparison.Ordinal))
            {
                var legacy = new ApiKeyRecord
                {
                    KeyPrefix = presentedSecret.Length >= 12 ? presentedSecret[..12] : presentedSecret,
                    KeyHash = Convert.ToHexString(
                        System.Security.Cryptography.SHA256.HashData(
                            System.Text.Encoding.UTF8.GetBytes(presentedSecret))).ToLowerInvariant(),
                    Scopes = new[] { "ingestion:write" }
                };
                return ValueTask.FromResult<ApiKeyRecord?>(legacy);
            }
        }

        return ValueTask.FromResult<ApiKeyRecord?>(null);
    }
}
