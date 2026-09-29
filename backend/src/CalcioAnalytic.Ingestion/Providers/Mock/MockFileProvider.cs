using CalcioAnalytic.Contracts.Providers;

namespace CalcioAnalytic.Ingestion.Providers.Mock;

/// <summary>
/// A deterministic, file/embedded-resource backed provider adapter used for local
/// development, tests and end-to-end demos. It exposes a single complete vertical
/// slice of realistic sample data (one competition, one season, two teams, one
/// finished match, statistics, events and a sequence of moving odds snapshots)
/// so the whole ingestion → settlement → analysis pipeline can be exercised
/// without a real vendor.
/// </summary>
/// <remarks>
/// The adapter implements the football catalog, fixture, odds and statistics
/// capabilities. All data is read (once) via <see cref="MockProviderData"/> from
/// JSON embedded in this assembly, or from a folder supplied to the constructor.
/// </remarks>
public sealed class MockFileProvider :
    IFootballProvider,
    IFixtureProvider,
    IOddsProvider,
    IStatisticsProvider
{
    /// <summary>The stable provider code for this adapter.</summary>
    public const string Code = "mock";

    private readonly MockProviderData _data;

    /// <summary>
    /// Initializes a new instance backed by the embedded sample data.
    /// </summary>
    public MockFileProvider()
        : this(MockProviderData.Embedded)
    {
    }

    /// <summary>
    /// Initializes a new instance backed by the supplied sample data set.
    /// </summary>
    /// <param name="data">The loaded sample data.</param>
    public MockFileProvider(MockProviderData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data;
    }

    /// <inheritdoc />
    public string ProviderCode => Code;

    // ---- IFootballProvider -------------------------------------------------

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderCompetitionDto>> GetCompetitionsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_data.Competitions);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderTeamDto>> GetTeamsAsync(
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitionExternalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seasonExternalId);
        ct.ThrowIfCancellationRequested();

        // Teams are those that appear in a match of the requested competition + season.
        var teamIds = _data.Matches
            .Where(m => m.CompetitionExternalId == competitionExternalId
                && (m.SeasonExternalId == seasonExternalId || m.SeasonExternalId is null))
            .SelectMany(m => new[] { m.HomeTeamExternalId, m.AwayTeamExternalId })
            .ToHashSet(StringComparer.Ordinal);

        IReadOnlyList<ProviderTeamDto> teams = _data.Teams
            .Where(t => teamIds.Contains(t.ExternalId))
            .ToArray();

        return Task.FromResult(teams);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderSeasonDto>> GetSeasonsAsync(
        string competitionExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitionExternalId);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<ProviderSeasonDto> seasons = _data.Seasons
            .Where(s => s.CompetitionExternalId == competitionExternalId)
            .ToArray();

        return Task.FromResult(seasons);
    }

    // ---- IFixtureProvider --------------------------------------------------

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderMatchDto>> GetFixturesAsync(
        string competitionExternalId,
        string seasonExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(competitionExternalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seasonExternalId);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<ProviderMatchDto> fixtures = _data.Matches
            .Where(m => m.CompetitionExternalId == competitionExternalId
                && (m.SeasonExternalId == seasonExternalId || m.SeasonExternalId is null))
            .ToArray();

        return Task.FromResult(fixtures);
    }

    /// <inheritdoc />
    public Task<ProviderMatchDto?> GetFixtureAsync(
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);
        ct.ThrowIfCancellationRequested();

        var match = _data.Matches.FirstOrDefault(m => m.ExternalId == matchExternalId);
        return Task.FromResult(match);
    }

    // ---- IOddsProvider -----------------------------------------------------

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderOddsSnapshotDto>> GetOddsAsync(
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);
        ct.ThrowIfCancellationRequested();

        // Ordered chronologically so downstream movement analysis is stable.
        IReadOnlyList<ProviderOddsSnapshotDto> snapshots = _data.OddsSnapshots
            .Where(o => o.MatchExternalId == matchExternalId)
            .OrderBy(o => o.BookmakerTimestamp)
            .ThenBy(o => o.BookmakerExternalId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(snapshots);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderBookmakerDto>> GetBookmakersAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_data.Bookmakers);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderMarketDto>> GetMarketsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_data.Markets);
    }

    // ---- IStatisticsProvider -----------------------------------------------

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderStatisticDto>> GetStatisticsAsync(
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<ProviderStatisticDto> stats = _data.Statistics
            .Where(s => s.MatchExternalId == matchExternalId)
            .ToArray();

        return Task.FromResult(stats);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProviderEventDto>> GetEventsAsync(
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<ProviderEventDto> events = _data.Events
            .Where(e => e.MatchExternalId == matchExternalId)
            .OrderBy(e => e.Minute ?? int.MaxValue)
            .ToArray();

        return Task.FromResult(events);
    }
}
