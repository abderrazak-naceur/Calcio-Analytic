using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CalcioAnalytic.Workers;

/// <summary>
/// Periodically refreshes a bounded set of SportMonks fixtures and pre-match
/// odds. The worker is opt-in and throttles every provider-backed operation so
/// the deployment can be aligned with the limits of the subscribed SportMonks plan.
/// </summary>
public sealed class SportmonksSyncWorker : BackgroundService
{
    private const string ProviderCode = "sportmonks";
    private const string MatchEntityType = "Match";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SportmonksSyncWorker> _logger;

    public SportmonksSyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options,
        IConfiguration configuration,
        ILogger<SportmonksSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IsEnabled(_options, _configuration["Sportmonks:ApiToken"]))
        {
            _logger.LogInformation(
                "SportMonks sync worker is disabled. Enable Worker:SportmonksSyncEnabled and configure Sportmonks:ApiToken to activate it.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(60, _options.IntervalSeconds));
        var delay = TimeSpan.FromMilliseconds(Math.Max(250, _options.SportmonksRequestDelayMilliseconds));

        _logger.LogInformation(
            "SportMonks sync worker started; interval {IntervalMinutes}m, batch {BatchSize}, request delay {DelayMs}ms.",
            interval.TotalMinutes,
            Math.Max(1, _options.SportmonksBatchSize),
            delay.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncCycleAsync(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SportMonks synchronization cycle failed; retrying next interval.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public static bool IsEnabled(WorkerOptions options, string? apiToken) =>
        options.SportmonksSyncEnabled && !string.IsNullOrWhiteSpace(apiToken);

    private async Task SyncCycleAsync(TimeSpan requestDelay, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CalcioAnalyticDbContext>();
        var fixtures = scope.ServiceProvider.GetRequiredService<IMatchIngestionService>();
        var odds = scope.ServiceProvider.GetRequiredService<IOddsIngestionService>();

        var providerId = await db.Providers.AsNoTracking()
            .Where(p => p.Code == ProviderCode && p.IsEnabled)
            .Select(p => (Guid?)p.Id)
            .SingleOrDefaultAsync(ct);

        if (providerId is null)
        {
            _logger.LogWarning("SportMonks sync skipped because the provider is not enabled in the catalog.");
            return;
        }

        var now = DateTime.UtcNow;
        var lookahead = now.AddHours(Math.Max(1, _options.SportmonksLookaheadHours));
        var recent = now.AddHours(-Math.Max(1, _options.SportmonksRecentHours));
        var batchSize = Math.Max(1, _options.SportmonksBatchSize);

        var candidates = await (
            from match in db.Matches.AsNoTracking()
            join map in db.ProviderEntityMaps.AsNoTracking()
                on match.Id equals map.InternalId
            where map.ProviderId == providerId.Value
                && map.EntityType == MatchEntityType
                && ((match.Status == MatchStatus.Scheduled || match.Status == MatchStatus.PreMatch)
                    && match.KickoffUtc >= now && match.KickoffUtc <= lookahead
                    || match.Status == MatchStatus.Finished
                    && match.KickoffUtc >= recent && match.KickoffUtc <= now)
            orderby match.KickoffUtc
            select new Candidate(match.Id, map.ExternalId, match.Status))
            .Take(batchSize)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            _logger.LogDebug("SportMonks sync found no matches in the configured synchronization window.");
            return;
        }

        _logger.LogInformation("SportMonks sync refreshing {Count} match(es).", candidates.Count);

        foreach (var candidate in candidates)
        {
            ct.ThrowIfCancellationRequested();

            var matchId = await fixtures.IngestFixtureAsync(ProviderCode, candidate.ExternalId, ct);
            if (matchId is null)
            {
                _logger.LogWarning("SportMonks fixture {ExternalId} no longer exists.", candidate.ExternalId);
                continue;
            }

            await Task.Delay(requestDelay, ct);
            await odds.IngestOddsAsync(ProviderCode, candidate.ExternalId, ct);
            await Task.Delay(requestDelay, ct);
        }
    }

    private sealed record Candidate(Guid MatchId, string ExternalId, MatchStatus Status);
}
