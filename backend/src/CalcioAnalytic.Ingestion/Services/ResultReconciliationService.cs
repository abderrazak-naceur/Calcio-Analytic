using System.Globalization;
using CalcioAnalytic.Application.Abstractions.Persistence;
using CalcioAnalytic.Application.Ingestion;
using CalcioAnalytic.Contracts.Providers;
using CalcioAnalytic.Domain.Catalog;
using CalcioAnalytic.Domain.Matches;
using CalcioAnalytic.Ingestion.Providers;
using Microsoft.Extensions.Logging;

namespace CalcioAnalytic.Ingestion.Services;

/// <summary>
/// Application-level implementation of <see cref="IResultReconciliationService"/>.
/// Loads a finished match, resolves its provider "Match" mappings, fetches each
/// provider's currently reported fixture, and compares those reported results to
/// the canonical stored score.
/// </summary>
/// <remarks>
/// <para>
/// The service is honest and non-destructive. When every available source agrees
/// with the stored score it advances the match to
/// <see cref="MatchStatus.Reconciled"/>. When no source is available it reports
/// <c>Unconfirmed</c> and changes nothing. When any source disagrees it reports a
/// <c>Conflict</c>, surfaces the discrepancies, and leaves the match untouched —
/// a stored score is never silently overwritten.
/// </para>
/// <para>
/// "Cross-provider" reconciliation is structurally supported: every provider
/// mapping for the match is consulted. With a single provider this degrades to
/// single-source confirmation.
/// </para>
/// <para>
/// Reconciliation is idempotent. A match already in
/// <see cref="MatchStatus.Reconciled"/> short-circuits to a confirmed result
/// without re-fetching or re-writing anything.
/// </para>
/// </remarks>
public sealed class ResultReconciliationService : IResultReconciliationService
{
    private const string MatchEntityType = "Match";

    private const string StatusReconciled = "Reconciled";
    private const string StatusUnconfirmed = "Unconfirmed";
    private const string StatusConflict = "Conflict";
    private const string StatusNotFinished = "NotFinished";
    private const string StatusNotFound = "NotFound";

    private readonly IMatchRepository _matches;
    private readonly IRepository<ProviderEntityMap> _entityMaps;
    private readonly IRepository<Provider> _providers;
    private readonly IProviderRegistry _registry;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResultReconciliationService> _logger;

    /// <summary>Initializes a new instance of the <see cref="ResultReconciliationService"/> class.</summary>
    public ResultReconciliationService(
        IMatchRepository matches,
        IRepository<ProviderEntityMap> entityMaps,
        IRepository<Provider> providers,
        IProviderRegistry registry,
        IUnitOfWork unitOfWork,
        ILogger<ResultReconciliationService> logger)
    {
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(entityMaps);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(logger);

        _matches = matches;
        _entityMaps = entityMaps;
        _providers = providers;
        _registry = registry;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReconciliationResult> ReconcileMatchAsync(Guid matchId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting reconciliation for match {MatchId}.", matchId);

        var match = await _matches.GetByIdAsync(matchId, ct).ConfigureAwait(false);
        if (match is null)
        {
            _logger.LogWarning("Match {MatchId} was not found; nothing to reconcile.", matchId);
            return new ReconciliationResult(matchId, false, StatusNotFound, Array.Empty<string>());
        }

        // Idempotent: an already-reconciled match is confirmed without re-work.
        if (match.Status == MatchStatus.Reconciled)
        {
            _logger.LogInformation("Match {MatchId} is already reconciled.", matchId);
            return new ReconciliationResult(matchId, true, StatusReconciled, Array.Empty<string>());
        }

        // Only matches that have finished play (and any post-finish states) can be reconciled.
        if (!IsReconcilable(match.Status))
        {
            _logger.LogInformation(
                "Match {MatchId} is in status {Status} and is not ready for reconciliation.",
                matchId,
                match.Status);
            return new ReconciliationResult(matchId, false, StatusNotFinished, Array.Empty<string>());
        }

        // Resolve this match's provider "Match" mappings across all providers.
        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allMaps = await _entityMaps.ListAsync(ct).ConfigureAwait(false);
        var matchMaps = allMaps
            .Where(m => string.Equals(m.EntityType, MatchEntityType, StringComparison.Ordinal)
                        && m.InternalId == matchId)
            .ToList();

        // TODO: replace in-memory ListAsync filtering with targeted queries at scale.
        var allProviders = await _providers.ListAsync(ct).ConfigureAwait(false);
        var providersById = allProviders.ToDictionary(p => p.Id);

        var discrepancies = new List<string>();
        var sourceCount = 0;

        foreach (var map in matchMaps)
        {
            if (!providersById.TryGetValue(map.ProviderId, out var provider))
            {
                _logger.LogWarning(
                    "Provider {ProviderId} referenced by match {MatchId} mapping was not found; skipping.",
                    map.ProviderId,
                    matchId);
                continue;
            }

            if (!_registry.TryGet<IFixtureProvider>(provider.Code, out var fixtureProvider)
                || fixtureProvider is null)
            {
                _logger.LogInformation(
                    "Provider {ProviderCode} has no fixture capability registered; skipping for match {MatchId}.",
                    provider.Code,
                    matchId);
                continue;
            }

            var fixture = await fixtureProvider
                .GetFixtureAsync(map.ExternalId, ct)
                .ConfigureAwait(false);
            if (fixture is null)
            {
                _logger.LogInformation(
                    "Provider {ProviderCode} returned no fixture for external id {ExternalId} (match {MatchId}); skipping.",
                    provider.Code,
                    map.ExternalId,
                    matchId);
                continue;
            }

            sourceCount++;

            var mismatch = DescribeMismatch(provider.Code, match, fixture);
            if (mismatch is not null)
            {
                discrepancies.Add(mismatch);
            }
        }

        // No source could be consulted: we cannot verify the stored result.
        if (sourceCount == 0)
        {
            _logger.LogInformation(
                "Match {MatchId} has no available provider sources; result is unconfirmed.",
                matchId);
            return new ReconciliationResult(matchId, false, StatusUnconfirmed, Array.Empty<string>());
        }

        // At least one source disagreed: never overwrite, report the conflict.
        if (discrepancies.Count > 0)
        {
            _logger.LogWarning(
                "Match {MatchId} reconciliation found {Count} discrepancy(ies); leaving status unchanged.",
                matchId,
                discrepancies.Count);
            return new ReconciliationResult(matchId, false, StatusConflict, discrepancies);
        }

        // Every available source agrees: advance the lifecycle.
        match.Status = MatchStatus.Reconciled;
        _matches.Update(match);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Match {MatchId} confirmed by {Count} source(s) and advanced to Reconciled.",
            matchId,
            sourceCount);

        return new ReconciliationResult(matchId, true, StatusReconciled, Array.Empty<string>());
    }

    /// <summary>
    /// Determines whether a match in the given status is eligible for reconciliation.
    /// Reconciliation runs once a match has finished play, including the
    /// post-finish settlement and analysis states.
    /// </summary>
    private static bool IsReconcilable(MatchStatus status) =>
        status is MatchStatus.Finished
            or MatchStatus.SettlementPending
            or MatchStatus.Analyzed;

    /// <summary>
    /// Compares a provider's reported fixture score to the stored match score and
    /// returns a human-readable description of any mismatch, or <c>null</c> when
    /// the full-time (and any reported half-time) scores agree.
    /// </summary>
    private static string? DescribeMismatch(string providerCode, Match match, ProviderMatchDto fixture)
    {
        var storedFull = FormatScore(match.HomeScore, match.AwayScore);
        var reportedFull = FormatScore(fixture.HomeScore, fixture.AwayScore);

        if (!ScoresEqual(match.HomeScore, match.AwayScore, fixture.HomeScore, fixture.AwayScore))
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "provider {0} reports {1} but stored is {2}",
                providerCode,
                reportedFull,
                storedFull);
        }

        // Only assert half-time agreement when the provider actually reports it.
        if ((fixture.HomeScoreHalfTime is not null || fixture.AwayScoreHalfTime is not null)
            && !ScoresEqual(
                match.HomeScoreHalfTime,
                match.AwayScoreHalfTime,
                fixture.HomeScoreHalfTime,
                fixture.AwayScoreHalfTime))
        {
            var storedHt = FormatScore(match.HomeScoreHalfTime, match.AwayScoreHalfTime);
            var reportedHt = FormatScore(fixture.HomeScoreHalfTime, fixture.AwayScoreHalfTime);
            return string.Format(
                CultureInfo.InvariantCulture,
                "provider {0} reports half-time {1} but stored is {2}",
                providerCode,
                reportedHt,
                storedHt);
        }

        return null;
    }

    /// <summary>Compares two nullable score pairs for exact equality.</summary>
    private static bool ScoresEqual(int? homeA, int? awayA, int? homeB, int? awayB) =>
        homeA == homeB && awayA == awayB;

    /// <summary>Renders a nullable score pair as "H-A", using "?" for unknown components.</summary>
    private static string FormatScore(int? home, int? away) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0}-{1}",
            home?.ToString(CultureInfo.InvariantCulture) ?? "?",
            away?.ToString(CultureInfo.InvariantCulture) ?? "?");
}
