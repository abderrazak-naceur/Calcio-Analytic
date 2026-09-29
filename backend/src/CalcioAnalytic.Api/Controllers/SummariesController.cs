using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CalcioAnalytic.Analytics.Match;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalcioAnalytic.Api.Controllers;

/// <summary>
/// Produces an AI-generated, strictly descriptive (non-predictive) summary for a
/// match. The endpoint builds the match's structured analysis report locally and
/// proxies it to the Python analytics summaries service, which turns the facts
/// into human-readable, traceable prose. The downstream response is relayed
/// verbatim so the summary contract stays owned by the analytics service.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
public sealed class SummariesController : ControllerBase
{
    /// <summary>
    /// A single shared <see cref="HttpClient"/> for the summaries proxy. A static
    /// instance is used deliberately: <c>IHttpClientFactory</c> is not registered
    /// in the DI container and this project must not modify the startup pipeline,
    /// so a long-lived static client avoids socket exhaustion from per-request
    /// <c>new HttpClient()</c> usage. Mirrors the pattern in
    /// <see cref="PatternsController"/>.
    /// </summary>
    private static readonly HttpClient SummaryHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private const string DefaultAnalyticsBaseUrl = "http://localhost:8000";

    /// <summary>
    /// Serializer options for the outgoing facts payload. camelCase property names
    /// and string enum values match the API's own JSON configuration (see
    /// <c>Program.cs</c>), so the facts the analytics service receives are shaped
    /// exactly like the report returned by <c>AnalyticsController.GetAnalysis</c>.
    /// </summary>
    private static readonly JsonSerializerOptions FactsSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly CalcioAnalyticDbContext _db;
    private readonly IMatchAnalysisEngine _engine;
    private readonly IConfiguration _configuration;

    public SummariesController(
        CalcioAnalyticDbContext db,
        IMatchAnalysisEngine engine,
        IConfiguration configuration)
    {
        _db = db;
        _engine = engine;
        _configuration = configuration;
    }

    /// <summary>
    /// Builds the analysis report for a match and returns a descriptive,
    /// non-predictive summary of it. Loads the match and all related data
    /// read-only, runs the analysis engine to obtain the structured report, then
    /// POSTs <c>{ "facts": &lt;report&gt; }</c> to the Python analytics summaries
    /// service at <c>{base}/api/v1/summaries/match</c> and relays the downstream
    /// status code and body verbatim. Returns 404 when the match does not exist
    /// and 502 when the analytics service is unreachable or times out.
    /// </summary>
    /// <param name="id">The canonical match identifier.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("matches/{id:guid}/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BacktestProxyErrorDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetSummary(Guid id, CancellationToken ct)
    {
        // NOTE: The analysis-loading block below mirrors
        // AnalyticsController.GetAnalysis. A little duplication is accepted here to
        // keep the controller thin; it could be extracted to a shared loader later.
        var match = await _db.Matches.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (match is null)
        {
            return NotFound();
        }

        var oddsSnapshots = await _db.OddsSnapshots.AsNoTracking()
            .Where(s => s.MatchId == id)
            .ToListAsync(ct);

        var marketLines = await _db.MarketLines.AsNoTracking()
            .Where(l => l.MatchId == id)
            .ToListAsync(ct);

        var marketLineIds = marketLines.Select(l => l.Id).ToList();

        var selections = await _db.Selections.AsNoTracking()
            .Where(sel => marketLineIds.Contains(sel.MarketLineId))
            .ToListAsync(ct);

        var statistics = await _db.MatchStatistics.AsNoTracking()
            .Where(s => s.MatchId == id)
            .ToListAsync(ct);

        var events = await _db.MatchEvents.AsNoTracking()
            .Where(e => e.MatchId == id)
            .ToListAsync(ct);

        var settlements = await _db.MarketSettlements.AsNoTracking()
            .Where(s => s.MatchId == id)
            .ToListAsync(ct);

        var bookmakerIds = oddsSnapshots
            .Select(s => s.BookmakerId)
            .Distinct()
            .ToList();

        var input = new MatchAnalysisInput(
            match,
            oddsSnapshots,
            marketLines,
            selections,
            bookmakerIds,
            statistics,
            events,
            settlements);

        var output = _engine.AnalyzeToJson(input);

        // Wrap the report as { "facts": <report> } to match the summaries request
        // schema, serializing with camelCase + string enums so the facts arrive in
        // the same shape the API exposes elsewhere.
        var payload = JsonSerializer.Serialize(new { facts = output.Report }, FactsSerializerOptions);

        var baseUrl = ResolveAnalyticsBaseUrl();
        var target = $"{baseUrl.TrimEnd('/')}/api/v1/summaries/match";

        try
        {
            using var content = new StringContent(payload, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var upstream = await SummaryHttpClient.PostAsync(target, content, ct);
            var responseBody = await upstream.Content.ReadAsStringAsync(ct);

            return new ContentResult
            {
                StatusCode = (int)upstream.StatusCode,
                Content = responseBody,
                ContentType = upstream.Content.Headers.ContentType?.ToString() ?? "application/json",
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or WebException)
        {
            var error = new BacktestProxyErrorDto(
                Error: "analytics_service_unreachable",
                Message: $"Could not reach the analytics service: {ex.Message}",
                Target: target);

            return StatusCode(StatusCodes.Status502BadGateway, error);
        }
    }

    /// <summary>
    /// Resolves the Python analytics base URL from configuration
    /// ("Analytics:BaseUrl"), then the ANALYTICS_BASE_URL environment variable,
    /// finally falling back to the local default.
    /// </summary>
    private string ResolveAnalyticsBaseUrl()
    {
        var configured = _configuration["Analytics:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var fromEnv = Environment.GetEnvironmentVariable("ANALYTICS_BASE_URL");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        return DefaultAnalyticsBaseUrl;
    }
}
