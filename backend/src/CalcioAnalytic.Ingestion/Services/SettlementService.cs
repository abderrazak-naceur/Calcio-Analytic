using CalcioAnalytic.Analytics.Settlement;
using CalcioAnalytic.Application.Abstractions.Clock;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Application.Settlement;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Domain.Settlement;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Application-level implementation of <see cref="ISettlementService"/>. Loads a
/// finished match and its market lines and selections from persistence,
/// delegates the outcome logic to the pure <see cref="ISettlementEngine"/>, and
/// upserts the resulting <see cref="MarketSettlement"/> rows.
/// </summary>
/// <remarks>
/// <para>
/// Settlement is idempotent. A settlement is keyed by
/// (match, market line, selection): when a matching row already exists its
/// <see cref="MarketSettlement.Status"/> and <see cref="MarketSettlement.SettledAtUtc"/>
/// are updated in place; otherwise a new row is inserted. Re-running settlement
/// for the same match therefore never produces duplicate rows.
/// </para>
/// </remarks>
public sealed class SettlementService : ISettlementService
{
    private readonly IMatchRepository _matches;
    private readonly IRepository<MarketLine> _marketLines;
    private readonly IRepository<Selection> _selections;
    private readonly IRepository<MarketSettlement> _settlements;
    private readonly ISettlementEngine _engine;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<SettlementService> _logger;

    /// <summary>Initializes a new instance of the <see cref="SettlementService"/> class.</summary>
    public SettlementService(
        IMatchRepository matches,
        IRepository<MarketLine> marketLines,
        IRepository<Selection> selections,
        IRepository<MarketSettlement> settlements,
        ISettlementEngine engine,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<SettlementService> logger)
    {
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(marketLines);
        ArgumentNullException.ThrowIfNull(selections);
        ArgumentNullException.ThrowIfNull(settlements);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _matches = matches;
        _marketLines = marketLines;
        _selections = selections;
        _settlements = settlements;
        _engine = engine;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> SettleMatchAsync(Guid matchId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting settlement for match {MatchId}.", matchId);

        var match = await _matches.GetByIdAsync(matchId, ct).ConfigureAwait(false);
        if (match is null)
        {
            _logger.LogWarning("Match {MatchId} was not found; nothing to settle.", matchId);
            return 0;
        }

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allLines = await _marketLines.ListAsync(ct).ConfigureAwait(false);
        var lines = allLines.Where(l => l.MatchId == matchId).ToList();
        if (lines.Count == 0)
        {
            _logger.LogInformation("Match {MatchId} has no market lines; nothing to settle.", matchId);
            return 0;
        }

        var lineIds = lines.Select(l => l.Id).ToHashSet();

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allSelections = await _selections.ListAsync(ct).ConfigureAwait(false);
        var selectionsByLine = allSelections
            .Where(s => lineIds.Contains(s.MarketLineId))
            .GroupBy(s => s.MarketLineId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var settlementLines = lines
            .Select(line => new SettlementMarketLine(
                line.Id,
                line.Line,
                line.Period,
                selectionsByLine.TryGetValue(line.Id, out var sels)
                    ? sels.Select(s => new SettlementSelection(s.Id, s.Name)).ToList()
                    : new List<SettlementSelection>()))
            .ToList();

        var input = new SettlementInput(match, settlementLines);
        var results = _engine.Settle(input);

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allSettlements = await _settlements.ListAsync(ct).ConfigureAwait(false);
        var existingByKey = allSettlements
            .Where(s => s.MatchId == matchId)
            .ToDictionary(s => (s.MarketLineId, s.SelectionId));

        var now = _clock.UtcNow;
        var count = 0;
        foreach (var result in results)
        {
            var key = (result.MarketLineId, result.SelectionId);
            if (existingByKey.TryGetValue(key, out var existing))
            {
                existing.Status = result.Status;
                existing.SettledAtUtc = now;
                existing.UpdatedAtUtc = now;
                _settlements.Update(existing);
            }
            else
            {
                var settlement = new MarketSettlement
                {
                    Id = Guid.NewGuid(),
                    MatchId = matchId,
                    MarketLineId = result.MarketLineId,
                    SelectionId = result.SelectionId,
                    Status = result.Status,
                    SettledAtUtc = now,
                    CreatedAtUtc = now,
                };

                await _settlements.AddAsync(settlement, ct).ConfigureAwait(false);
                existingByKey[key] = settlement;
            }

            count++;
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Completed settlement for match {MatchId}: {Count} selections settled.",
            matchId,
            count);

        return count;
    }
}
