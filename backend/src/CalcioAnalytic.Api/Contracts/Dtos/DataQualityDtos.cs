using CalcioAnalytic.Analytics.DataQuality;

namespace CalcioAnalytic.Api.Contracts.Dtos;

/// <summary>
/// Wire representation of a single data-quality check, mirroring
/// <see cref="DataQualityCheck"/>.
/// </summary>
/// <param name="Code">Stable machine-readable identifier for the check.</param>
/// <param name="Severity">Severity classification: "Info", "Warning", or "Error".</param>
/// <param name="Message">Human-readable explanation of the check outcome.</param>
/// <param name="Count">Number of items the check flagged; 0 when the check passed.</param>
public sealed record DataQualityCheckDto(string Code, string Severity, string Message, int Count);

/// <summary>
/// Wire representation of a match data-quality report, mirroring
/// <see cref="DataQualityReport"/>.
/// </summary>
/// <param name="MatchId">The match the report describes, or null when not match-specific.</param>
/// <param name="Score">Overall quality score in the range [0, 1].</param>
/// <param name="TotalChecks">Total number of checks evaluated.</param>
/// <param name="Failed">Number of checks that flagged a problem.</param>
/// <param name="Checks">The individual check results, in a stable order.</param>
/// <param name="Methodology">Version identifier of the scoring methodology used.</param>
public sealed record DataQualityReportDto(
    Guid? MatchId,
    decimal Score,
    int TotalChecks,
    int Failed,
    IReadOnlyList<DataQualityCheckDto> Checks,
    string Methodology)
{
    /// <summary>
    /// Projects a pure <see cref="DataQualityReport"/> into its wire DTO.
    /// </summary>
    /// <param name="report">The report produced by the data-quality engine.</param>
    /// <returns>The corresponding response DTO.</returns>
    public static DataQualityReportDto FromReport(DataQualityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var checks = report.Checks
            .Select(c => new DataQualityCheckDto(c.Code, c.Severity, c.Message, c.Count))
            .ToList();

        return new DataQualityReportDto(
            report.MatchId,
            report.Score,
            report.TotalChecks,
            report.Failed,
            checks,
            report.Methodology);
    }
}
