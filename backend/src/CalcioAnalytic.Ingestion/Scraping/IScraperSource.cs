namespace CalcioAnalytic.Ingestion.Scraping;

public interface IScraperSource
{
    string SourceCode { get; }

    Task<ScrapedPage> FetchAsync(
        Uri uri,
        CancellationToken cancellationToken = default);
}

public sealed record ScrapedPage(
    string SourceCode,
    Uri Uri,
    int StatusCode,
    string Content,
    DateTime RetrievedAtUtc,
    string? ETag = null,
    string? LastModified = null);
