using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Application.Analytics;
using CalcioAnalytic.Application.Settlement;
using CalcioAnalytic.Domain.Matches;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CalcioAnalytic.Workers;

/// <summary>
/// Hosted background service that drives the post-match lifecycle. On each cycle
/// it finds finished matches that still need post-match processing and, for each,
/// runs settlement followed by analysis, then advances the match from
/// <see cref="MatchStatus.Finished"/> to <see cref="MatchStatus.Analyzed"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Idempotency.</b> Settlement (<see cref="ISettlementService.SettleMatchAsync"/>)
/// and analysis (<see cref="IMatchAnalysisPersistenceService.GenerateAndStoreAsync"/>)
/// are themselves idempotent/versioned, so re-running them for the same match is
/// safe. The worker only selects matches whose status is
/// <see cref="MatchStatus.Finished"/>; once a match is advanced to
/// <see cref="MatchStatus.Analyzed"/> it is no longer returned by the query and
/// will not be picked up again. If a cycle fails part-way (for example the status
/// transition never commits), the match stays <see cref="MatchStatus.Finished"/>
/// and is simply reprocessed on a later cycle without duplicating stored data.
/// </para>
/// <para>
/// <b>Resilience.</b> Each match is processed inside its own try/catch so a single
/// failure does not abort the batch, and the whole cycle is wrapped so that a
/// transient outage (such as the database being unavailable at startup) is logged
/// and retried on the next cycle rather than crashing the host.
/// </para>
/// </remarks>
public sealed class PostMatchProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerOptions _options;
    private readonly ILogger<PostMatchProcessingWorker> _logger;

    /// <summary>Initializes a new instance of the <see cref="PostMatchProcessingWorker"/> class.</summary>
    /// <param name="scopeFactory">Factory used to create a DI scope per cycle.</param>
    /// <param name="options">The bound worker options.</param>
    /// <param name="logger">The logger.</param>
    public PostMatchProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options,
        ILogger<PostMatchProcessingWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = _options.IntervalSeconds > 0 ? _options.IntervalSeconds : 30;
        var interval = TimeSpan.FromSeconds(seconds);

        _logger.LogInformation(
            "PostMatchProcessingWorker started; polling every {IntervalSeconds}s.",
            seconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown requested; exit the loop.
                break;
            }
            catch (Exception ex)
            {
                // Never let a cycle-level failure (e.g. DB unavailable) crash the host.
                _logger.LogError(ex, "Post-match processing cycle failed; will retry next interval.");
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

        _logger.LogInformation("PostMatchProcessingWorker stopping.");
    }

    /// <summary>
    /// Runs a single processing cycle: creates a scope, loads all finished
    /// matches, and processes each one independently.
    /// </summary>
    /// <param name="stoppingToken">A token to observe for cancellation.</param>
    private async Task ProcessCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

        var finished = await matches.GetByStatusAsync(MatchStatus.Finished, stoppingToken);

        if (finished.Count == 0)
        {
            _logger.LogDebug("No finished matches awaiting post-match processing.");
            return;
        }

        _logger.LogInformation(
            "Found {Count} finished match(es) awaiting post-match processing.",
            finished.Count);

        var settlement = scope.ServiceProvider.GetRequiredService<ISettlementService>();
        var analysis = scope.ServiceProvider.GetRequiredService<IMatchAnalysisPersistenceService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        foreach (var match in finished)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await ProcessMatchAsync(match.Id, matches, settlement, analysis, unitOfWork, stoppingToken);
        }
    }

    /// <summary>
    /// Processes one match end-to-end: settlement, analysis, then the
    /// <see cref="MatchStatus.Finished"/> to <see cref="MatchStatus.Analyzed"/>
    /// transition. Any failure is logged and swallowed so the batch continues.
    /// </summary>
    private async Task ProcessMatchAsync(
        Guid matchId,
        IMatchRepository matches,
        ISettlementService settlement,
        IMatchAnalysisPersistenceService analysis,
        IUnitOfWork unitOfWork,
        CancellationToken stoppingToken)
    {
        try
        {
            var settledCount = await settlement.SettleMatchAsync(matchId, stoppingToken);
            var version = await analysis.GenerateAndStoreAsync(matchId, stoppingToken);

            // Reload through the tracking repository so the update is staged
            // against a tracked entity, then advance the lifecycle status.
            var match = await matches.GetByIdAsync(matchId, stoppingToken);
            if (match is null)
            {
                _logger.LogWarning(
                    "Match {MatchId} disappeared before status transition; skipping.",
                    matchId);
                return;
            }

            if (match.Status != MatchStatus.Finished)
            {
                _logger.LogInformation(
                    "Match {MatchId} is no longer Finished (now {Status}); skipping transition.",
                    matchId,
                    match.Status);
                return;
            }

            match.Status = MatchStatus.Analyzed;
            matches.Update(match);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "Processed match {MatchId}: settled {SettledCount} selection(s), stored analysis v{Version}, transitioned Finished -> Analyzed.",
                matchId,
                settledCount,
                version);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Propagate cooperative cancellation to the cycle loop.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process match {MatchId}; it remains Finished and will be retried next cycle.",
                matchId);
        }
    }
}
