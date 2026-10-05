using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CalcioAnalytic.Ingestion.Scraping;

public sealed class HttpScraperSource : IScraperSource
{
    private readonly HttpClient _httpClient;
    private readonly ScraperOptions _options;
    private readonly ILogger<HttpScraperSource> _logger;
    private readonly SemaphoreSlim _throttle = new(1, 1);
    private DateTime _lastRequestUtc = DateTime.MinValue;

    public HttpScraperSource(
        HttpClient httpClient,
        IOptions<ScraperOptions> options,
        ILogger<HttpScraperSource> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string SourceCode => "http";

    public async Task<ScrapedPage> FetchAsync(
        Uri uri,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        await _throttle.WaitAsync(cancellationToken);
        try
        {
            await RespectMinimumDelayAsync(cancellationToken);

            for (var attempt = 1; attempt <= Math.Max(1, _options.MaxRetries); attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.UserAgent.ParseAdd(_options.UserAgent);
                    request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/json;q=0.9,*/*;q=0.8");

                    using var response = await _httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    _lastRequestUtc = DateTime.UtcNow;

                    if (!response.IsSuccessStatusCode)
                    {
                        if (attempt == _options.MaxRetries || !IsTransient(response.StatusCode))
                        {
                            throw new HttpRequestException(
                                $"Scraper request failed with HTTP {(int)response.StatusCode} ({response.StatusCode}) for {uri}.");
                        }

                        await BackoffAsync(attempt, cancellationToken);
                        continue;
                    }

                    return new ScrapedPage(
                        SourceCode,
                        uri,
                        (int)response.StatusCode,
                        content,
                        _lastRequestUtc,
                        response.Headers.ETag?.Tag,
                        response.Content.Headers.LastModified?.ToString());
                }
                catch (HttpRequestException) when (attempt < _options.MaxRetries)
                {
                    _logger.LogWarning(
                        "Scraper request attempt {Attempt}/{MaxRetries} failed for {Uri}.",
                        attempt,
                        _options.MaxRetries,
                        uri);

                    await BackoffAsync(attempt, cancellationToken);
                }
            }

            throw new InvalidOperationException("Scraper retry loop ended unexpectedly.");
        }
        finally
        {
            _throttle.Release();
        }
    }

    private async Task RespectMinimumDelayAsync(CancellationToken cancellationToken)
    {
        var elapsed = DateTime.UtcNow - _lastRequestUtc;
        var delay = TimeSpan.FromMilliseconds(Math.Max(0, _options.MinimumDelayMilliseconds)) - elapsed;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static Task BackoffAsync(int attempt, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(Math.Min(10_000, 500 * Math.Pow(2, attempt - 1))), cancellationToken);
}
