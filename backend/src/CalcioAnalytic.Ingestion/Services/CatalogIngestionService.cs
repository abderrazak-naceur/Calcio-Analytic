using CalcioAnalytic.Application.Abstractions.Clock;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Contracts.Providers;
using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Ingestion.Providers;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Ingests provider catalog data (competitions, seasons, teams, bookmakers and
/// markets) into canonical domain entities.
/// </summary>
/// <remarks>
/// <para>
/// Idempotency is guaranteed by the <see cref="IProviderEntityMapRepository"/>:
/// every provider entity is keyed by (provider id, entity type, external id).
/// Before creating a domain entity the service resolves that key; when a mapping
/// already exists it loads and updates the existing row, otherwise it creates a
/// new row and records the mapping. Re-running the same source data therefore
/// never produces duplicates.
/// </para>
/// </remarks>
public sealed class CatalogIngestionService : ICatalogIngestionService
{
    private readonly IProviderRegistry _registry;
    private readonly IProviderEntityMapRepository _maps;
    private readonly IRepository<Provider> _providers;
    private readonly IRepository<Country> _countries;
    private readonly IRepository<Competition> _competitions;
    private readonly IRepository<Season> _seasons;
    private readonly IRepository<Team> _teams;
    private readonly IRepository<Bookmaker> _bookmakers;
    private readonly IRepository<Market> _markets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<CatalogIngestionService> _logger;

    /// <summary>Initializes a new instance of the <see cref="CatalogIngestionService"/> class.</summary>
    public CatalogIngestionService(
        IProviderRegistry registry,
        IProviderEntityMapRepository maps,
        IRepository<Provider> providers,
        IRepository<Country> countries,
        IRepository<Competition> competitions,
        IRepository<Season> seasons,
        IRepository<Team> teams,
        IRepository<Bookmaker> bookmakers,
        IRepository<Market> markets,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<CatalogIngestionService> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(countries);
        ArgumentNullException.ThrowIfNull(competitions);
        ArgumentNullException.ThrowIfNull(seasons);
        ArgumentNullException.ThrowIfNull(teams);
        ArgumentNullException.ThrowIfNull(bookmakers);
        ArgumentNullException.ThrowIfNull(markets);
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
        _bookmakers = bookmakers;
        _markets = markets;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CatalogIngestionResult> IngestCatalogAsync(
        string providerCode,
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(competitionExternalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seasonExternalId);

        _logger.LogInformation(
            "Starting catalog ingestion for provider {ProviderCode}, competition {CompetitionExternalId}, season {SeasonExternalId}.",
            providerCode,
            competitionExternalId,
            seasonExternalId);

        var football = _registry.Get<IFootballProvider>(providerCode);
        var provider = await EnsureProviderAsync(providerCode, ct).ConfigureAwait(false);

        var helper = new CatalogUpsertHelper(_maps, _countries, _competitions, _seasons, _teams, provider.Id, _clock);

        // Competitions.
        var competitionsUpserted = 0;
        var competitionDtos = await football.GetCompetitionsAsync(ct).ConfigureAwait(false);
        foreach (var dto in competitionDtos)
        {
            await helper.UpsertCompetitionAsync(dto, ct).ConfigureAwait(false);
            competitionsUpserted++;
        }

        // Seasons for the requested competition.
        var seasonsUpserted = 0;
        var seasonDtos = await football.GetSeasonsAsync(competitionExternalId, ct).ConfigureAwait(false);
        foreach (var dto in seasonDtos)
        {
            await helper.UpsertSeasonAsync(dto, ct).ConfigureAwait(false);
            seasonsUpserted++;
        }

        // Teams participating in the competition + season.
        var teamsUpserted = 0;
        var teamDtos = await football.GetTeamsAsync(competitionExternalId, seasonExternalId, ct).ConfigureAwait(false);
        foreach (var dto in teamDtos)
        {
            await helper.UpsertTeamAsync(dto, ct).ConfigureAwait(false);
            teamsUpserted++;
        }

        // Bookmakers and markets (via the odds capability, when the provider offers it).
        var bookmakersUpserted = 0;
        var marketsUpserted = 0;
        if (_registry.TryGet<IOddsProvider>(providerCode, out var odds) && odds is not null)
        {
            var bookmakerDtos = await odds.GetBookmakersAsync(ct).ConfigureAwait(false);
            foreach (var dto in bookmakerDtos)
            {
                await UpsertBookmakerAsync(provider.Id, dto, ct).ConfigureAwait(false);
                bookmakersUpserted++;
            }

            var marketDtos = await odds.GetMarketsAsync(ct).ConfigureAwait(false);
            foreach (var dto in marketDtos)
            {
                await UpsertMarketAsync(provider.Id, dto, ct).ConfigureAwait(false);
                marketsUpserted++;
            }
        }
        else
        {
            _logger.LogDebug(
                "Provider {ProviderCode} does not expose an odds capability; skipping bookmakers and markets.",
                providerCode);
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Completed catalog ingestion for provider {ProviderCode}: {Competitions} competitions, {Seasons} seasons, {Teams} teams, {Bookmakers} bookmakers, {Markets} markets.",
            providerCode,
            competitionsUpserted,
            seasonsUpserted,
            teamsUpserted,
            bookmakersUpserted,
            marketsUpserted);

        return new CatalogIngestionResult(
            competitionsUpserted,
            seasonsUpserted,
            teamsUpserted,
            bookmakersUpserted,
            marketsUpserted);
    }

    /// <summary>
    /// Resolves the <see cref="Provider"/> row for the given code, creating it
    /// when it does not yet exist. The provider owns all entity mappings, so it
    /// must exist before any mapping is recorded.
    /// </summary>
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
        _logger.LogInformation("Created provider row for code {ProviderCode} (id {ProviderId}).", providerCode, provider.Id);
        return provider;
    }

    private async Task UpsertBookmakerAsync(Guid providerId, ProviderBookmakerDto dto, CancellationToken ct)
    {
        const string entityType = "Bookmaker";
        var internalId = await _maps.ResolveInternalIdAsync(providerId, entityType, dto.ExternalId, ct).ConfigureAwait(false);
        if (internalId is { } id)
        {
            var existing = await _bookmakers.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                existing.Name = dto.Name;
                existing.Code = dto.Code ?? existing.Code;
                existing.UpdatedAtUtc = _clock.UtcNow;
                _bookmakers.Update(existing);
                return;
            }
        }

        var bookmaker = new Bookmaker
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code ?? dto.ExternalId,
            IsEnabled = true,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _bookmakers.AddAsync(bookmaker, ct).ConfigureAwait(false);
        await _maps.AddAsync(CatalogUpsertHelper.NewMap(providerId, entityType, bookmaker.Id, dto.ExternalId, _clock), ct)
            .ConfigureAwait(false);
    }

    private async Task UpsertMarketAsync(Guid providerId, ProviderMarketDto dto, CancellationToken ct)
    {
        const string entityType = "Market";
        var internalId = await _maps.ResolveInternalIdAsync(providerId, entityType, dto.ExternalId, ct).ConfigureAwait(false);
        if (internalId is { } id)
        {
            var existing = await _markets.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                existing.Name = dto.Name;
                existing.Code = dto.Code ?? existing.Code;
                existing.Description = dto.Description ?? existing.Description;
                existing.UpdatedAtUtc = _clock.UtcNow;
                _markets.Update(existing);
                return;
            }
        }

        var market = new Market
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code ?? dto.ExternalId,
            Description = dto.Description,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _markets.AddAsync(market, ct).ConfigureAwait(false);
        await _maps.AddAsync(CatalogUpsertHelper.NewMap(providerId, entityType, market.Id, dto.ExternalId, _clock), ct)
            .ConfigureAwait(false);
    }
}
