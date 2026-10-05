/**
 * Response types for the Calcio-Analytic backend API.
 *
 * All backend JSON is camelCase. These interfaces mirror only what the API
 * returns — nothing is fabricated on the client.
 */

/** Match status as returned by the backend (e.g. "Scheduled", "Live", "Finished"). */
export type MatchStatus = string

/** Item shape from GET /api/v1/matches. */
export interface MatchSummary {
  id: string
  competitionId: string
  seasonId: string
  homeTeamId: string
  awayTeamId: string
  kickoffUtc: string
  status: MatchStatus
  homeScore: number | null
  awayScore: number | null
}

/** Shape from GET /api/v1/matches/{id} (summary plus detail fields). */
export interface MatchDetail extends MatchSummary {
  venue: string | null
  round: string | null
  referee: string | null
  homeScoreHalfTime: number | null
  awayScoreHalfTime: number | null
}

/** A single selection within a market line. */
export interface MarketSelection {
  selectionId: string
  selectionName: string
  decimalOdds: number
  impliedProbability: number
  normalizedProbability: number
}

/** A market line with its selections and overround/margin. */
export interface MarketAnalysis {
  marketLineId: string
  overround: number
  marginPercentage: number
  selections: MarketSelection[]
}

/** Match result summary within the analysis report. */
export interface MatchResult {
  matchId: string
  homeTeamId: string
  awayTeamId: string
  homeScore: number | null
  awayScore: number | null
  outcome: string
}

/** Movement statistics for a single bookmaker/market/selection. */
export interface OddsMovementStats {
  sampleCount: number
  openingOdds: number
  closingOdds: number
  minOdds: number
  maxOdds: number
  numberOfChanges: number
  movementAbsolute: number
  movementPercentage: number
  volatility: number
}

/** An odds movement entry. */
export interface OddsMovement {
  bookmakerId: string
  marketLineId: string
  selectionId: string
  movement: OddsMovementStats
}

/** Dispersion statistics across bookmakers. */
export interface BookmakerDispersionStats {
  bookmakerCount: number
  bestOdds: number
  worstOdds: number
  averageOdds: number
  dispersion: number
}

/** A bookmaker dispersion entry. */
export interface BookmakerDispersion {
  marketLineId: string
  selectionId: string
  dispersion: BookmakerDispersionStats
}

/** A named statistic aggregate. */
export interface StatisticValue {
  name: string
  totalValue: number
}

/** A count of a given event type. */
export interface EventCount {
  type: string
  count: number
}

/** Statistics block within the analysis report. */
export interface MatchStatistics {
  statistics: StatisticValue[]
  eventCounts: EventCount[]
  totalEvents: number
}

/** Shape from GET /api/v1/analytics/matches/{id}. */
export interface MatchAnalysisReport {
  result: MatchResult
  markets: MarketAnalysis[]
  oddsMovements: OddsMovement[]
  bookmakerDispersions: BookmakerDispersion[]
  statistics: MatchStatistics
  methodologyVersion: string
  generatedAtUtc: string
}

/** Item shape from GET /api/v1/analytics/matches/{id}/odds. */
export interface OddsSnapshot {
  id: string
  matchId: string
  bookmakerId: string
  marketLineId: string
  selectionId: string
  decimalOdds: number
  impliedProbability: number
  isLive: boolean
  kind: string
  bookmakerTimestampUtc: string
  providerTimestampUtc: string
}

/** Request body for the ingestion endpoints. */
export interface IngestionRequest {
  providerCode: string
  competitionExternalId?: string
  seasonExternalId?: string
  matchExternalId?: string
}

/** Query filter for POST /api/v1/analytics/patterns/query. */
export interface PatternQueryRequest {
  competitionId?: string
  seasonId?: string
  fromUtc?: string
  toUtc?: string
  homeOrAway?: string
  minClosingHomeOdds?: number
  maxClosingHomeOdds?: number
  minMovementPercentage?: number
  maxMovementPercentage?: number
}

/** Distribution of match outcomes (counts). */
export interface ResultDistribution {
  Home: number
  Draw: number
  Away: number
}

/** Over/under 2.5 goals distribution (counts). */
export interface OverUnderDistribution {
  'Over2.5': number
  'Under2.5': number
}

/** A confidence interval bound pair. */
export interface ConfidenceInterval {
  lower: number
  upper: number
}

/** Shape from POST /api/v1/analytics/patterns/query. */
export interface PatternResult {
  sampleSize: number
  resultDistribution: ResultDistribution
  resultPercentages: ResultDistribution
  averageTotalGoals: number
  overUnderDistribution: OverUnderDistribution
  averageClosingHomeOdds: number
  averageMovementPercentage: number
  dataCompleteness: number
  homeWinConfidenceInterval: ConfidenceInterval | null
  methodology: string
  queryHash: string
}

/** Item shape from GET /api/v1/analytics/matches/{id}/similar. */
export interface SimilarMatch {
  matchId: string
  similarityScore: number
  explanation: string[]
}
/** Shape from GET /api/v1/dashboard/summary. */
export interface DashboardSummary {
  totalMatches: number
  matchesByStatus: Record<string, number>
  finishedMatches: number
  analyzedMatches: number
  oddsSnapshotsCount: number
  bookmakersCount: number
  marketsCount: number
  competitionsCount: number
  teamsCount: number
  dataFreshnessUtc: string | null
  analysisBacklog: number
}

/** Item shape from GET /api/v1/dashboard/recent. */
export interface RecentMatch {
  id: string
  homeTeamId: string
  awayTeamId: string
  kickoffUtc: string
  status: MatchStatus
  homeScore: number | null
  awayScore: number | null
}

export interface ProviderCompetition {
  externalId: string
  name: string
  countryName: string | null
  tier: string | null
}

export interface ProviderSeason {
  externalId: string
  competitionExternalId: string
  label: string
  startDate: string | null
  endDate: string | null
}

export interface SportmonksSeasonImportResult {
  leagueId: string
  seasonId: string
  fixturesUpserted: number
  completedFixturesWithOddsRequested: number
  oddsRequestsAttempted: number
  oddsSnapshotsInserted: number
  duplicateOddsSnapshotsSkipped: number
  oddsWarning: string | null
  catalog: {
    competitionsUpserted: number
    seasonsUpserted: number
    teamsUpserted: number
    bookmakersUpserted: number
    marketsUpserted: number
  }
}

/** A single data-quality check within a report. */
export interface DataQualityCheck {
  code: string
  severity: string
  message: string
  count: number
}

/** Shape from GET /api/v1/dataquality/matches/{id}. */
export interface DataQualityReport {
  matchId: string
  score: number
  totalChecks: number
  failed: number
  checks: DataQualityCheck[]
  methodology: string
}

/**
 * Shape from GET /api/v1/analytics/matches/{id}/summary.
 *
 * The AI-generated match summary (proxied from the Python analytics service).
 * Named `MatchAiSummary` to avoid clashing with {@link MatchSummary}, which is
 * the match-list item.
 */
export interface MatchAiSummary {
  summary: string
  bullets: string[]
  dataCompleteness: Record<string, boolean>
  caveats: string[]
}

/** Historical high-odds intelligence contracts. */
export interface HighOddsQuery {
  fromUtc: string | null
  toUtc: string | null
  minOdds: number
  competitionId: string | null
  bookmakerId: string | null
  result: string | null
  page: number
  pageSize: number
}

export interface HighOddsSelection {
  matchId: string
  kickoffUtc: string
  competitionId: string
  competitionName: string
  homeTeamId: string
  homeTeamName: string
  awayTeamId: string
  awayTeamName: string
  selection: string
  result: string
  won: boolean
  odds: number
  impliedProbability: number
  bookmakerId: string
  bookmakerName: string
  openingOdds: number | null
  closingOdds: number | null
  movementPercentage: number | null
  profitUnits: number
}

export interface HighOddsRangeStats {
  range: string
  selections: number
  wins: number
  winRatePercentage: number
  averageOdds: number
  profitUnits: number
  roiPercentage: number
}

export interface HighOddsGroupStats {
  group: string
  selections: number
  wins: number
  winRatePercentage: number
  averageOdds: number
  profitUnits: number
  roiPercentage: number
}

export interface HighOddsSummary {
  uniqueMatches: number
  qualifyingSelections: number
  wins: number
  winRatePercentage: number
  averageOdds: number
  averageImpliedProbabilityPercentage: number
  stakeUnits: number
  returnUnits: number
  profitUnits: number
  roiPercentage: number
  maxDrawdownUnits: number
  maxWinningStreak: number
  maxLosingStreak: number
}

export interface HighOddsAnalytics {
  query: HighOddsQuery
  summary: HighOddsSummary
  byOddsRange: HighOddsRangeStats[]
  bySelection: HighOddsGroupStats[]
  byBookmaker: HighOddsGroupStats[]
  results: HighOddsSelection[]
  totalResults: number
  page: number
  pageSize: number
}

export interface HighOddsCatalogItem {
  id: string
  name: string
}

export interface HighOddsCatalog {
  competitions: HighOddsCatalogItem[]
  bookmakers: HighOddsCatalogItem[]
}
