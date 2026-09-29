using CalcioAnalytic.Domain.Odds;

namespace CalcioAnalytic.Analytics.Odds;

/// <summary>
/// The price of a single selection quoted by one bookmaker, used as an input to
/// cross-bookmaker dispersion analysis.
/// </summary>
/// <param name="BookmakerId">The bookmaker offering the price.</param>
/// <param name="DecimalOdds">The quoted decimal odds.</param>
public sealed record BookmakerPrice(Guid BookmakerId, decimal DecimalOdds);

/// <summary>
/// The distribution of a single selection's price across multiple bookmakers at
/// a common point in time (typically closing or latest). Odds retain full
/// precision.
/// </summary>
/// <param name="BookmakerCount">Number of bookmakers contributing a price.</param>
/// <param name="BestOdds">Highest decimal odds (best value for a backer).</param>
/// <param name="BestBookmakerId">The bookmaker offering <see cref="BestOdds"/>.</param>
/// <param name="WorstOdds">Lowest decimal odds (worst value for a backer).</param>
/// <param name="WorstBookmakerId">The bookmaker offering <see cref="WorstOdds"/>.</param>
/// <param name="AverageOdds">Arithmetic mean of the quoted odds.</param>
/// <param name="Dispersion">Population standard deviation of the quoted odds.</param>
public sealed record BookmakerDispersionResult(
    int BookmakerCount,
    decimal BestOdds,
    Guid BestBookmakerId,
    decimal WorstOdds,
    Guid WorstBookmakerId,
    decimal AverageOdds,
    decimal Dispersion)
{
    /// <summary>An empty result, used when no prices are supplied.</summary>
    public static BookmakerDispersionResult Empty { get; } =
        new(0, 0m, Guid.Empty, 0m, Guid.Empty, 0m, 0m);
}

/// <summary>
/// Analyzes how a single selection's price differs across bookmakers. The caller
/// supplies one representative price per bookmaker (e.g. the closing or latest
/// snapshot) for one market line and selection.
/// </summary>
public static class BookmakerAnalyzer
{
    /// <summary>
    /// Computes the best, worst, average, and dispersion of prices across
    /// bookmakers from raw snapshots. Snapshots are grouped by bookmaker and the
    /// latest snapshot per bookmaker (by provider timestamp) is used.
    /// </summary>
    /// <param name="snapshots">
    /// Snapshots for a single market line and selection across bookmakers.
    /// </param>
    /// <returns>A <see cref="BookmakerDispersionResult"/>.</returns>
    public static BookmakerDispersionResult AnalyzeLatestPerBookmaker(IEnumerable<OddsSnapshot> snapshots)
    {
        if (snapshots is null)
        {
            return BookmakerDispersionResult.Empty;
        }

        var latestPerBookmaker = snapshots
            .GroupBy(s => s.BookmakerId)
            .Select(g => g
                .OrderBy(s => s.ProviderTimestampUtc)
                .ThenBy(s => s.IngestionTimestampUtc)
                .ThenBy(s => s.Id)
                .Last())
            .Select(s => new BookmakerPrice(s.BookmakerId, s.DecimalOdds));

        return Analyze(latestPerBookmaker);
    }

    /// <summary>
    /// Computes the best, worst, average, and dispersion of prices across
    /// bookmakers from pre-selected prices.
    /// </summary>
    /// <param name="prices">One price per bookmaker.</param>
    /// <returns>A <see cref="BookmakerDispersionResult"/>.</returns>
    public static BookmakerDispersionResult Analyze(IEnumerable<BookmakerPrice> prices)
    {
        if (prices is null)
        {
            return BookmakerDispersionResult.Empty;
        }

        // Deterministic ordering so ties for best/worst resolve stably.
        var list = prices
            .OrderByDescending(p => p.DecimalOdds)
            .ThenBy(p => p.BookmakerId)
            .ToList();

        if (list.Count == 0)
        {
            return BookmakerDispersionResult.Empty;
        }

        var best = list[0];
        var worst = list[^1];

        decimal sum = 0m;
        for (var i = 0; i < list.Count; i++)
        {
            sum += list[i].DecimalOdds;
        }

        var average = sum / list.Count;
        var dispersion = OddsMovementAnalyzer.PopulationStandardDeviation(list.Select(p => p.DecimalOdds));

        return new BookmakerDispersionResult(
            list.Count,
            best.DecimalOdds,
            best.BookmakerId,
            worst.DecimalOdds,
            worst.BookmakerId,
            average,
            dispersion);
    }
}
