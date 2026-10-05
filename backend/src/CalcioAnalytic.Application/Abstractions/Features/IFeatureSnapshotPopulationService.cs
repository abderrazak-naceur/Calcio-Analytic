namespace CalcioAnalytic.Application.Abstractions.Features;

public interface IFeatureSnapshotPopulationService
{
    Task<int> PopulateMissingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
}
