using CalcioAnalytic.Analytics.Odds;
using CalcioAnalytic.Domain.Odds;

namespace CalcioAnalytic.Api.Contracts.Dtos;

/// <summary>
/// A projection of a single odds snapshot for a match. Scoped scalar fields only.
/// </summary>
public sealed record OddsSnapshotDto(
    Guid Id,
    Guid BookmakerId,
    Guid MarketLineId,
    Guid SelectionId,
    decimal DecimalOdds,
    decimal ImpliedProbability,
    bool IsLive,
    OddsSnapshotKind Kind,
    DateTime BookmakerTimestampUtc,
    DateTime ProviderTimestampUtc);

/// <summary>
/// The odds movement result for a single (bookmaker, market line, selection)
/// group, pairing the grouping keys with the computed <see cref="OddsMovementResult"/>.
/// </summary>
public sealed record OddsMovementDto(
    Guid BookmakerId,
    Guid MarketLineId,
    Guid SelectionId,
    OddsMovementResult Movement);

/// <summary>
/// The cross-bookmaker dispersion result for a single (market line, selection)
/// group, pairing the grouping keys with the computed
/// <see cref="BookmakerDispersionResult"/>.
/// </summary>
public sealed record BookmakerDispersionDto(
    Guid MarketLineId,
    Guid SelectionId,
    BookmakerDispersionResult Dispersion);
