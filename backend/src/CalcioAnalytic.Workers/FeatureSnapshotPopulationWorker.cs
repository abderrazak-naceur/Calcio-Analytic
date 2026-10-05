using CalcioAnalytic.Application.Abstractions.Features;
using Microsoft.Extensions.Options;

namespace CalcioAnalytic.Workers;

public sealed class FeatureSnapshotPopulationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WorkerOptions _options;
    private readonly ILogger<FeatureSnapshotPopulationWorker> _logger;

    public FeatureSnapshotPopulationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<WorkerOptions> options,
        ILogger<FeatureSnapshotPopulationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(5, _options.FeatureSnapshotIntervalMinutes));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IFeatureSnapshotPopulationService>();
                var created = await service.PopulateMissingAsync(_options.FeatureSnapshotBatchSize, stoppingToken);
                if (created > 0)
                    _logger.LogInformation("Point-in-time feature population created {Count} snapshot(s).", created);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Point-in-time feature population cycle failed."); }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
