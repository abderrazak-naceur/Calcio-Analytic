using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CalcioAnalytic.Contracts.Providers;

namespace CalcioAnalytic.Ingestion.Providers.Mock;

/// <summary>
/// Loads and caches the deterministic mock sample data used by
/// <see cref="MockFileProvider"/>. Sample data is read from JSON files embedded
/// in this assembly (under <c>SampleData/</c>) by default, or from a configurable
/// folder on disk when one is supplied.
/// </summary>
/// <remarks>
/// The loader is deterministic: the same source files always produce the same
/// DTOs, and every odds snapshot is assigned a stable <see cref="ProviderOddsSnapshotDto.RawPayloadHash"/>
/// computed as the SHA-256 of a canonical string. This lets downstream ingestion
/// deduplicate snapshots reproducibly.
/// </remarks>
public sealed class MockProviderData
{
    private const string EmbeddedResourcePrefix = "CalcioAnalytic.Ingestion.SampleData.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly Lazy<MockProviderData> EmbeddedInstance =
        new(() => new MockProviderData(LoadFromEmbeddedResources()));

    private MockProviderData(RawSampleData raw)
    {
        Competitions = raw.Competitions;
        Seasons = raw.Seasons;
        Teams = raw.Teams;
        Matches = raw.Matches;
        Bookmakers = raw.Bookmakers;
        Markets = raw.Markets;
        Statistics = raw.Statistics;
        Events = raw.Events;

        // Assign a stable, reproducible provenance hash to every snapshot.
        OddsSnapshots = raw.Odds
            .Select(WithStableHash)
            .ToArray();
    }

    /// <summary>Gets the shared instance backed by the embedded sample data.</summary>
    public static MockProviderData Embedded => EmbeddedInstance.Value;

    /// <summary>Gets the sample competitions.</summary>
    public IReadOnlyList<ProviderCompetitionDto> Competitions { get; }

    /// <summary>Gets the sample seasons.</summary>
    public IReadOnlyList<ProviderSeasonDto> Seasons { get; }

    /// <summary>Gets the sample teams.</summary>
    public IReadOnlyList<ProviderTeamDto> Teams { get; }

    /// <summary>Gets the sample matches.</summary>
    public IReadOnlyList<ProviderMatchDto> Matches { get; }

    /// <summary>Gets the sample bookmakers.</summary>
    public IReadOnlyList<ProviderBookmakerDto> Bookmakers { get; }

    /// <summary>Gets the sample markets.</summary>
    public IReadOnlyList<ProviderMarketDto> Markets { get; }

    /// <summary>Gets the sample per-team statistics.</summary>
    public IReadOnlyList<ProviderStatisticDto> Statistics { get; }

    /// <summary>Gets the sample in-match events.</summary>
    public IReadOnlyList<ProviderEventDto> Events { get; }

    /// <summary>
    /// Gets the sample odds snapshots, each carrying a stable
    /// <see cref="ProviderOddsSnapshotDto.RawPayloadHash"/>.
    /// </summary>
    public IReadOnlyList<ProviderOddsSnapshotDto> OddsSnapshots { get; }

    /// <summary>
    /// Loads sample data from JSON files in the given folder. File names must match
    /// the embedded resource names (competitions.json, seasons.json, teams.json,
    /// matches.json, bookmakers.json, markets.json, statistics.json, events.json, odds.json).
    /// </summary>
    /// <param name="folderPath">The absolute or relative folder containing the JSON files.</param>
    public static MockProviderData FromFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        return new MockProviderData(LoadFromFolder(folderPath));
    }

    /// <summary>
    /// Computes the canonical SHA-256 hash for an odds snapshot. The canonical
    /// string is independent of JSON formatting so identical odds always hash to
    /// the same value regardless of source layout.
    /// </summary>
    public static string ComputeSnapshotHash(ProviderOddsSnapshotDto snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var sb = new StringBuilder();
        sb.Append(snapshot.BookmakerExternalId).Append('|')
          .Append(snapshot.MatchExternalId).Append('|')
          .Append(snapshot.IsLive ? '1' : '0').Append('|')
          .Append(snapshot.MatchMinute?.ToString(CultureInfo.InvariantCulture) ?? "-").Append('|')
          .Append(snapshot.BookmakerTimestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)).Append('|')
          .Append(snapshot.ProviderTimestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        foreach (var market in snapshot.Markets.OrderBy(m => m.MarketExternalId, StringComparer.Ordinal)
                     .ThenBy(m => m.Line ?? decimal.MinValue)
                     .ThenBy(m => m.Period, StringComparer.Ordinal))
        {
            sb.Append("||M:").Append(market.MarketExternalId)
              .Append(';').Append(market.Line?.ToString(CultureInfo.InvariantCulture) ?? "-")
              .Append(';').Append(market.Period ?? "-");

            foreach (var sel in market.Selections.OrderBy(s => s.Name, StringComparer.Ordinal))
            {
                sb.Append(";S:").Append(sel.Name)
                  .Append('=').Append(sel.DecimalOdds.ToString(CultureInfo.InvariantCulture))
                  .Append('/').Append(sel.IsSuspended ? '1' : '0');
            }
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexStringLower(bytes);
    }

    private static ProviderOddsSnapshotDto WithStableHash(ProviderOddsSnapshotDto snapshot) =>
        snapshot with { RawPayloadHash = ComputeSnapshotHash(snapshot) };

    private static RawSampleData LoadFromEmbeddedResources()
    {
        var assembly = typeof(MockProviderData).Assembly;

        return new RawSampleData
        {
            Competitions = DeserializeEmbedded<ProviderCompetitionDto>(assembly, "competitions.json"),
            Seasons = DeserializeEmbedded<ProviderSeasonDto>(assembly, "seasons.json"),
            Teams = DeserializeEmbedded<ProviderTeamDto>(assembly, "teams.json"),
            Matches = DeserializeEmbedded<ProviderMatchDto>(assembly, "matches.json"),
            Bookmakers = DeserializeEmbedded<ProviderBookmakerDto>(assembly, "bookmakers.json"),
            Markets = DeserializeEmbedded<ProviderMarketDto>(assembly, "markets.json"),
            Statistics = DeserializeEmbedded<ProviderStatisticDto>(assembly, "statistics.json"),
            Events = DeserializeEmbedded<ProviderEventDto>(assembly, "events.json"),
            Odds = DeserializeEmbedded<ProviderOddsSnapshotDto>(assembly, "odds.json"),
        };
    }

    private static RawSampleData LoadFromFolder(string folderPath) =>
        new()
        {
            Competitions = DeserializeFile<ProviderCompetitionDto>(folderPath, "competitions.json"),
            Seasons = DeserializeFile<ProviderSeasonDto>(folderPath, "seasons.json"),
            Teams = DeserializeFile<ProviderTeamDto>(folderPath, "teams.json"),
            Matches = DeserializeFile<ProviderMatchDto>(folderPath, "matches.json"),
            Bookmakers = DeserializeFile<ProviderBookmakerDto>(folderPath, "bookmakers.json"),
            Markets = DeserializeFile<ProviderMarketDto>(folderPath, "markets.json"),
            Statistics = DeserializeFile<ProviderStatisticDto>(folderPath, "statistics.json"),
            Events = DeserializeFile<ProviderEventDto>(folderPath, "events.json"),
            Odds = DeserializeFile<ProviderOddsSnapshotDto>(folderPath, "odds.json"),
        };

    private static IReadOnlyList<T> DeserializeEmbedded<T>(Assembly assembly, string fileName)
    {
        var resourceName = EmbeddedResourcePrefix + fileName;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded mock sample resource '{resourceName}' was not found in assembly '{assembly.GetName().Name}'.");

        var result = JsonSerializer.Deserialize<List<T>>(stream, SerializerOptions);
        return result ?? throw new InvalidOperationException(
            $"Embedded mock sample resource '{resourceName}' deserialized to null.");
    }

    private static IReadOnlyList<T> DeserializeFile<T>(string folderPath, string fileName)
    {
        var path = Path.Combine(folderPath, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Mock sample file '{fileName}' was not found in folder '{folderPath}'.", path);
        }

        using var stream = File.OpenRead(path);
        var result = JsonSerializer.Deserialize<List<T>>(stream, SerializerOptions);
        return result ?? throw new InvalidOperationException(
            $"Mock sample file '{path}' deserialized to null.");
    }

    private sealed class RawSampleData
    {
        public IReadOnlyList<ProviderCompetitionDto> Competitions { get; init; } = [];
        public IReadOnlyList<ProviderSeasonDto> Seasons { get; init; } = [];
        public IReadOnlyList<ProviderTeamDto> Teams { get; init; } = [];
        public IReadOnlyList<ProviderMatchDto> Matches { get; init; } = [];
        public IReadOnlyList<ProviderBookmakerDto> Bookmakers { get; init; } = [];
        public IReadOnlyList<ProviderMarketDto> Markets { get; init; } = [];
        public IReadOnlyList<ProviderStatisticDto> Statistics { get; init; } = [];
        public IReadOnlyList<ProviderEventDto> Events { get; init; } = [];
        public IReadOnlyList<ProviderOddsSnapshotDto> Odds { get; init; } = [];
    }
}
