namespace CalcioAnalytic.Ingestion.Scraping;

public sealed class ScraperOptions
{
    public const string SectionName = "Scraping";

    public bool Enabled { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MinimumDelayMilliseconds { get; set; } = 1500;
    public int MaxRetries { get; set; } = 3;
    public string UserAgent { get; set; } = "CalcioAnalytic/1.0 (+data-ingestion)";
}
