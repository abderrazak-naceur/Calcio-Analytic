using System.Net;
using System.Net.Http.Headers;
using CalcioAnalytic.Analytics.Patterns;
using CalcioAnalytic.Analytics.Similarity;
using CalcioAnalytic.Api.Contracts.Dtos;
using CalcioAnalytic.Api.Services;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace CalcioAnalytic.Api.Controllers;

/// <summary>
/// Analytics query endpoints: historical pattern aggregation, similar-match
/// retrieval, and a resilient proxy to the Python analytics service for
/// backtests. Reads are performed read-only and asynchronously; the match-to-
/// feature projection lives in <see cref="MatchFeatureProjector"/> and the data
/// loading in <see cref="MatchFeatureLoader"/>, keeping these actions thin.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
public sealed class PatternsController : ControllerBase
{
    /// <summary>
    /// A single shared <see cref="HttpClient"/> for the backtest proxy. A static
    /// instance is used deliberately: <c>IHttpClientFactory</c> is not registered
    /// in the DI container and this project must not modify the startup pipeline,
    /// so a long-lived static client avoids socket exhaustion from per-request
    /// <c>new HttpClient()</c> usage.
    /// </summary>
    private static readonly HttpClient BacktestHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
    };

    private const string DefaultAnalyticsBaseUrl = "http://localhost:8000";

    private readonly CalcioAnalyticDbContext _db;
    private readonly IHistoricalPatternEngine _patternEngine;
    private readonly ISimilarMatchEngine _similarEngine;
    private readonly IConfiguration _configuration;

    public PatternsController(
        CalcioAnalyticDbContext db,
        IHistoricalPatternEngine patternEngine,
        ISimilarMatchEngine similarEngine,
        IConfiguration configuration)
    {
        _db = db;
        _patternEngine = patternEngine;
        _similarEngine = similarEngine;
        _configuration = configuration;
    }

    /// <summary>
    /// Runs a historical pattern query. Loads all finished matches, projects each
    /// to a <see cref="PatternMatchRecord"/> (deriving the full-time outcome from
    /// the score and the opening/closing home odds from the match's 1X2 "Home"
    /// snapshots), then aggregates via the pattern engine. Fields lacking source
    /// data (e.g. odds when no snapshots exist, or the over/under opening line,
    /// which is not yet sourced) are returned as null and excluded from averages.
    /// </summary>
    /// <param name="request">The query filters, mirroring <see cref="PatternQuery"/>.</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpPost("patterns/query")]
    [ProducesResponseType(typeof(PatternResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatternResult>> QueryPatterns(
        [FromBody] PatternQueryRequest request,
        CancellationToken ct)
    {
        var query = new PatternQuery(
            request.CompetitionId,
            request.SeasonId,
            request.FromUtc,
            request.ToUtc,
            request.HomeOrAway,
            request.MinClosingHomeOdds,
            request.MaxClosingHomeOdds,
            request.MinMovementPercentage,
            request.MaxMovementPercentage);

        var loader = new MatchFeatureLoader(_db);
        var finished = await loader.LoadFinishedAsync(ct);

        var records = finished
            .Select(x => MatchFeatureProjector.ToPatternRecord(x.Match, x.HomeSnapshots))
            .ToList();

        var result = _patternEngine.Analyze(query, records);
        return Ok(result);
    }

    /// <summary>
    /// Returns the finished matches most similar to match <paramref name="id"/>.
    /// Builds the target's <see cref="SimilarityFeatures"/> from its 1X2 "Home"
    /// snapshots and compares it against all other finished matches. Ratings, form,
    /// and goal averages are placeholder zeros until a feature store exists
    /// (TASK-031), so ranking currently reflects odds-based signals only.
    /// Returns 404 when the match does not exist.
    /// </summary>
    /// <param name="id">The target match identifier.</param>
    /// <param name="topK">The maximum number of neighbors to return (default 10).</param>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpGet("matches/{id:guid}/similar")]
    [ProducesResponseType(typeof(IReadOnlyList<SimilarMatch>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SimilarMatch>>> GetSimilar(
        Guid id,
        [FromQuery] int topK,
        CancellationToken ct)
    {
        var loader = new MatchFeatureLoader(_db);

        var target = await loader.LoadByIdAsync(id, ct);
        if (target is null)
        {
            return NotFound();
        }

        var finished = await loader.LoadFinishedAsync(ct);

        // The full finished-match pool for point-in-time feature computation. Each
        // match's features are computed as-of its own kickoff, using only matches
        // that kicked off strictly earlier (see TeamFeatureProjector), so no future
        // data leaks into any feature vector (TASK-031 anti-leakage).
        var pool = finished.Select(x => x.Match).ToList();

        var targetFeatures = MatchFeatureProjector.ToSimilarityFeatures(
            target.Match,
            target.HomeSnapshots,
            ComputeTeamFeatureInputs(target.Match, pool));

        var candidates = finished
            .Where(x => x.Match.Id != id)
            .Select(x => MatchFeatureProjector.ToSimilarityFeatures(
                x.Match,
                x.HomeSnapshots,
                ComputeTeamFeatureInputs(x.Match, pool)))
            .ToList();

        var options = topK > 0 ? new SimilarityOptions(TopK: topK) : SimilarityOptions.Default;

        var results = _similarEngine.FindSimilar(targetFeatures, candidates, options);
        return Ok(results);
    }

    /// <summary>
    /// Computes the point-in-time home/away team features and ELO-style ratings for
    /// a match as-of its own kickoff, drawing only on the supplied finished-match
    /// <paramref name="pool"/>. Because <see cref="TeamFeatureProjector"/> restricts
    /// every computation to matches that kicked off strictly before the cutoff, the
    /// resulting inputs contain no information from this match or any later one
    /// (anti-leakage).
    /// </summary>
    private static MatchFeatureProjector.TeamFeatureInputs ComputeTeamFeatureInputs(
        CalcioAnalytic.Domain.Matches.Match match,
        IReadOnlyList<CalcioAnalytic.Domain.Matches.Match> pool)
    {
        var cutoff = match.KickoffUtc;

        var homeForm = TeamFeatureProjector.ComputeAsOf(match.HomeTeamId, cutoff, pool);
        var awayForm = TeamFeatureProjector.ComputeAsOf(match.AwayTeamId, cutoff, pool);
        var homeRating = TeamFeatureProjector.ComputeRatingAsOf(match.HomeTeamId, cutoff, pool);
        var awayRating = TeamFeatureProjector.ComputeRatingAsOf(match.AwayTeamId, cutoff, pool);

        return new MatchFeatureProjector.TeamFeatureInputs(homeForm, awayForm, homeRating, awayRating);
    }

    /// <summary>
    /// Proxies a backtest request to the Python analytics service. Reads the base
    /// URL from configuration ("Analytics:BaseUrl") or the ANALYTICS_BASE_URL
    /// environment variable, defaulting to <c>http://localhost:8000</c>, and
    /// forwards the received JSON body to <c>{base}/api/v1/backtests/</c>. The
    /// downstream status code and response body are relayed verbatim. When the
    /// service is unreachable or times out, responds with 502 and a small error
    /// payload.
    /// </summary>
    /// <param name="ct">A token to observe for cancellation.</param>
    [HttpPost("backtests")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BacktestProxyErrorDto), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ProxyBacktest(CancellationToken ct)
    {
        var baseUrl = ResolveAnalyticsBaseUrl();
        var target = $"{baseUrl.TrimEnd('/')}/api/v1/backtests/";

        // Buffer the incoming body so we can forward it verbatim.
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync(ct);
        }

        try
        {
            using var content = new StringContent(body);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var upstream = await BacktestHttpClient.PostAsync(target, content, ct);
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
