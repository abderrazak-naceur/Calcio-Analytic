namespace CalcioAnalytic.Contracts.Providers;

/// <summary>
/// Provider-agnostic representation of a competition season as delivered by an external data provider.
/// </summary>
/// <param name="ExternalId">The provider-native identifier for the season (provider-defined format).</param>
/// <param name="CompetitionExternalId">The provider-native identifier of the competition this season belongs to.</param>
/// <param name="Label">The season label (e.g. "2024/2025").</param>
/// <param name="StartDate">The optional start date of the season.</param>
/// <param name="EndDate">The optional end date of the season.</param>
public sealed record ProviderSeasonDto(
    string ExternalId,
    string CompetitionExternalId,
    string Label,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null);
