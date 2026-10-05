namespace CalcioAnalytic.Ingestion.Scraping;

public sealed class FootballDataCsvOptions
{
    public const string SectionName = "FootballDataCsv";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://www.football-data.co.uk/";
    public int TimeoutSeconds { get; set; } = 60;
}
