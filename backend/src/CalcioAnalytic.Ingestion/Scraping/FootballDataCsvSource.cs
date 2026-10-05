using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CalcioAnalytic.Ingestion.Scraping;

public sealed class FootballDataCsvSource
{
    private readonly HttpClient _httpClient;
    private readonly FootballDataCsvOptions _options;
    private readonly ILogger<FootballDataCsvSource> _logger;

    public FootballDataCsvSource(HttpClient httpClient, IOptions<FootballDataCsvOptions> options, ILogger<FootballDataCsvSource> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FootballDataCsvSnapshot> DownloadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("CSV path is required.", nameof(relativePath));

        var baseUri = new Uri(_options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var uri = new Uri(baseUri, relativePath.TrimStart('/'));
        using var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var content = Encoding.UTF8.GetString(bytes);
        var retrievedAt = DateTime.UtcNow;

        _logger.LogInformation("Downloaded Football-Data CSV {Path}: {Bytes} bytes at {RetrievedAtUtc}.", relativePath, bytes.Length, retrievedAt);
        return new FootballDataCsvSnapshot(uri, relativePath, content, retrievedAt);
    }

    public static IReadOnlyList<IReadOnlyDictionary<string, string>> ParseRows(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return [];

        using var reader = new StringReader(csv);
        var headerLine = reader.ReadLine();
        if (headerLine is null)
            return [];

        var headers = SplitCsvLine(headerLine);
        var rows = new List<IReadOnlyDictionary<string, string>>();
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = SplitCsvLine(line);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Count; i++)
                row[headers[i]] = i < values.Count ? values[i] : string.Empty;

            rows.Add(row);
        }

        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var value = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
                continue;
            }

            if (ch == ',' && !quoted)
            {
                result.Add(value.ToString().Trim());
                value.Clear();
                continue;
            }

            value.Append(ch);
        }

        result.Add(value.ToString().Trim());
        return result;
    }
}

public sealed record FootballDataCsvSnapshot(Uri Uri, string RelativePath, string Content, DateTime RetrievedAtUtc);
