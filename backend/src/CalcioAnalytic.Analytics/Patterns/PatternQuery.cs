using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CalcioAnalytic.Analytics.Patterns;

/// <summary>
/// A reproducible, identifiable filter definition applied over a collection of
/// <see cref="PatternMatchRecord"/> values. Every field participates in the
/// <see cref="QueryHash"/> so that an identical query produces an identical hash,
/// making a historical question reproducible and cacheable.
/// </summary>
/// <param name="CompetitionId">When set, only matches in this competition are included.</param>
/// <param name="SeasonId">When set, only matches in this season are included.</param>
/// <param name="FromUtc">When set, only matches kicking off at or after this UTC instant are included.</param>
/// <param name="ToUtc">When set, only matches kicking off at or before this UTC instant are included.</param>
/// <param name="HomeOrAway">
/// Reserved perspective filter: "Home", "Away", or null for both. Retained as
/// part of the reproducible query definition.
/// </param>
/// <param name="MinClosingHomeOdds">When set, only matches with closing home odds at or above this value are included.</param>
/// <param name="MaxClosingHomeOdds">When set, only matches with closing home odds at or below this value are included.</param>
/// <param name="MinMovementPercentage">When set, only matches with movement percentage at or above this value are included.</param>
/// <param name="MaxMovementPercentage">When set, only matches with movement percentage at or below this value are included.</param>
public sealed record PatternQuery(
    Guid? CompetitionId,
    Guid? SeasonId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? HomeOrAway,
    decimal? MinClosingHomeOdds,
    decimal? MaxClosingHomeOdds,
    decimal? MinMovementPercentage,
    decimal? MaxMovementPercentage)
{
    /// <summary>
    /// A stable SHA-256 hex digest computed over a canonical string form of every
    /// filter field. Two queries with identical fields yield an identical hash,
    /// making the query reproducible and safe to use as a cache/identity key.
    /// </summary>
    public string QueryHash
    {
        get
        {
            var canonical = BuildCanonicalString();
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Builds a deterministic, culture-invariant canonical representation of all
    /// filter fields. Nulls are rendered as an empty token so absent fields still
    /// contribute to the hash unambiguously.
    /// </summary>
    private string BuildCanonicalString()
    {
        static string G(Guid? value) => value?.ToString("N", CultureInfo.InvariantCulture) ?? string.Empty;
        static string D(DateTime? value) => value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
        static string M(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        static string S(string? value) => value ?? string.Empty;

        var builder = new StringBuilder();
        builder.Append("competitionId=").Append(G(CompetitionId)).Append('|');
        builder.Append("seasonId=").Append(G(SeasonId)).Append('|');
        builder.Append("fromUtc=").Append(D(FromUtc)).Append('|');
        builder.Append("toUtc=").Append(D(ToUtc)).Append('|');
        builder.Append("homeOrAway=").Append(S(HomeOrAway)).Append('|');
        builder.Append("minClosingHomeOdds=").Append(M(MinClosingHomeOdds)).Append('|');
        builder.Append("maxClosingHomeOdds=").Append(M(MaxClosingHomeOdds)).Append('|');
        builder.Append("minMovementPercentage=").Append(M(MinMovementPercentage)).Append('|');
        builder.Append("maxMovementPercentage=").Append(M(MaxMovementPercentage));
        return builder.ToString();
    }
}
