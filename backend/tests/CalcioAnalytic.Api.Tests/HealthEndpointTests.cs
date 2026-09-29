using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CalcioAnalytic.Api.Tests;

/// <summary>
/// Integration tests for the API health endpoints. The liveness probe
/// (<c>/health/live</c>) intentionally excludes external dependencies, so it
/// can be verified without a running PostgreSQL instance.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Liveness_returns_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthPayload>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body!.Status);
    }

    [Fact]
    public async Task Root_returns_service_info()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RootPayload>();
        Assert.NotNull(body);
        Assert.Equal("Calcio-Analytic API", body!.Name);
    }

    [Fact]
    public async Task Response_carries_correlation_id_header()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    private sealed record HealthPayload(string Status);

    private sealed record RootPayload(string Name, string Version, string Status);
}
