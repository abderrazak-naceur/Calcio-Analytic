using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Domain.Matches;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Bounded, repeatable historical backfill orchestrator. Match ingestion is
/// idempotent; odds ingestion is append-only/deduplicated, so re-running a
/// period is safe.
/// </summary>
public sealed class HistoricalBackfillService : IHistoricalBackfillService
{
    private readonly IMatchIngestionService _matches;
    private readonly IOddsIngestionService _odds;
    private readonly IResultReconciliationService _results;
    private readonly IHistoricalMatchQuery _query;
    private readonly ILogger<HistoricalBackfillService> _logger;

    public HistoricalBackfillService(
        IMatchIngestionService matches,
        IOddsIngestionService odds,
        IResultReconciliationService results,
        IHistoricalMatchQuery query,
        ILogger<HistoricalBackfillService> logger)
    {
        _matches = matches;
        _odds = odds;
        _results = results;
        _query = query;
        _logger = logger;
    }

    public async Task<HistoricalBackfillResult> RunAsync(
        HistoricalBackfillRequest request,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProviderCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CompetitionExternalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SeasonExternalId);
        if (request.FromUtc >= request.ToUtc)
            throw new ArgumentException("FromUtc must be earlier than ToUtc.");

        var discovered = await _matches.IngestFixturesAsync(
            request.ProviderCode,
            request.CompetitionExternalId,
            request.SeasonExternalId,
            ct);

        var candidates = await _query.FindAsync(
            request.ProviderCode,
            request.FromUtc,
            request.ToUtc,
            ct);

        var oddsProcessed = 0;
        var reconciled = 0;
        foreach (var match in candidates)
        {
            ct.ThrowIfCancellationRequested();
            if (request.IncludeOdds)
            {
                var odds = await _odds.IngestOddsAsync(request.ProviderCode, match.ExternalId, ct);
                oddsProcessed += odds.SnapshotsInserted;
            }

            if (match.Status is MatchStatus.Finished or MatchStatus.SettlementPending or MatchStatus.Analyzed)
            {
                var result = await _results.ReconcileMatchAsync(match.MatchId, ct);
                if (result.Confirmed)
                    reconciled++;
            }
        }

        _logger.LogInformation(
            "Historical backfill completed: discovered={Discovered}, window={Window}, odds={Odds}, reconciled={Reconciled}.",
            discovered.MatchesUpserted, candidates.Count, oddsProcessed, reconciled);

        return new HistoricalBackfillResult(discovered.MatchesUpserted, candidates.Count, oddsProcessed, reconciled);
    }
}
