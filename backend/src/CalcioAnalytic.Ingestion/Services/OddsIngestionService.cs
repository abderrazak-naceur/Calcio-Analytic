using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CalcioAnalytic.Application.Abstractions.Clock;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Contracts.Providers;
using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Odds;
using CalcioAnalytic.Ingestion.Providers;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Ingests provider odds snapshots for a single match into canonical
/// <see cref="MarketLine"/>, <see cref="Selection"/> and append-only
/// <see cref="OddsSnapshot"/> records.
/// </summary>
/// <remarks>
/// <para>
/// Snapshots are historical and never overwritten. Idempotency is enforced by
/// the snapshot <see cref="OddsSnapshot.PayloadHash"/>: before appending a
/// snapshot the service checks both the already-committed hashes (loaded once at
/// the start of the run) and an in-run set of hashes staged during the same run.
/// A snapshot whose hash is already known is skipped and counted as a duplicate,
/// so running the same source data twice never double-inserts.
/// </para>
/// <para>
/// Market lines and selections are reconciled by their natural keys
/// ((match, market, line, period) and (market line, name)); an existing row is
/// reused and only created when missing. Bookmaker, market and match internal
/// identifiers are resolved through the <see cref="IProviderEntityMapRepository"/>;
/// when a referenced bookmaker or market is not mapped the snapshot is skipped
/// with a warning rather than inventing a mapping.
/// </para>
/// </remarks>
public sealed class OddsIngestionService : IOddsIngestionService
{
    private const string MatchEntityType = "Match";
    private const string BookmakerEntityType = "Bookmaker";
    private const string MarketEntityType = "Market";

    private readonly IProviderRegistry _registry;
    private readonly IProviderEntityMapRepository _maps;
    private readonly IRepository<Provider> _providers;
    private readonly IRepository<MarketLine> _marketLines;
    private readonly IRepository<Selection> _selections;
    private readonly IRepository<OddsSnapshot> _snapshots;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<OddsIngestionService> _logger;

    /// <summary>Initializes a new instance of the <see cref="OddsIngestionService"/> class.</summary>
    public OddsIngestionService(
        IProviderRegistry registry,
        IProviderEntityMapRepository maps,
        IRepository<Provider> providers,
        IRepository<MarketLine> marketLines,
        IRepository<Selection> selections,
        IRepository<OddsSnapshot> snapshots,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<OddsIngestionService> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(marketLines);
        ArgumentNullException.ThrowIfNull(selections);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _maps = maps;
        _providers = providers;
        _marketLines = marketLines;
        _selections = selections;
        _snapshots = snapshots;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OddsIngestionResult> IngestOddsAsync(
        string providerCode,
        string matchExternalId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExternalId);

        _logger.LogInformation(
            "Starting odds ingestion for provider {ProviderCode}, match {MatchExternalId}.",
            providerCode,
            matchExternalId);

        var odds = _registry.Get<IOddsProvider>(providerCode);
        var provider = await ResolveProviderAsync(providerCode, ct).ConfigureAwait(false);
        if (provider is null)
        {
            _logger.LogWarning(
                "Provider {ProviderCode} is not registered in the catalog; no odds ingested for match {MatchExternalId}.",
                providerCode,
                matchExternalId);
            return new OddsIngestionResult(0, 0, 0, 0);
        }

        var matchId = await _maps
            .ResolveInternalIdAsync(provider.Id, MatchEntityType, matchExternalId, ct)
            .ConfigureAwait(false);
        if (matchId is not { } matchInternalId)
        {
            _logger.LogWarning(
                "Match {MatchExternalId} is not mapped for provider {ProviderCode}; no odds ingested.",
                matchExternalId,
                providerCode);
            return new OddsIngestionResult(0, 0, 0, 0);
        }

        var snapshots = await odds.GetOddsAsync(matchExternalId, ct).ConfigureAwait(false);

        // Load the payload hashes already committed for this match so re-runs are
        // idempotent, and keep an in-run set for hashes staged during this run.
        var knownHashes = await LoadExistingHashesAsync(matchInternalId, ct).ConfigureAwait(false);

        // In-run reconciliation caches for market lines and selections so a line or
        // selection referenced repeatedly within one run is created only once.
        var marketLineCache = new Dictionary<(Guid MarketId, decimal? Line, string? Period), Guid>();
        var selectionCache = new Dictionary<(Guid MarketLineId, string Name), Guid>();

        var snapshotsInserted = 0;
        var snapshotsSkipped = 0;
        var marketLinesUpserted = 0;
        var selectionsUpserted = 0;

        foreach (var snapshot in snapshots)
        {
            var bookmakerId = await _maps
                .ResolveInternalIdAsync(provider.Id, BookmakerEntityType, snapshot.BookmakerExternalId, ct)
                .ConfigureAwait(false);
            if (bookmakerId is not { } bookmakerInternalId)
            {
                _logger.LogWarning(
                    "Bookmaker {BookmakerExternalId} is not mapped for provider {ProviderCode}; skipping snapshot for match {MatchExternalId}.",
                    snapshot.BookmakerExternalId,
                    providerCode,
                    matchExternalId);
                continue;
            }

            foreach (var line in snapshot.Markets)
            {
                var marketId = await _maps
                    .ResolveInternalIdAsync(provider.Id, MarketEntityType, line.MarketExternalId, ct)
                    .ConfigureAwait(false);
                if (marketId is not { } marketInternalId)
                {
                    _logger.LogWarning(
                        "Market {MarketExternalId} is not mapped for provider {ProviderCode}; skipping market line for match {MatchExternalId}.",
                        line.MarketExternalId,
                        providerCode,
                        matchExternalId);
                    continue;
                }

                var (marketLineId, marketLineCreated) = await ResolveMarketLineAsync(
                    marketLineCache, matchInternalId, marketInternalId, line, ct).ConfigureAwait(false);
                if (marketLineCreated)
                {
                    marketLinesUpserted++;
                }

                foreach (var selection in line.Selections)
                {
                    var (selectionId, selectionCreated) = await ResolveSelectionAsync(
                        selectionCache, marketLineId, selection.Name, ct).ConfigureAwait(false);
                    if (selectionCreated)
                    {
                        selectionsUpserted++;
                    }

                    var bookmakerTimestampUtc = snapshot.BookmakerTimestamp.UtcDateTime;
                    var providerTimestampUtc = snapshot.ProviderTimestamp.UtcDateTime;

                    var hash = snapshot.RawPayloadHash ?? ComputeSelectionHash(
                        snapshot.BookmakerExternalId,
                        snapshot.MatchExternalId,
                        line,
                        selection,
                        providerTimestampUtc);

                    // Dedup: skip any snapshot whose payload hash is already known,
                    // whether committed before this run or staged during it.
                    if (!knownHashes.Add(hash))
                    {
                        snapshotsSkipped++;
                        continue;
                    }

                    var record = new OddsSnapshot
                    {
                        Id = Guid.NewGuid(),
                        MatchId = matchInternalId,
                        BookmakerId = bookmakerInternalId,
                        MarketLineId = marketLineId,
                        SelectionId = selectionId,
                        DecimalOdds = selection.DecimalOdds,
                        FractionalOdds = selection.FractionalOdds,
                        AmericanOdds = selection.AmericanOdds,
                        ImpliedProbability = selection.DecimalOdds > 1m ? 1m / selection.DecimalOdds : 0m,
                        IsLive = snapshot.IsLive,
                        MatchMinute = snapshot.MatchMinute,
                        Period = line.Period,
                        Kind = snapshot.IsLive ? OddsSnapshotKind.InPlay : OddsSnapshotKind.Other,
                        BookmakerTimestampUtc = bookmakerTimestampUtc,
                        ProviderTimestampUtc = providerTimestampUtc,
                        IngestionTimestampUtc = _clock.UtcNow,
                        PayloadHash = hash,
                        CreatedAtUtc = _clock.UtcNow,
                    };

                    await _snapshots.AddAsync(record, ct).ConfigureAwait(false);
                    snapshotsInserted++;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Completed odds ingestion for provider {ProviderCode}, match {MatchExternalId}: {Inserted} inserted, {Skipped} duplicates skipped, {Lines} market lines, {Selections} selections.",
            providerCode,
            matchExternalId,
            snapshotsInserted,
            snapshotsSkipped,
            marketLinesUpserted,
            selectionsUpserted);

        return new OddsIngestionResult(snapshotsInserted, snapshotsSkipped, marketLinesUpserted, selectionsUpserted);
    }

    /// <summary>
    /// Loads the payload hashes already persisted for the match, seeding the
    /// in-run dedup set so previously ingested snapshots are recognised.
    /// </summary>
    private async Task<HashSet<string>> LoadExistingHashesAsync(Guid matchId, CancellationToken ct)
    {
        var all = await _snapshots.ListAsync(ct).ConfigureAwait(false);
        return all
            .Where(s => s.MatchId == matchId && !string.IsNullOrEmpty(s.PayloadHash))
            .Select(s => s.PayloadHash)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Resolves the market line for (match, market, line, period), reusing an
    /// existing row when present and creating one otherwise. Returns the line id
    /// and whether a new row was created during this run.
    /// </summary>
    private async Task<(Guid Id, bool Created)> ResolveMarketLineAsync(
        Dictionary<(Guid MarketId, decimal? Line, string? Period), Guid> cache,
        Guid matchId,
        Guid marketId,
        ProviderMarketLineDto line,
        CancellationToken ct)
    {
        var key = (marketId, line.Line, line.Period);
        if (cache.TryGetValue(key, out var cached))
        {
            return (cached, false);
        }

        var all = await _marketLines.ListAsync(ct).ConfigureAwait(false);
        var existing = all.FirstOrDefault(
            ml => ml.MatchId == matchId
                && ml.MarketId == marketId
                && ml.Line == line.Line
                && string.Equals(ml.Period, line.Period, StringComparison.Ordinal));

        if (existing is not null)
        {
            cache[key] = existing.Id;
            return (existing.Id, false);
        }

        var marketLine = new MarketLine
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            MarketId = marketId,
            Line = line.Line,
            Period = line.Period,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _marketLines.AddAsync(marketLine, ct).ConfigureAwait(false);
        cache[key] = marketLine.Id;
        return (marketLine.Id, true);
    }

    /// <summary>
    /// Resolves the selection for (market line, name), reusing an existing row
    /// when present and creating one otherwise. Returns the selection id and
    /// whether a new row was created during this run.
    /// </summary>
    private async Task<(Guid Id, bool Created)> ResolveSelectionAsync(
        Dictionary<(Guid MarketLineId, string Name), Guid> cache,
        Guid marketLineId,
        string name,
        CancellationToken ct)
    {
        var key = (marketLineId, name);
        if (cache.TryGetValue(key, out var cached))
        {
            return (cached, false);
        }

        var all = await _selections.ListAsync(ct).ConfigureAwait(false);
        var existing = all.FirstOrDefault(
            s => s.MarketLineId == marketLineId && string.Equals(s.Name, name, StringComparison.Ordinal));

        if (existing is not null)
        {
            cache[key] = existing.Id;
            return (existing.Id, false);
        }

        var selection = new Selection
        {
            Id = Guid.NewGuid(),
            MarketLineId = marketLineId,
            Name = name,
            CreatedAtUtc = _clock.UtcNow,
        };

        await _selections.AddAsync(selection, ct).ConfigureAwait(false);
        cache[key] = selection.Id;
        return (selection.Id, true);
    }

    /// <summary>
    /// Computes a deterministic SHA-256 hash for a single priced selection when
    /// the provider did not supply a raw payload hash. The hash covers the fields
    /// that uniquely identify the priced point in time so repeated identical
    /// snapshots collapse to one row.
    /// </summary>
    private static string ComputeSelectionHash(
        string bookmakerExternalId,
        string matchExternalId,
        ProviderMarketLineDto line,
        ProviderSelectionDto selection,
        DateTime providerTimestampUtc)
    {
        var lineValue = line.Line?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        var marketLineKey = $"{line.MarketExternalId}:{lineValue}:{line.Period ?? string.Empty}";
        var payload = string.Join(
            '|',
            bookmakerExternalId,
            matchExternalId,
            marketLineKey,
            selection.Name,
            selection.DecimalOdds.ToString(CultureInfo.InvariantCulture),
            providerTimestampUtc.ToString("O", CultureInfo.InvariantCulture));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(bytes);
    }

    private async Task<Provider?> ResolveProviderAsync(string providerCode, CancellationToken ct)
    {
        var existing = await _providers.ListAsync(ct).ConfigureAwait(false);
        return existing.FirstOrDefault(
            p => string.Equals(p.Code, providerCode, StringComparison.OrdinalIgnoreCase));
    }
}
