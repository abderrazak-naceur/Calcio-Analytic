namespace CalcioAnalytic.Application.Ingestion;

public sealed record HistoricalBackfillRequest(
    string ProviderCode,
    string CompetitionExternalId,
    string SeasonExternalId,
    DateTime FromUtc,
    DateTime ToUtc,
    bool IncludeOdds = true);

public sealed record HistoricalBackfillResult(
    int MatchesDiscovered,
    int MatchesInWindow,
    int OddsProcessed,
    int ResultsReconciled);

public interface IHistoricalBackfillService
{
    Task<HistoricalBackfillResult> RunAsync(
        HistoricalBackfillRequest request,
        CancellationToken ct = default);
}
