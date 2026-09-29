using CalcioAnalytic.Application.Abstractions.Clock;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Contracts.Providers;
using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Ingestion.Providers;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Ingests provider match fixtures into canonical <see cref="Match"/> aggregates.
/// </summary>
/// <remarks>
/// Idempotency is guaranteed by the <see cref="IProviderEntityMapRepository"/>:
/// a fixture is keyed by (provider id, "Match", external id). When a mapping
/// exists the canonical match is updated in place; otherwise it is created and a
/// new mapping is recorded. Referenced competition, season and teams are
/// resolved-or-created on demand via <see cref="CatalogUpsertHelper"/> so foreign
/// keys are always valid, even if the catalog has not been ingested separately.
/// </remarks>
public sealed class MatchIngestionService : IMatchIngestionService
{
    private const string MatchEntityType = "Match";

    private readonly IProviderRegistry _registry;
    private readonly IProviderEntityMapRepository _maps;
    private readonly IRepository<Provider> _providers;
    private readonly IRepository<Country> _countries;
    private readonly IRepository<Competition> _competitions;
    private readonly IRepository<Season> _seasons;
    private readonly IRepository<Team> _teams;
    private readonly IMatchRepository _matches;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<MatchIngestionService> _logger;

    /// <summary>Initializes a new instance of the <see cref="MatchIngestionService"/> class.</summary>
    public MatchIngestionService(
        IProviderRegistry registry,
        IProviderEntityMapRepository maps,
        IRepository<Provider> providers,
        IRepository<Country> countries,
        IRepository<Competition> competitions,
        IRepository<Season> seasons,
        IRepository<Team> teams,
        IMatchRepository matches,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<MatchIngestionService> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(countries);
        ArgumentNullException.ThrowIfNull(competitions);
        ArgumentNullException.ThrowIfNull(seasons);
        ArgumentNullException.ThrowIfNull(teams);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _maps = maps;
        _providers = providers;
        _countries = countries;
        _competitions = competitions;
        _seasons = seasons;
        _teams = teams;
        _matches = matches;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MatchIngestionResult> IngestFixturesAsync(
        string providerCode,
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(competitionExternalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seasonExternalId);

        var fixtures = _registry.Get<IFixtureProvider>(providerCode);
        var provider = await EnsureProviderAsync(providerCode, ct).ConfigureAwait(false);
        var helper = BuildHelper(provider.Id);

        var dtos = await fixtures.GetFixturesAsync(competitionExternalId, seasonExternalId, ct).ConfigureAwait(false);

        var ids = new List<Guid>();
        foreach (var dto in dtos)
        {
            var id = await UpsertMatchAsync(provider.Id, helper, dto, ct).ConfigureAwait(false);
            ids.Add(id);
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Ingested {Count} fixtures for provider {ProviderCode} ({Competition}/{Season}).",
            ids.Count,
            providerCode,
            competitionExternalId,
            seasonExternalId);

        return new MatchIngestionResult(ids.Count, ids);
    }

    /// <inheritdoc />
    public async Task<Guid?> IngestFixtureAsync(
        string providerCode,
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);

        var fixtures = _registry.Get<IFixtureProvider>(providerCode);
        var dto = await fixtures.GetFixtureAsync(matchExternalId, ct).ConfigureAwait(false);
        if (dto is null)
        {
            _logger.LogWarning(
                "Provider {ProviderCode} returned no fixture for external id {MatchExternalId}.",
                providerCode,
                matchExternalId);
            return null;
        }

        var provider = await EnsureProviderAsync(providerCode, ct).ConfigureAwait(false);
        var helper = BuildHelper(provider.Id);

        var id = await UpsertMatchAsync(provider.Id, helper, dto, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return id;
    }

    private CatalogUpsertHelper BuildHelper(Guid providerId)
        => new(_maps, _countries, _competitions, _seasons, _teams, providerId, _clock);

    private async Task<Guid> UpsertMatchAsync(
        Guid providerId,
        CatalogUpsertHelper helper,
        ProviderMatchDto dto,
        CancellationToken ct)
    {
        // Resolve-or-create the referenced catalog entities so FKs are valid.
        var competitionId = await ResolveCompetitionAsync(providerId, helper, dto.CompetitionExternalId, ct)
            .ConfigureAwait(false);

        var seasonId = await ResolveSeasonAsync(providerId, helper, dto, ct).ConfigureAwait(false);

        var homeTeamId = await ResolveTeamAsync(providerId, helper, dto.HomeTeamExternalId, ct).ConfigureAwait(false);
        var awayTeamId = await ResolveTeamAsync(providerId, helper, dto.AwayTeamExternalId, ct).ConfigureAwait(false);

        var status = MapStatus(dto.Status);
        var kickoffUtc = dto.KickoffUtc.UtcDateTime;

        var internalId = await _maps
            .ResolveInternalIdAsync(providerId, MatchEntityType, dto.ExternalId, ct)
            .ConfigureAwait(false);

        if (internalId is { } existingId)
        {
            var existing = await _matches.GetByIdAsync(existingId, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                existing.CompetitionId = competitionId;
                existing.SeasonId = seasonId;
                existing.HomeTeamId = homeTeamId;
                existing.AwayTeamId = awayTeamId;
                existing.KickoffUtc = kickoffUtc;
                existing.Venue = dto.Venue ?? existing.Venue;
                existing.Round = dto.Round ?? existing.Round;
                existing.Referee = dto.Referee ?? existing.Referee;
                existing.Status = status;
                existing.HomeScore = dto.HomeScore ?? existing.HomeScore;
                existing.AwayScore = dto.AwayScore ?? existing.AwayScore;
                existing.HomeScoreHalfTime = dto.HomeScoreHalfTime ?? existing.HomeScoreHalfTime;
                existing.AwayScoreHalfTime = dto.AwayScoreHalfTime ?? existing.AwayScoreHalfTime;
                existing.UpdatedAtUtc = _clock.UtcNow;
                _matches.Update(existing);
                return existing.Id;
            }
        }

        var match = new Match
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            SeasonId = seasonId,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            KickoffUtc = kickoffUtc,
            Venue = dto.Venue,
            Round = dto.Round,
            Referee = dto.Referee,
            Status = status,
            HomeScore = dto.HomeScore,
            AwayScore = dto.AwayScore,
            HomeScoreHalfTime = dto.HomeScoreHalfTime,
            AwayScoreHalfTime = dto.AwayScoreHalfTime,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _matches.AddAsync(match, ct).ConfigureAwait(false);
        await _maps.AddAsync(
            CatalogUpsertHelper.NewMap(providerId, MatchEntityType, match.Id, dto.ExternalId, _clock),
            ct).ConfigureAwait(false);
        return match.Id;
    }

    // These delegate to the helper's upsert methods, which are idempotent via
    // both the provider entity map (committed rows) and an in-run pending cache
    // (staged-but-uncommitted rows), so no duplicates are created within a run.
    private static Task<Guid> ResolveCompetitionAsync(
        Guid providerId,
        CatalogUpsertHelper helper,
        string competitionExternalId,
        CancellationToken ct)
        => helper.UpsertCompetitionAsync(
            new ProviderCompetitionDto(competitionExternalId, competitionExternalId, null, null), ct);

    private static Task<Guid> ResolveSeasonAsync(
        Guid providerId,
        CatalogUpsertHelper helper,
        ProviderMatchDto dto,
        CancellationToken ct)
    {
        var seasonExternalId = dto.SeasonExternalId ?? $"{dto.CompetitionExternalId}-default";
        return helper.UpsertSeasonAsync(
            new ProviderSeasonDto(seasonExternalId, dto.CompetitionExternalId, seasonExternalId, null, null), ct);
    }

    private static Task<Guid> ResolveTeamAsync(
        Guid providerId,
        CatalogUpsertHelper helper,
        string teamExternalId,
        CancellationToken ct)
        => helper.UpsertTeamAsync(
            new ProviderTeamDto(teamExternalId, teamExternalId, null, null, null), ct);

    private async Task<Provider> EnsureProviderAsync(string providerCode, CancellationToken ct)
    {
        var existing = await _providers.ListAsync(ct).ConfigureAwait(false);
        var provider = existing.FirstOrDefault(
            p => string.Equals(p.Code, providerCode, StringComparison.OrdinalIgnoreCase));
        if (provider is not null)
        {
            return provider;
        }

        provider = new Provider
        {
            Id = Guid.NewGuid(),
            Name = providerCode,
            Code = providerCode,
            IsEnabled = true,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _providers.AddAsync(provider, ct).ConfigureAwait(false);
        return provider;
    }

    /// <summary>Maps a provider status string to the canonical <see cref="MatchStatus"/>.</summary>
    private static MatchStatus MapStatus(string? status)
        => status?.Trim().ToLowerInvariant() switch
        {
            "scheduled" => MatchStatus.Scheduled,
            "prematch" or "pre-match" or "pre_match" => MatchStatus.PreMatch,
            "live" or "inplay" or "in-play" or "in_play" => MatchStatus.Live,
            "finished" or "ft" or "fulltime" or "full-time" or "ended" => MatchStatus.Finished,
            "settlementpending" or "settlement-pending" => MatchStatus.SettlementPending,
            "analyzed" => MatchStatus.Analyzed,
            "reconciled" => MatchStatus.Reconciled,
            _ => MatchStatus.Scheduled,
        };
}
