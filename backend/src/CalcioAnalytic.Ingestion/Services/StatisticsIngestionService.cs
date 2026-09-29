using CalcioAnalytic.Application.Abstractions.Clock;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Statistics;
using CalcioAnalytic.Ingestion.Providers;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Ingests provider match statistics and timeline events for a single match into
/// canonical <see cref="MatchStatistic"/> and <see cref="MatchEvent"/> records.
/// </summary>
/// <remarks>
/// <para>
/// Ingestion is idempotent. A statistic is keyed by (match, team, name): when a
/// matching row already exists its value is updated in place instead of a
/// duplicate being inserted. An event is keyed by (match, type, minute, team):
/// an identical event is skipped so re-running the same source data does not
/// duplicate the timeline. Events are never deleted and re-inserted, preserving
/// historical integrity.
/// </para>
/// <para>
/// The match internal id and per-statistic team internal ids are resolved through
/// the <see cref="IProviderEntityMapRepository"/>. When the match, or a
/// referenced team, is not mapped the affected row is skipped with a warning
/// rather than a mapping being invented.
/// </para>
/// </remarks>
public sealed class StatisticsIngestionService : IStatisticsIngestionService
{
    private const string MatchEntityType = "Match";
    private const string TeamEntityType = "Team";

    private readonly IProviderRegistry _registry;
    private readonly IProviderEntityMapRepository _maps;
    private readonly IRepository<Provider> _providers;
    private readonly IRepository<MatchStatistic> _statistics;
    private readonly IRepository<MatchEvent> _events;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<StatisticsIngestionService> _logger;

    /// <summary>Initializes a new instance of the <see cref="StatisticsIngestionService"/> class.</summary>
    public StatisticsIngestionService(
        IProviderRegistry registry,
        IProviderEntityMapRepository maps,
        IRepository<Provider> providers,
        IRepository<MatchStatistic> statistics,
        IRepository<MatchEvent> events,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<StatisticsIngestionService> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _maps = maps;
        _providers = providers;
        _statistics = statistics;
        _events = events;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StatisticsIngestionResult> IngestStatisticsAsync(
        string providerCode,
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);

        _logger.LogInformation(
            "Starting statistics ingestion for provider {ProviderCode}, match {MatchExternalId}.",
            providerCode,
            matchExternalId);

        var statisticsProvider = _registry.Get<IStatisticsProvider>(providerCode);
        var provider = await ResolveProviderAsync(providerCode, ct).ConfigureAwait(false);
        if (provider is null)
        {
            _logger.LogWarning(
                "Provider {ProviderCode} is not registered in the catalog; no statistics ingested for match {MatchExternalId}.",
                providerCode,
                matchExternalId);
            return new StatisticsIngestionResult(0, 0);
        }

        var matchId = await _maps
            .ResolveInternalIdAsync(provider.Id, MatchEntityType, matchExternalId, ct)
            .ConfigureAwait(false);
        if (matchId is not { } matchInternalId)
        {
            _logger.LogWarning(
                "Match {MatchExternalId} is not mapped for provider {ProviderCode}; no statistics ingested.",
                matchExternalId,
                providerCode);
            return new StatisticsIngestionResult(0, 0);
        }

        var statisticsInserted = await IngestStatisticRowsAsync(
            statisticsProvider, provider.Id, matchInternalId, providerCode, matchExternalId, ct)
            .ConfigureAwait(false);

        var eventsInserted = await IngestEventRowsAsync(
            statisticsProvider, provider.Id, matchInternalId, providerCode, matchExternalId, ct)
            .ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Completed statistics ingestion for provider {ProviderCode}, match {MatchExternalId}: {Stats} statistics inserted, {Events} events inserted.",
            providerCode,
            matchExternalId,
            statisticsInserted,
            eventsInserted);

        return new StatisticsIngestionResult(statisticsInserted, eventsInserted);
    }

    /// <summary>
    /// Upserts the provider statistics for the match. Existing statistics keyed by
    /// (match, team, name) are updated in place; missing ones are inserted.
    /// Returns the count of newly inserted rows.
    /// </summary>
    private async Task<int> IngestStatisticRowsAsync(
        IStatisticsProvider provider,
        Guid providerId,
        Guid matchId,
        string providerCode,
        string matchExternalId,
        CancellationToken ct)
    {
        var dtos = await provider.GetStatisticsAsync(matchExternalId, ct).ConfigureAwait(false);
        if (dtos.Count == 0)
        {
            return 0;
        }

        var all = await _statistics.ListAsync(ct).ConfigureAwait(false);
        var existingForMatch = all.Where(s => s.MatchId == matchId).ToList();

        // Track team ids staged in this run so repeated stats for the same team
        // reconcile against the just-inserted row too.
        var pending = new Dictionary<(Guid TeamId, string Name), MatchStatistic>();

        var inserted = 0;
        foreach (var dto in dtos)
        {
            var teamId = await _maps
                .ResolveInternalIdAsync(providerId, TeamEntityType, dto.TeamExternalId, ct)
                .ConfigureAwait(false);
            if (teamId is not { } teamInternalId)
            {
                _logger.LogWarning(
                    "Team {TeamExternalId} is not mapped for provider {ProviderCode}; skipping statistic '{Name}' for match {MatchExternalId}.",
                    dto.TeamExternalId,
                    providerCode,
                    dto.Name,
                    matchExternalId);
                continue;
            }

            if (pending.TryGetValue((teamInternalId, dto.Name), out var staged))
            {
                staged.Value = dto.Value;
                staged.UpdatedAtUtc = _clock.UtcNow;
                _statistics.Update(staged);
                continue;
            }

            var existing = existingForMatch.FirstOrDefault(
                s => s.TeamId == teamInternalId && string.Equals(s.Name, dto.Name, StringComparison.Ordinal));

            if (existing is not null)
            {
                existing.Value = dto.Value;
                existing.UpdatedAtUtc = _clock.UtcNow;
                _statistics.Update(existing);
                pending[(teamInternalId, dto.Name)] = existing;
                continue;
            }

            var statistic = new MatchStatistic
            {
                Id = Guid.NewGuid(),
                MatchId = matchId,
                TeamId = teamInternalId,
                Name = dto.Name,
                Value = dto.Value,
                CreatedAtUtc = _clock.UtcNow,
            };

            await _statistics.AddAsync(statistic, ct).ConfigureAwait(false);
            pending[(teamInternalId, dto.Name)] = statistic;
            inserted++;
        }

        return inserted;
    }

    /// <summary>
    /// Inserts the provider events for the match, skipping any event identical to
    /// one already present (keyed by match, type, minute, team) so the timeline is
    /// never duplicated. Returns the count of newly inserted rows.
    /// </summary>
    private async Task<int> IngestEventRowsAsync(
        IStatisticsProvider provider,
        Guid providerId,
        Guid matchId,
        string providerCode,
        string matchExternalId,
        CancellationToken ct)
    {
        var dtos = await provider.GetEventsAsync(matchExternalId, ct).ConfigureAwait(false);
        if (dtos.Count == 0)
        {
            return 0;
        }

        var all = await _events.ListAsync(ct).ConfigureAwait(false);
        var seen = all
            .Where(e => e.MatchId == matchId)
            .Select(e => (e.Type, e.Minute, e.TeamId))
            .ToHashSet();

        var inserted = 0;
        foreach (var dto in dtos)
        {
            Guid? teamInternalId = null;
            if (!string.IsNullOrWhiteSpace(dto.TeamExternalId))
            {
                var teamId = await _maps
                    .ResolveInternalIdAsync(providerId, TeamEntityType, dto.TeamExternalId, ct)
                    .ConfigureAwait(false);
                if (teamId is not { } resolved)
                {
                    _logger.LogWarning(
                        "Team {TeamExternalId} is not mapped for provider {ProviderCode}; skipping event '{Type}' for match {MatchExternalId}.",
                        dto.TeamExternalId,
                        providerCode,
                        dto.Type,
                        matchExternalId);
                    continue;
                }

                teamInternalId = resolved;
            }

            Guid? playerInternalId = null;
            if (!string.IsNullOrWhiteSpace(dto.PlayerExternalId))
            {
                playerInternalId = await _maps
                    .ResolveInternalIdAsync(providerId, "Player", dto.PlayerExternalId, ct)
                    .ConfigureAwait(false);
            }

            var key = (dto.Type, dto.Minute, teamInternalId);
            if (!seen.Add(key))
            {
                // Identical event already present (committed or staged this run).
                continue;
            }

            var matchEvent = new MatchEvent
            {
                Id = Guid.NewGuid(),
                MatchId = matchId,
                Type = dto.Type,
                Minute = dto.Minute,
                TeamId = teamInternalId,
                PlayerId = playerInternalId,
                Detail = dto.Detail,
                CreatedAtUtc = _clock.UtcNow,
            };

            await _events.AddAsync(matchEvent, ct).ConfigureAwait(false);
            inserted++;
        }

        return inserted;
    }

    private async Task<Provider?> ResolveProviderAsync(string providerCode, CancellationToken ct)
    {
        var existing = await _providers.ListAsync(ct).ConfigureAwait(false);
        return existing.FirstOrDefault(
            p => string.Equals(p.Code, providerCode, StringComparison.OrdinalIgnoreCase));
    }
}
