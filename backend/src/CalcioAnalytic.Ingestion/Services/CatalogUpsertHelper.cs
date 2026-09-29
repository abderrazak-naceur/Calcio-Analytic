using CalcioAnalytic.Application.Abstractions.Clock;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Contracts.Providers;
using CalcioAnalytic.Domain.Catalog;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Shared idempotent upsert logic for catalog entities (countries, competitions,
/// seasons and teams). Keyed reconciliation uses the
/// <see cref="IProviderEntityMapRepository"/> so that re-ingesting the same
/// provider data updates existing rows instead of creating duplicates.
/// </summary>
/// <remarks>
/// The helper only stages entities and mappings on the injected repositories; it
/// does not commit. Callers are responsible for calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> once the batch is complete.
/// </remarks>
internal sealed class CatalogUpsertHelper
{
    /// <summary>Entity type discriminator for competition mappings.</summary>
    public const string CompetitionEntityType = "Competition";

    /// <summary>Entity type discriminator for season mappings.</summary>
    public const string SeasonEntityType = "Season";

    /// <summary>Entity type discriminator for team mappings.</summary>
    public const string TeamEntityType = "Team";

    private readonly IProviderEntityMapRepository _maps;
    private readonly IRepository<Country> _countries;
    private readonly IRepository<Competition> _competitions;
    private readonly IRepository<Season> _seasons;
    private readonly IRepository<Team> _teams;
    private readonly Guid _providerId;
    private readonly IClock _clock;

    // In-run cache of (entityType, externalId) -> internal id. The provider entity
    // map repository only sees committed rows, but within a single ingestion run
    // several entities are staged before SaveChanges. This cache lets repeated
    // resolves in the same run reuse the just-created entity instead of creating
    // a duplicate.
    private readonly Dictionary<(string EntityType, string ExternalId), Guid> _pending = new();

    public CatalogUpsertHelper(
        IProviderEntityMapRepository maps,
        IRepository<Country> countries,
        IRepository<Competition> competitions,
        IRepository<Season> seasons,
        IRepository<Team> teams,
        Guid providerId,
        IClock clock)
    {
        _maps = maps;
        _countries = countries;
        _competitions = competitions;
        _seasons = seasons;
        _teams = teams;
        _providerId = providerId;
        _clock = clock;
    }

    /// <summary>Creates a new provider entity mapping row.</summary>
    public static ProviderEntityMap NewMap(
        Guid providerId,
        string entityType,
        Guid internalId,
        string externalId,
        IClock clock)
        => new()
        {
            Id = Guid.NewGuid(),
            ProviderId = providerId,
            EntityType = entityType,
            InternalId = internalId,
            ExternalId = externalId,
            CreatedAtUtc = clock.UtcNow,
        };

    /// <summary>
    /// Resolves the internal id for a provider external id, consulting the in-run
    /// pending cache first (for entities staged but not yet committed), then the
    /// mapping repository.
    /// </summary>
    private async Task<Guid?> ResolveAsync(string entityType, string externalId, CancellationToken ct)
    {
        if (_pending.TryGetValue((entityType, externalId), out var cached))
        {
            return cached;
        }

        return await _maps
            .ResolveInternalIdAsync(_providerId, entityType, externalId, ct)
            .ConfigureAwait(false);
    }

    private void RememberPending(string entityType, string externalId, Guid internalId)
        => _pending[(entityType, externalId)] = internalId;

    /// <summary>Upserts a competition and returns its canonical id.</summary>
    public async Task<Guid> UpsertCompetitionAsync(ProviderCompetitionDto dto, CancellationToken ct)
    {
        var countryId = await ResolveOrCreateCountryAsync(dto.CountryName, ct).ConfigureAwait(false);

        var internalId = await ResolveAsync(CompetitionEntityType, dto.ExternalId, ct).ConfigureAwait(false);

        if (internalId is { } id)
        {
            var existing = await _competitions.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                existing.Name = dto.Name;
                existing.Tier = ParseTier(dto.Tier) ?? existing.Tier;
                if (countryId != Guid.Empty)
                {
                    existing.CountryId = countryId;
                }

                existing.UpdatedAtUtc = _clock.UtcNow;
                _competitions.Update(existing);
                return existing.Id;
            }
        }

        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            CountryId = countryId,
            Tier = ParseTier(dto.Tier),
            CreatedAtUtc = _clock.UtcNow,
        };

        await _competitions.AddAsync(competition, ct).ConfigureAwait(false);
        await _maps.AddAsync(NewMap(_providerId, CompetitionEntityType, competition.Id, dto.ExternalId, _clock), ct)
            .ConfigureAwait(false);
        RememberPending(CompetitionEntityType, dto.ExternalId, competition.Id);
        return competition.Id;
    }

    /// <summary>Upserts a season, resolving its competition on demand, and returns its canonical id.</summary>
    public async Task<Guid> UpsertSeasonAsync(ProviderSeasonDto dto, CancellationToken ct)
    {
        var competitionId = await ResolveAsync(CompetitionEntityType, dto.CompetitionExternalId, ct).ConfigureAwait(false)
            ?? await UpsertCompetitionAsync(
                    new ProviderCompetitionDto(dto.CompetitionExternalId, dto.CompetitionExternalId, null, null),
                    ct)
                .ConfigureAwait(false);

        var internalId = await ResolveAsync(SeasonEntityType, dto.ExternalId, ct).ConfigureAwait(false);

        if (internalId is { } id)
        {
            var existing = await _seasons.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                existing.Label = dto.Label;
                existing.StartDate = dto.StartDate ?? existing.StartDate;
                existing.EndDate = dto.EndDate ?? existing.EndDate;
                existing.CompetitionId = competitionId;
                existing.UpdatedAtUtc = _clock.UtcNow;
                _seasons.Update(existing);
                return existing.Id;
            }
        }

        var season = new Season
        {
            Id = Guid.NewGuid(),
            CompetitionId = competitionId,
            Label = dto.Label,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _seasons.AddAsync(season, ct).ConfigureAwait(false);
        await _maps.AddAsync(NewMap(_providerId, SeasonEntityType, season.Id, dto.ExternalId, _clock), ct)
            .ConfigureAwait(false);
        RememberPending(SeasonEntityType, dto.ExternalId, season.Id);
        return season.Id;
    }

    /// <summary>Upserts a team and returns its canonical id.</summary>
    public async Task<Guid> UpsertTeamAsync(ProviderTeamDto dto, CancellationToken ct)
    {
        var countryId = await ResolveOrCreateCountryAsync(dto.CountryName, ct).ConfigureAwait(false);

        var internalId = await ResolveAsync(TeamEntityType, dto.ExternalId, ct).ConfigureAwait(false);

        if (internalId is { } id)
        {
            var existing = await _teams.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                existing.Name = dto.Name;
                existing.ShortName = dto.ShortName ?? existing.ShortName;
                if (countryId != Guid.Empty)
                {
                    existing.CountryId = countryId;
                }

                existing.UpdatedAtUtc = _clock.UtcNow;
                _teams.Update(existing);
                return existing.Id;
            }
        }

        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ShortName = dto.ShortName,
            CountryId = countryId == Guid.Empty ? null : countryId,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _teams.AddAsync(team, ct).ConfigureAwait(false);
        await _maps.AddAsync(NewMap(_providerId, TeamEntityType, team.Id, dto.ExternalId, _clock), ct)
            .ConfigureAwait(false);
        RememberPending(TeamEntityType, dto.ExternalId, team.Id);
        return team.Id;
    }

    /// <summary>
    /// Parses a provider tier string (e.g. "1", "Tier 1") into an integer level,
    /// returning null when no numeric value can be extracted.
    /// </summary>
    private static int? ParseTier(string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier))
        {
            return null;
        }

        if (int.TryParse(tier, out var value))
        {
            return value;
        }

        var digits = new string(tier.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Resolves a country by name, creating it if needed. Returns
    /// <see cref="Guid.Empty"/> when no country name is supplied. Country is not
    /// provider-keyed (names are shared across providers), so it is matched by name.
    /// </summary>
    private async Task<Guid> ResolveOrCreateCountryAsync(string? countryName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(countryName))
        {
            return Guid.Empty;
        }

        var all = await _countries.ListAsync(ct).ConfigureAwait(false);
        var existing = all.FirstOrDefault(
            c => string.Equals(c.Name, countryName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing.Id;
        }

        var country = new Country
        {
            Id = Guid.NewGuid(),
            Name = countryName,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _countries.AddAsync(country, ct).ConfigureAwait(false);
        return country.Id;
    }
}
