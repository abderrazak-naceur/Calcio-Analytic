using CalcioAnalytic.Analytics.Match;
using CalcioAnalytic.Application.Abstractions.Clock;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Application.Analytics;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Domain.Settlement;
using CalcioAnalytic.Domain.Statistics;
using Microsoft.Extensions.Logging;
using DomainMatchAnalysis = CalcioAnalytic.Domain.Analytics.MatchAnalysis;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Application-level implementation of <see cref="IMatchAnalysisPersistenceService"/>.
/// Loads a match and every collection the analysis engine needs, delegates the
/// analysis to the pure <see cref="IMatchAnalysisEngine"/>, and persists the
/// result as a new, immutable versioned <see cref="DomainMatchAnalysis"/> row.
/// </summary>
/// <remarks>
/// <para>
/// Each call inserts a brand-new analysis row whose version is one greater than
/// the highest existing version for the match (or 1 when none exist). Previously
/// stored versions are never mutated, preserving a complete audit trail.
/// </para>
/// </remarks>
public sealed class MatchAnalysisPersistenceService : IMatchAnalysisPersistenceService
{
    private readonly IMatchRepository _matches;
    private readonly IRepository<OddsSnapshot> _snapshots;
    private readonly IRepository<MarketLine> _marketLines;
    private readonly IRepository<Selection> _selections;
    private readonly IRepository<MatchStatistic> _statistics;
    private readonly IRepository<MatchEvent> _events;
    private readonly IRepository<MarketSettlement> _settlements;
    private readonly IRepository<DomainMatchAnalysis> _analyses;
    private readonly IMatchAnalysisEngine _engine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<MatchAnalysisPersistenceService> _logger;

    /// <summary>Initializes a new instance of the <see cref="MatchAnalysisPersistenceService"/> class.</summary>
    public MatchAnalysisPersistenceService(
        IMatchRepository matches,
        IRepository<OddsSnapshot> snapshots,
        IRepository<MarketLine> marketLines,
        IRepository<Selection> selections,
        IRepository<MatchStatistic> statistics,
        IRepository<MatchEvent> events,
        IRepository<MarketSettlement> settlements,
        IRepository<DomainMatchAnalysis> analyses,
        IMatchAnalysisEngine engine,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<MatchAnalysisPersistenceService> logger)
    {
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(marketLines);
        ArgumentNullException.ThrowIfNull(selections);
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(settlements);
        ArgumentNullException.ThrowIfNull(analyses);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _matches = matches;
        _snapshots = snapshots;
        _marketLines = marketLines;
        _selections = selections;
        _statistics = statistics;
        _events = events;
        _settlements = settlements;
        _analyses = analyses;
        _engine = engine;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> GenerateAndStoreAsync(Guid matchId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting analysis generation for match {MatchId}.", matchId);

        var match = await _matches.GetByIdAsync(matchId, ct).ConfigureAwait(false);
        if (match is null)
        {
            _logger.LogWarning("Match {MatchId} was not found; no analysis generated.", matchId);
            return 0;
        }

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allSnapshots = await _snapshots.ListAsync(ct).ConfigureAwait(false);
        var snapshots = allSnapshots.Where(s => s.MatchId == matchId).ToList();

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allLines = await _marketLines.ListAsync(ct).ConfigureAwait(false);
        var lines = allLines.Where(l => l.MatchId == matchId).ToList();
        var lineIds = lines.Select(l => l.Id).ToHashSet();

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allSelections = await _selections.ListAsync(ct).ConfigureAwait(false);
        var selections = allSelections.Where(s => lineIds.Contains(s.MarketLineId)).ToList();

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allStatistics = await _statistics.ListAsync(ct).ConfigureAwait(false);
        var statistics = allStatistics.Where(s => s.MatchId == matchId).ToList();

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allEvents = await _events.ListAsync(ct).ConfigureAwait(false);
        var events = allEvents.Where(e => e.MatchId == matchId).ToList();

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allSettlements = await _settlements.ListAsync(ct).ConfigureAwait(false);
        var settlements = allSettlements.Where(s => s.MatchId == matchId).ToList();

        var bookmakerIds = snapshots
            .Select(s => s.BookmakerId)
            .Distinct()
            .ToList();

        var input = new MatchAnalysisInput(
            match,
            snapshots,
            lines,
            selections,
            bookmakerIds,
            statistics,
            events,
            settlements);

        var now = _clock.UtcNow;
        var output = _engine.AnalyzeToJson(input, now);

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allAnalyses = await _analyses.ListAsync(ct).ConfigureAwait(false);
        var existingForMatch = allAnalyses.Where(a => a.MatchId == matchId).ToList();
        var nextVersion = existingForMatch.Count == 0
            ? 1
            : existingForMatch.Max(a => a.Version) + 1;

        // Always insert a new, immutable row; existing versions are never mutated.
        var analysis = new DomainMatchAnalysis
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            Version = nextVersion,
            GeneratedAtUtc = now,
            AnalysisJson = output.AnalysisJson,
            MethodologyVersion = output.MethodologyVersion,
            CreatedAtUtc = now,
        };

        await _analyses.AddAsync(analysis, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Completed analysis generation for match {MatchId}: stored version {Version}.",
            matchId,
            nextVersion);

        return nextVersion;
    }
}
