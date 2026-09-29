namespace CalcioAnalytic.Analytics.Odds;

/// <summary>
/// Pure, stateless helpers for odds arithmetic. All calculations operate on
/// <see cref="decimal"/> to preserve the precision expected of monetary and
/// probability values, and every method guards against degenerate inputs
/// (odds &lt;= 1, empty collections, zero overround) by returning safe defaults
/// rather than throwing.
/// </summary>
/// <remarks>
/// Worked overround example (documented for correctness verification):
/// decimal odds 2.10 / 3.40 / 3.60 produce implied probabilities
/// 1/2.10 = 0.476190..., 1/3.40 = 0.294117..., 1/3.60 = 0.277777...
/// whose sum (the overround) is approximately 1.048085 — i.e. a book margin of
/// about 4.81%. Dividing each implied probability by that overround yields the
/// fair, normalized probabilities that sum to exactly 1.
/// </remarks>
public static class OddsMath
{
    /// <summary>
    /// Converts decimal (European) odds to the implied probability of the
    /// outcome, defined as <c>1 / decimalOdds</c>.
    /// </summary>
    /// <param name="decimalOdds">Decimal odds (must be greater than 1 to be meaningful).</param>
    /// <returns>
    /// The implied probability in the range (0, 1). Returns <c>0</c> when
    /// <paramref name="decimalOdds"/> is less than or equal to 1, since such odds
    /// carry no meaningful (positive-margin) probability.
    /// </returns>
    public static decimal ImpliedProbability(decimal decimalOdds)
    {
        if (decimalOdds <= 1m)
        {
            return 0m;
        }

        return 1m / decimalOdds;
    }

    /// <summary>
    /// Computes the overround (also called the "book" or "vig") for a market as
    /// the sum of the implied probabilities of all its selections. A fair market
    /// sums to 1; a real market with a bookmaker margin sums to slightly more
    /// than 1.
    /// </summary>
    /// <param name="decimalOddsForAllSelectionsInAMarket">
    /// The decimal odds of every selection that composes the market.
    /// </param>
    /// <returns>
    /// The sum of implied probabilities, or <c>0</c> when the input is null or
    /// empty. Selections with odds &lt;= 1 contribute 0 (see
    /// <see cref="ImpliedProbability"/>).
    /// </returns>
    public static decimal Overround(IEnumerable<decimal> decimalOddsForAllSelectionsInAMarket)
    {
        if (decimalOddsForAllSelectionsInAMarket is null)
        {
            return 0m;
        }

        decimal sum = 0m;
        foreach (var odds in decimalOddsForAllSelectionsInAMarket)
        {
            sum += ImpliedProbability(odds);
        }

        return sum;
    }

    /// <summary>
    /// Normalizes the implied probabilities of a market's selections so that they
    /// sum to exactly 1, removing the bookmaker margin. Each selection's implied
    /// probability is divided by the market overround.
    /// </summary>
    /// <param name="decimalOdds">The decimal odds of every selection, in order.</param>
    /// <returns>
    /// A list of fair probabilities aligned to the input order that sum to 1.
    /// Returns an empty list when the input is null or empty, and a list of zeros
    /// when the overround is 0 (all odds &lt;= 1).
    /// </returns>
    public static IReadOnlyList<decimal> NormalizedProbabilities(IEnumerable<decimal> decimalOdds)
    {
        if (decimalOdds is null)
        {
            return Array.Empty<decimal>();
        }

        var oddsList = decimalOdds as IReadOnlyList<decimal> ?? decimalOdds.ToList();
        if (oddsList.Count == 0)
        {
            return Array.Empty<decimal>();
        }

        var implied = new decimal[oddsList.Count];
        decimal overround = 0m;
        for (var i = 0; i < oddsList.Count; i++)
        {
            implied[i] = ImpliedProbability(oddsList[i]);
            overround += implied[i];
        }

        var normalized = new decimal[oddsList.Count];
        if (overround <= 0m)
        {
            // No meaningful probabilities; return zeros rather than dividing by zero.
            return normalized;
        }

        for (var i = 0; i < oddsList.Count; i++)
        {
            normalized[i] = implied[i] / overround;
        }

        return normalized;
    }

    /// <summary>
    /// Converts decimal odds to their fractional (UK) representation on a
    /// best-effort basis. The fractional profit <c>decimalOdds - 1</c> is scaled
    /// to a denominator of 100 and reduced by the greatest common divisor, so the
    /// result is an approximation for odds that are not clean fractions.
    /// </summary>
    /// <param name="decimalOdds">Decimal odds (must be greater than 1).</param>
    /// <returns>
    /// A string of the form "numerator/denominator" (e.g. "11/10"), or
    /// <c>"0/1"</c> when <paramref name="decimalOdds"/> is less than or equal to 1.
    /// </returns>
    public static string DecimalToFractional(decimal decimalOdds)
    {
        if (decimalOdds <= 1m)
        {
            return "0/1";
        }

        // Represent the net profit as a fraction over a fixed denominator, then reduce.
        const long denominator = 100L;
        var profit = decimalOdds - 1m;
        var numerator = (long)Math.Round(profit * denominator, MidpointRounding.AwayFromZero);
        if (numerator <= 0)
        {
            return "0/1";
        }

        var divisor = Gcd(numerator, denominator);
        return $"{numerator / divisor}/{denominator / divisor}";
    }

    /// <summary>
    /// Converts decimal odds to their American (moneyline) representation on a
    /// best-effort basis, rounding to the nearest whole number.
    /// </summary>
    /// <param name="decimalOdds">Decimal odds (must be greater than 1).</param>
    /// <returns>
    /// A positive moneyline for underdogs (odds &gt;= 2.0), a negative moneyline for
    /// favorites (1 &lt; odds &lt; 2.0), or <c>0</c> when
    /// <paramref name="decimalOdds"/> is less than or equal to 1.
    /// </returns>
    public static int DecimalToAmerican(decimal decimalOdds)
    {
        if (decimalOdds <= 1m)
        {
            return 0;
        }

        if (decimalOdds >= 2m)
        {
            return (int)Math.Round((decimalOdds - 1m) * 100m, MidpointRounding.AwayFromZero);
        }

        return -(int)Math.Round(100m / (decimalOdds - 1m), MidpointRounding.AwayFromZero);
    }

    private static long Gcd(long a, long b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a == 0 ? 1 : a;
    }
}
