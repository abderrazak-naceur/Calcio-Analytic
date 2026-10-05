using System.Globalization;
using System.Text.Json;
using CalcioAnalytic.Contracts.Providers;
using Microsoft.Extensions.Configuration;

namespace CalcioAnalytic.Ingestion.Providers.Sportmonks;

/// <summary>
/// Sportmonks Football API v3 adapter for subscribed leagues, season schedules
/// and standard pre-match odds.
/// </summary>
public sealed class SportmonksProvider :
    IFootballProvider,
    IFixtureProvider,
    IOddsProvider
{
    public const string Code = "sportmonks";
    private const int PageSize = 50;
    private const int MaximumPages = 10_000;
    private readonly HttpClient _httpClient;
    private readonly string? _apiToken;

    public SportmonksProvider(HttpClient httpClient, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(configuration);

        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://api.sportmonks.com/v3/");
        _apiToken = configuration["Sportmonks:ApiToken"];
    }

    public string ProviderCode => Code;

    public async Task<IReadOnlyList<ProviderCompetitionDto>> GetCompetitionsAsync(
        CancellationToken ct = default)
    {
        using var document = await GetAllPagesAsync("football/leagues?include=country", ct)
            .ConfigureAwait(false);

        return GetDataItems(document.RootElement)
            .Select(league => new ProviderCompetitionDto(
                GetRequiredString(league, "id"),
                GetString(league, "name") ?? GetRequiredString(league, "id"),
                GetRelatedName(league, "country"),
                GetString(league, "type")))
            .ToArray();
    }

    public async Task<IReadOnlyList<ProviderSeasonDto>> GetSeasonsAsync(
        string competitionExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitionExternalId);

        var endpoint = $"football/seasons?include=league&filters=seasonLeagues:{Uri.EscapeDataString(competitionExternalId)}";
        using var document = await GetAllPagesAsync(endpoint, ct).ConfigureAwait(false);

        return GetDataItems(document.RootElement)
            .Select(season => new ProviderSeasonDto(
                GetRequiredString(season, "id"),
                GetString(season, "league_id") ?? competitionExternalId,
                GetString(season, "name") ?? GetRequiredString(season, "id"),
                ParseDateOnly(GetString(season, "starting_at")),
                ParseDateOnly(GetString(season, "ending_at"))))
            .ToArray();
    }

    public async Task<IReadOnlyList<ProviderTeamDto>> GetTeamsAsync(
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitionExternalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seasonExternalId);

        using var document = await GetAsync(
            $"football/schedules/seasons/{Uri.EscapeDataString(seasonExternalId)}?include=participants",
            ct).ConfigureAwait(false);

        var teams = new Dictionary<string, ProviderTeamDto>(StringComparer.Ordinal);
        CollectTeams(document.RootElement, teams);
        return teams.Values.ToArray();
    }

    public async Task<IReadOnlyList<ProviderMatchDto>> GetFixturesAsync(
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitionExternalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seasonExternalId);

        using var document = await GetAsync(
            $"football/schedules/seasons/{Uri.EscapeDataString(seasonExternalId)}",
            ct).ConfigureAwait(false);

        var fixtures = new Dictionary<string, ProviderMatchDto>(StringComparer.Ordinal);
        CollectFixtures(document.RootElement, competitionExternalId, seasonExternalId, fixtures);
        return fixtures.Values.OrderBy(match => match.KickoffUtc).ToArray();
    }

    public async Task<ProviderMatchDto?> GetFixtureAsync(
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);

        using var document = await GetAsync(
            $"football/fixtures/{Uri.EscapeDataString(matchExternalId)}?include=participants;scores;state;venue;round",
            ct).ConfigureAwait(false);

        var fixture = GetSingleDataItem(document.RootElement);
        return fixture.ValueKind == JsonValueKind.Undefined
            ? null
            : ParseFixture(fixture, GetString(fixture, "league_id") ?? string.Empty);
    }

    public async Task<IReadOnlyList<ProviderOddsSnapshotDto>> GetOddsAsync(
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);

        using var document = await GetAllPagesAsync(
            $"football/odds/pre-match/fixtures/{Uri.EscapeDataString(matchExternalId)}?include=bookmaker;market",
            ct).ConfigureAwait(false);

        var odds = GetDataItems(document.RootElement)
            .Select(ParseOdd)
            .Where(odd => odd is not null)
            .Select(odd => odd!)
            .ToArray();

        return odds
            .GroupBy(odd => new { odd.BookmakerId, odd.BookmakerTimestamp })
            .Select(group => new ProviderOddsSnapshotDto(
                group.Key.BookmakerId,
                matchExternalId,
                false,
                null,
                group.Key.BookmakerTimestamp,
                group.Max(odd => odd.ProviderTimestamp),
                group.GroupBy(odd => new { odd.MarketId, odd.Line, odd.Period })
                    .Select(marketGroup => new ProviderMarketLineDto(
                        marketGroup.Key.MarketId,
                        marketGroup.Key.Line,
                        marketGroup.Key.Period,
                        marketGroup.Select(odd => new ProviderSelectionDto(
                            NormalizeSelection(odd.Label),
                            odd.DecimalOdds,
                            IsSuspended: odd.IsStopped))
                            .ToArray()))
                    .ToArray()))
            .OrderBy(snapshot => snapshot.BookmakerTimestamp)
            .ThenBy(snapshot => snapshot.BookmakerExternalId, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<IReadOnlyList<ProviderBookmakerDto>> GetBookmakersAsync(
        CancellationToken ct = default)
    {
        using var document = await GetAllPagesAsync("odds/bookmakers", ct).ConfigureAwait(false);
        return GetDataItems(document.RootElement)
            .Select(bookmaker => new ProviderBookmakerDto(
                GetRequiredString(bookmaker, "id"),
                GetString(bookmaker, "name") ?? GetRequiredString(bookmaker, "id")))
            .ToArray();
    }

    public async Task<IReadOnlyList<ProviderMarketDto>> GetMarketsAsync(
        CancellationToken ct = default)
    {
        using var document = await GetAllPagesAsync("odds/markets", ct).ConfigureAwait(false);
        return GetDataItems(document.RootElement)
            .Select(market =>
            {
                var id = GetRequiredString(market, "id");
                var code = GetString(market, "developer_name");
                var name = GetString(market, "name") ?? id;
                return new ProviderMarketDto(
                    id,
                    IsFullTimeResultMarket(code, name) ? "1X2" : name,
                    code,
                    GetString(market, "description"));
            })
            .ToArray();
    }

    private async Task<JsonDocument> GetAllPagesAsync(string endpoint, CancellationToken ct)
    {
        var items = new List<JsonElement>();
        var pageEndpoint = AddQueryParameter(endpoint, "per_page", PageSize.ToString(CultureInfo.InvariantCulture));

        for (var page = 0; page < MaximumPages; page++)
        {
            using var document = await GetAsync(pageEndpoint, ct).ConfigureAwait(false);
            items.AddRange(GetDataItems(document.RootElement).Select(item => item.Clone()));

            if (!TryGetProperty(document.RootElement, "pagination", out var pagination) ||
                !TryGetProperty(pagination, "has_more", out var hasMore) ||
                hasMore.ValueKind != JsonValueKind.True)
            {
                return CreateDataDocument(items);
            }

            pageEndpoint = GetNextPageEndpoint(endpoint, pagination);
            if (string.IsNullOrWhiteSpace(pageEndpoint))
            {
                throw new InvalidOperationException(
                    "Sportmonks reported more pages but did not provide a pagination cursor or next page URL.");
            }
        }

        throw new InvalidOperationException(
            $"Sportmonks pagination exceeded the safety limit of {MaximumPages} pages.");
    }

    private static string? GetNextPageEndpoint(string originalEndpoint, JsonElement pagination)
    {
        var nextPage = GetString(pagination, "next_page");
        if (!string.IsNullOrWhiteSpace(nextPage))
        {
            return ToRelativeEndpoint(nextPage);
        }

        var cursor = GetString(pagination, "next_cursor");
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        if (Uri.TryCreate(cursor, UriKind.Absolute, out var cursorUri))
        {
            return ToRelativeEndpoint(cursorUri);
        }

        return AddQueryParameter(originalEndpoint, "cursor", cursor);
    }

    private static string ToRelativeEndpoint(string endpointOrUrl) =>
        Uri.TryCreate(endpointOrUrl, UriKind.Absolute, out var uri)
            ? ToRelativeEndpoint(uri)
            : endpointOrUrl.TrimStart('/');

    private static string ToRelativeEndpoint(Uri uri) =>
        string.Concat(RemoveApiVersionPrefix(uri.AbsolutePath.TrimStart('/')), uri.Query);

    private static string RemoveApiVersionPrefix(string path)
    {
        const string VersionPrefix = "v3/";
        return path.StartsWith(VersionPrefix, StringComparison.OrdinalIgnoreCase)
            ? path[VersionPrefix.Length..]
            : path;
    }

    private static string AddQueryParameter(string endpoint, string name, string value)
    {
        var separator = endpoint.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{endpoint}{separator}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
    }

    private async Task<JsonDocument> GetAsync(string endpoint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            throw new InvalidOperationException(
                "Sportmonks API token is not configured. Set Sportmonks:ApiToken in .NET User Secrets.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.TryAddWithoutValidation("Authorization", _apiToken);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            ct).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var detail = string.IsNullOrWhiteSpace(payload)
                ? response.ReasonPhrase
                : payload.Length <= 500 ? payload : payload[..500];
            throw new HttpRequestException(
                $"Sportmonks API request '{endpoint}' failed with HTTP {(int)response.StatusCode}: {detail}",
                null,
                response.StatusCode);
        }

        try
        {
            return JsonDocument.Parse(payload);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Sportmonks returned invalid JSON for endpoint '{endpoint}'.",
                exception);
        }
    }

    private static JsonDocument CreateDataDocument(IReadOnlyList<JsonElement> items)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("data");
            writer.WriteStartArray();
            foreach (var item in items)
            {
                item.WriteTo(writer);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return JsonDocument.Parse(stream.ToArray());
    }

    private static IEnumerable<JsonElement> GetDataItems(JsonElement root)
    {
        if (TryGetProperty(root, "data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            return data.EnumerateArray().ToArray();
        }

        return [];
    }

    private static JsonElement GetSingleDataItem(JsonElement root)
    {
        if (!TryGetProperty(root, "data", out var data))
        {
            return default;
        }

        return data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray().FirstOrDefault()
            : data;
    }

    private static void CollectFixtures(
        JsonElement node,
        string requestedCompetitionId,
        string requestedSeasonId,
        IDictionary<string, ProviderMatchDto> fixtures)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray())
            {
                CollectFixtures(child, requestedCompetitionId, requestedSeasonId, fixtures);
            }
            return;
        }

        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (HasKickoff(node) &&
            TryGetProperty(node, "participants", out _))
        {
            var match = ParseFixture(node, requestedCompetitionId, requestedSeasonId);
            if (string.Equals(
                match.CompetitionExternalId,
                requestedCompetitionId,
                StringComparison.Ordinal))
            {
                fixtures[match.ExternalId] = match;
            }
            return;
        }

        foreach (var propertyName in new[] { "data", "stages", "rounds", "fixtures" })
        {
            if (TryGetProperty(node, propertyName, out var child))
            {
                CollectFixtures(child, requestedCompetitionId, requestedSeasonId, fixtures);
            }
        }
    }

    private static void CollectTeams(JsonElement node, IDictionary<string, ProviderTeamDto> teams)
    {
        if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray())
            {
                CollectTeams(child, teams);
            }
            return;
        }

        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (HasKickoff(node) &&
            TryGetProperty(node, "participants", out var participants) &&
            participants.ValueKind == JsonValueKind.Array)
        {
            foreach (var participant in participants.EnumerateArray())
            {
                var id = GetString(participant, "id");
                var name = GetString(participant, "name");
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                {
                    teams[id] = new ProviderTeamDto(
                        id,
                        name,
                        GetString(participant, "short_code"));
                }
            }
            return;
        }

        foreach (var propertyName in new[] { "data", "stages", "rounds", "fixtures" })
        {
            if (TryGetProperty(node, propertyName, out var child))
            {
                CollectTeams(child, teams);
            }
        }
    }

    private static ProviderMatchDto ParseFixture(
        JsonElement fixture,
        string fallbackCompetitionId,
        string? fallbackSeasonId = null)
    {
        var participants = TryGetProperty(fixture, "participants", out var participantsNode) &&
            participantsNode.ValueKind == JsonValueKind.Array
            ? participantsNode.EnumerateArray().ToArray()
            : [];
        var home = participants.FirstOrDefault(item =>
            string.Equals(GetRelatedString(item, "meta", "location"), "home", StringComparison.OrdinalIgnoreCase));
        var away = participants.FirstOrDefault(item =>
            string.Equals(GetRelatedString(item, "meta", "location"), "away", StringComparison.OrdinalIgnoreCase));

        var homeId = GetString(home, "id");
        var awayId = GetString(away, "id");
        if (string.IsNullOrWhiteSpace(homeId) || string.IsNullOrWhiteSpace(awayId))
        {
            throw new InvalidOperationException(
                $"Sportmonks fixture {GetString(fixture, "id") ?? "(unknown)"} is missing its home or away participant.");
        }

        var scores = TryGetProperty(fixture, "scores", out var scoresNode) &&
            scoresNode.ValueKind == JsonValueKind.Array
            ? scoresNode.EnumerateArray().ToArray()
            : [];
        var homeScore = GetFinalScore(scores, homeId);
        var awayScore = GetFinalScore(scores, awayId);
        var homeHalfTime = GetScore(scores, homeId, "1ST_HALF");
        var awayHalfTime = GetScore(scores, awayId, "1ST_HALF");

        var kickoff = TryGetProperty(fixture, "starting_at_timestamp", out var timestampNode) &&
            timestampNode.TryGetInt64(out var timestamp)
            ? DateTimeOffset.FromUnixTimeSeconds(timestamp)
            : ParseTimestamp(GetString(fixture, "starting_at"));
        if (kickoff is null)
        {
            throw new InvalidOperationException(
                $"Sportmonks fixture {GetString(fixture, "id") ?? "(unknown)"} is missing its kickoff time.");
        }

        return new ProviderMatchDto(
            GetRequiredString(fixture, "id"),
            GetString(fixture, "league_id") ?? fallbackCompetitionId,
            GetString(fixture, "season_id") ?? fallbackSeasonId,
            homeId,
            awayId,
            kickoff.Value,
            GetRelatedName(fixture, "venue"),
            GetRelatedName(fixture, "round"),
            null,
            GetRelatedName(fixture, "state") ??
                (GetString(fixture, "result_info") is not null && homeScore is not null && awayScore is not null
                    ? "finished"
                    : "scheduled"),
            homeScore,
            awayScore,
            homeHalfTime,
            awayHalfTime);
    }

    private static int? GetScore(IEnumerable<JsonElement> scores, string participantId, string description)
    {
        foreach (var score in scores)
        {
            var scoreParticipantId = GetString(score, "participant_id");
            var scoreDescription = GetString(score, "description");
            if (!string.Equals(scoreParticipantId, participantId, StringComparison.Ordinal) ||
                !string.Equals(scoreDescription, description, StringComparison.OrdinalIgnoreCase) ||
                !TryGetProperty(score, "score", out var scoreValue) ||
                !TryGetProperty(scoreValue, "goals", out var goals))
            {
                continue;
            }

            return goals.TryGetInt32(out var value) ? value : null;
        }

        return null;
    }

    private static int? GetFinalScore(IEnumerable<JsonElement> scores, string participantId) =>
        GetScore(scores, participantId, "CURRENT") ??
        GetScore(scores, participantId, "2ND_HALF") ??
        GetScore(scores, participantId, "EXTRA_TIME") ??
        GetScore(scores, participantId, "PENALTY_SHOOTOUT");

    private static bool HasKickoff(JsonElement fixture) =>
        TryGetProperty(fixture, "starting_at", out _) ||
        TryGetProperty(fixture, "starting_at_timestamp", out _);

    private static ParsedOdd? ParseOdd(JsonElement odd)
    {
        var bookmakerId = GetString(odd, "bookmaker_id");
        var marketId = GetString(odd, "market_id");
        var label = GetString(odd, "label") ?? GetString(odd, "name");
        var oddsText = GetString(odd, "value");
        if (string.IsNullOrWhiteSpace(bookmakerId) ||
            string.IsNullOrWhiteSpace(marketId) ||
            string.IsNullOrWhiteSpace(label) ||
            !decimal.TryParse(oddsText, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalOdds) ||
            decimalOdds <= 1m)
        {
            return null;
        }

        var timestamp = ParseTimestamp(
            GetString(odd, "latest_bookmaker_update") ??
            GetString(odd, "updated_at") ??
            GetString(odd, "created_at"));
        if (timestamp is null)
        {
            return null;
        }

        var providerTimestamp = ParseTimestamp(GetString(odd, "updated_at")) ?? timestamp.Value;
        var marketCode = GetRelatedString(odd, "market", "developer_name");
        var marketName = GetRelatedString(odd, "market", "name") ??
            GetString(odd, "market_description");
        var period = IsFirstHalfMarket(marketCode, marketName)
            ? "FirstHalf"
            : IsFullTimeResultMarket(marketCode, marketName)
                ? "FullTime"
                : null;
        var line = ParseDecimal(GetString(odd, "total")) ?? ParseDecimal(GetString(odd, "handicap"));

        return new ParsedOdd(
            bookmakerId,
            marketId,
            label,
            decimalOdds,
            timestamp.Value,
            providerTimestamp,
            line,
            period,
            GetBoolean(odd, "stopped"));
    }

    private static string NormalizeSelection(string value)
    {
        var normalized = value.Trim();
        return normalized.ToLowerInvariant() switch
        {
            "home" or "1" => "Home",
            "draw" or "x" => "Draw",
            "away" or "2" => "Away",
            _ => normalized,
        };
    }

    private static bool IsFullTimeResultMarket(string? code, string? name) =>
        string.Equals(code, "FULLTIME_RESULT", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Fulltime Result", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "Full Time Result", StringComparison.OrdinalIgnoreCase);

    private static bool IsFirstHalfMarket(string? code, string? name) =>
        (code?.Contains("HALF_TIME", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (name?.Contains("half time", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (name?.Contains("1st half", StringComparison.OrdinalIgnoreCase) ?? false);

    private static DateOnly? ParseDateOnly(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    private static DateTimeOffset? ParseTimestamp(string? value) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : null;

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static bool GetBoolean(JsonElement element, string propertyName) =>
        TryGetProperty(element, propertyName, out var property) &&
        property.ValueKind is JsonValueKind.True;

    private static string GetRequiredString(JsonElement element, string propertyName) =>
        GetString(element, propertyName) ??
        throw new InvalidOperationException(
            $"Sportmonks response is missing required field '{propertyName}'.");

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null,
        };
    }

    private static string? GetRelatedName(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(property, "name");
    }

    private static string? GetRelatedString(JsonElement element, string propertyName, string relatedPropertyName)
    {
        if (!TryGetProperty(element, propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetString(property, relatedPropertyName);
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var item in element.EnumerateObject())
            {
                if (string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = item.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private sealed record ParsedOdd(
        string BookmakerId,
        string MarketId,
        string Label,
        decimal DecimalOdds,
        DateTimeOffset BookmakerTimestamp,
        DateTimeOffset ProviderTimestamp,
        decimal? Line,
        string? Period,
        bool IsStopped);
}
