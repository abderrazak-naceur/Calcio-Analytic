using System.Net;
using CalcioAnalytic.Ingestion.Providers.Sportmonks;
using Microsoft.Extensions.Configuration;

namespace CalcioAnalytic.Api.Tests;

public sealed class SportmonksProviderTests
{
    [Fact]
    public async Task GetCompetitionsAsync_UsesAuthorizationHeaderAndMapsLeague()
    {
        using var client = CreateClient("""
            {
              "data": [
                {
                  "id": 384,
                  "name": "Serie A",
                  "type": "league",
                  "country": { "name": "Italy" }
                }
              ]
            }
            """, out var handler);
        var provider = CreateProvider(client);

        var leagues = await provider.GetCompetitionsAsync();

        var league = Assert.Single(leagues);
        Assert.Equal("384", league.ExternalId);
        Assert.Equal("Serie A", league.Name);
        Assert.Equal("Italy", league.CountryName);
        Assert.Equal("test-token", handler.AuthorizationToken);
        Assert.Contains("football/leagues", handler.RequestUri);
    }

    [Fact]
    public async Task GetCompetitionsAsync_FollowsSportmonksCursorPagination()
    {
        using var client = CreateClient(out var handler,
            """
            {
              "data": [{ "id": 1, "name": "League One" }],
              "pagination": {
                "has_more": true,
                "next_cursor": "https://api.sportmonks.com/v3/football/leagues?include=country&cursor=next-page-token"
              }
            }
            """,
            """
            {
              "data": [{ "id": 2, "name": "League Two" }],
              "pagination": {
                "has_more": false,
                "next_cursor": null
              }
            }
            """);
        var provider = CreateProvider(client);

        var leagues = await provider.GetCompetitionsAsync();

        Assert.Equal(new[] { "1", "2" }, leagues.Select(league => league.ExternalId));
        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Contains("per_page=50", handler.RequestUris[0]);
        Assert.Contains("cursor=next-page-token", handler.RequestUris[1]);
        Assert.DoesNotContain("cursor=https", handler.RequestUris[1]);
        Assert.DoesNotContain("per_page", handler.RequestUris[1]);
    }

    [Fact]
    public async Task GetCompetitionsAsync_FollowsAbsoluteNextPageWithoutDuplicatingApiVersion()
    {
        using var client = CreateClient(out var handler,
            """
            {
              "data": [{ "id": 1, "name": "League One" }],
              "pagination": {
                "has_more": true,
                "next_page": "https://api.sportmonks.com/v3/football/leagues?include=country&per_page=50&page=2"
              }
            }
            """,
            """
            {
              "data": [{ "id": 2, "name": "League Two" }],
              "pagination": {
                "has_more": false,
                "next_page": null
              }
            }
            """);
        var provider = CreateProvider(client);

        var leagues = await provider.GetCompetitionsAsync();

        Assert.Equal(new[] { "1", "2" }, leagues.Select(league => league.ExternalId));
        Assert.Equal(2, handler.RequestUris.Count);
        Assert.Contains("/v3/football/leagues?", handler.RequestUris[1]);
        Assert.DoesNotContain("/v3/v3/", handler.RequestUris[1]);
        Assert.Contains("page=2", handler.RequestUris[1]);
    }

    [Fact]
    public async Task GetFixturesAsync_MapsScheduleParticipantsResultsAndTeams()
    {
        using var client = CreateClient("""
            {
              "data": [
                {
                  "rounds": [
                    {
                      "fixtures": [
                        {
                          "id": 123,
                          "league_id": 384,
                          "season_id": 23697,
                          "starting_at_timestamp": 1730058300,
                          "result_info": "Inter won after full-time.",
                          "participants": [
                            { "id": 10, "name": "Inter", "meta": { "location": "home" } },
                            { "id": 20, "name": "Juventus", "meta": { "location": "away" } }
                          ],
                          "scores": [
                            {
                              "participant_id": 10,
                              "description": "CURRENT",
                              "score": { "goals": 4, "participant": "home" }
                            },
                            {
                              "participant_id": 20,
                              "description": "CURRENT",
                              "score": { "goals": 2, "participant": "away" }
                            },
                            {
                              "participant_id": 10,
                              "description": "1ST_HALF",
                              "score": { "goals": 1, "participant": "home" }
                            },
                            {
                              "participant_id": 20,
                              "description": "1ST_HALF",
                              "score": { "goals": 0, "participant": "away" }
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """, out _);
        var provider = CreateProvider(client);

        var fixtures = await provider.GetFixturesAsync("384", "23697");
        var fixture = Assert.Single(fixtures);
        var teams = await provider.GetTeamsAsync("384", "23697");

        Assert.Equal("123", fixture.ExternalId);
        Assert.Equal("finished", fixture.Status);
        Assert.Equal(4, fixture.HomeScore);
        Assert.Equal(2, fixture.AwayScore);
        Assert.Equal(1, fixture.HomeScoreHalfTime);
        Assert.Equal(0, fixture.AwayScoreHalfTime);
        Assert.Equal(new[] { "Inter", "Juventus" }, teams.Select(team => team.Name).Order().ToArray());
    }

    [Fact]
    public async Task GetOddsAsync_GroupsSelectionsAndMapsFullTimeMarket()
    {
        using var client = CreateClient("""
            {
              "data": [
                {
                  "fixture_id": 123,
                  "market_id": 1,
                  "bookmaker_id": 34,
                  "label": "Home",
                  "value": "2.10",
                  "stopped": false,
                  "latest_bookmaker_update": "2024-10-27 19:40:00",
                  "updated_at": "2024-10-27T19:40:05Z",
                  "market": { "name": "Fulltime Result", "developer_name": "FULLTIME_RESULT" },
                  "bookmaker": { "name": "Example Bookmaker" }
                },
                {
                  "fixture_id": 123,
                  "market_id": 1,
                  "bookmaker_id": 34,
                  "label": "X",
                  "value": "3.20",
                  "stopped": false,
                  "latest_bookmaker_update": "2024-10-27 19:40:00",
                  "updated_at": "2024-10-27T19:40:05Z",
                  "market": { "name": "Fulltime Result", "developer_name": "FULLTIME_RESULT" },
                  "bookmaker": { "name": "Example Bookmaker" }
                },
                {
                  "fixture_id": 123,
                  "market_id": 1,
                  "bookmaker_id": 34,
                  "label": "2",
                  "value": "3.50",
                  "stopped": false,
                  "latest_bookmaker_update": "2024-10-27 19:40:00",
                  "updated_at": "2024-10-27T19:40:05Z",
                  "market": { "name": "Fulltime Result", "developer_name": "FULLTIME_RESULT" },
                  "bookmaker": { "name": "Example Bookmaker" }
                }
              ]
            }
            """, out _);
        var provider = CreateProvider(client);

        var snapshots = await provider.GetOddsAsync("123");
        var snapshot = Assert.Single(snapshots);
        var market = Assert.Single(snapshot.Markets);

        Assert.Equal("34", snapshot.BookmakerExternalId);
        Assert.Equal("123", snapshot.MatchExternalId);
        Assert.False(snapshot.IsLive);
        Assert.Equal("1", market.MarketExternalId);
        Assert.Equal(
            new[] { "Home", "Draw", "Away" },
            market.Selections.Select(selection => selection.Name).ToArray());
        Assert.Equal(new[] { 2.10m, 3.20m, 3.50m }, market.Selections.Select(selection => selection.DecimalOdds));

        using var marketClient = CreateClient("""
            {
              "data": [
                {
                  "id": 1,
                  "name": "Fulltime Result",
                  "developer_name": "FULLTIME_RESULT"
                }
              ]
            }
            """, out _);
        var markets = await CreateProvider(marketClient).GetMarketsAsync();
        Assert.Equal("1X2", Assert.Single(markets).Name);
    }

    [Fact]
    public async Task GetCompetitionsAsync_WithoutTokenThrowsActionableError()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(["{}"]))
        {
            BaseAddress = new Uri("https://api.sportmonks.com/v3/"),
        };
        var provider = new SportmonksProvider(client, new ConfigurationBuilder().Build());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetCompetitionsAsync());

        Assert.Contains("Sportmonks:ApiToken", exception.Message);
    }

    private static SportmonksProvider CreateProvider(HttpClient client) =>
        new(
            client,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Sportmonks:ApiToken"] = "test-token",
                })
                .Build());

    private static HttpClient CreateClient(
        string responseBody,
        out StubHttpMessageHandler handler) =>
        CreateClient(out handler, responseBody);

    private static HttpClient CreateClient(
        out StubHttpMessageHandler handler,
        params string[] responseBodies)
    {
        handler = new StubHttpMessageHandler(responseBodies);

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.sportmonks.com/v3/"),
        };
    }

    private sealed class StubHttpMessageHandler(string[] responseBodies) : HttpMessageHandler
    {
        public string? AuthorizationToken { get; set; }
        public string? RequestUri { get; set; }
        public List<string> RequestUris { get; } = [];
        private int _responseIndex;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            AuthorizationToken = request.Headers.GetValues("Authorization").Single();
            RequestUri = request.RequestUri?.ToString();
            RequestUris.Add(RequestUri ?? string.Empty);
            var responseBody = responseBodies[Math.Min(_responseIndex, responseBodies.Length - 1)];
            _responseIndex++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody),
            });
        }
    }
}
