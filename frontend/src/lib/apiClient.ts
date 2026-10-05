/**
 * API client for the Calcio-Analytic backend (.NET API).
 *
 * The base URL is read from the `VITE_API_BASE_URL` environment variable and
 * falls back to the local development API at http://localhost:8080.
 */

export const API_BASE_URL: string =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080'

export interface HealthResponse {
  /** Reported service status (e.g. "Healthy"). */
  status: string
  /** Any additional fields the backend may return. */
  [key: string]: unknown
}

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

/**
 * Thin fetch wrapper that resolves a path against the configured base URL,
 * sets JSON headers, and surfaces non-2xx responses as {@link ApiError}.
 */
export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  const url = `${API_BASE_URL}${path.startsWith('/') ? path : `/${path}`}`

  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...init?.headers,
    },
  })

  if (!response.ok) {
    throw new ApiError(
      `Request to ${path} failed with status ${response.status}`,
      response.status,
    )
  }

  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('application/json')) {
    return (await response.json()) as T
  }

  // Fallback: some /health endpoints return plain text.
  const text = await response.text()
  return text as unknown as T
}

/**
 * Calls the backend `/health` endpoint.
 *
 * Normalizes both JSON (`{ "status": "Healthy" }`) and plain-text
 * (`"Healthy"`) responses into a {@link HealthResponse}.
 */
export async function getHealth(): Promise<HealthResponse> {
  const result = await apiFetch<HealthResponse | string>('/health')

  if (typeof result === 'string') {
    return { status: result.trim() || 'Unknown' }
  }

  return result
}

import type {
  BookmakerDispersion,
  DashboardSummary,
  DataQualityReport,
  IngestionRequest,
  HighOddsAnalytics,
  HighOddsCatalog,
  MatchAiSummary,
  MarketOutcomeAnalytics,
  UpcomingAnalysis,
  DailyMarketReport,
  MatchAnalysisReport,
  MatchDetail,
  MatchSummary,
  OddsMovement,
  OddsSnapshot,
  PatternQueryRequest,
  PatternResult,
  ProviderCompetition,
  ProviderSeason,
  RecentMatch,
  SimilarMatch,
  SportmonksBulkSeasonImportResult,
  SportmonksSeasonImportResult,
  DemoOddsSeedResult,
} from './types'

/**
 * POST helper that serializes a JSON body and returns the parsed response.
 */
async function apiPost<TResponse>(
  path: string,
  body: unknown,
): Promise<TResponse> {
  return apiFetch<TResponse>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

/** GET /api/v1/ingestion/sportmonks/leagues. */
export async function getSportmonksLeagues(): Promise<ProviderCompetition[]> {
  return apiFetch<ProviderCompetition[]>('/api/v1/ingestion/sportmonks/leagues')
}

/** GET /api/v1/ingestion/sportmonks/leagues/{leagueId}/seasons. */
export async function getSportmonksSeasons(leagueId: string): Promise<ProviderSeason[]> {
  return apiFetch<ProviderSeason[]>(`/api/v1/ingestion/sportmonks/leagues/${encodeURIComponent(leagueId)}/seasons`)
}

/** POST /api/v1/ingestion/sportmonks/seasons/import. */
export async function postSportmonksSeasonImport(
  leagueId: string,
  seasonId: string,
  seedDemoOdds = false,
): Promise<SportmonksSeasonImportResult> {
  return apiPost<SportmonksSeasonImportResult>('/api/v1/ingestion/sportmonks/seasons/import', {
    leagueId,
    seasonId,
    seedDemoOdds,
  })
}

/** POST /api/v1/ingestion/sportmonks/seasons/import-all. */
export async function postSportmonksBulkSeasonImport(
  seasonLabelOrId: string,
  options?: { seedDemoOdds?: boolean; maxLeagues?: number; skipLeagues?: number },
): Promise<SportmonksBulkSeasonImportResult> {
  return apiPost<SportmonksBulkSeasonImportResult>('/api/v1/ingestion/sportmonks/seasons/import-all', {
    seasonLabelOrId,
    seedDemoOdds: options?.seedDemoOdds ?? true,
    maxLeagues: options?.maxLeagues ?? 10,
    skipLeagues: options?.skipLeagues ?? 0,
  })
}

/** POST /api/v1/ingestion/sportmonks/demo-odds/seed. */
export async function postSeedDemoOdds(
  leagueId?: string,
  seasonId?: string,
): Promise<DemoOddsSeedResult> {
  return apiPost<DemoOddsSeedResult>('/api/v1/ingestion/sportmonks/demo-odds/seed', {
    leagueId: leagueId ?? null,
    seasonId: seasonId ?? null,
  })
}

/** GET /api/v1/matches — optionally filtered by status. */
export async function getMatches(status?: string): Promise<MatchSummary[]> {
  const query = status ? `?status=${encodeURIComponent(status)}` : ''
  return apiFetch<MatchSummary[]>(`/api/v1/matches${query}`)
}

/** GET /api/v1/matches/{id}. */
export async function getMatch(id: string): Promise<MatchDetail> {
  return apiFetch<MatchDetail>(`/api/v1/matches/${encodeURIComponent(id)}`)
}

/** GET /api/v1/analytics/matches/{id}. */
export async function getMatchAnalysis(
  id: string,
): Promise<MatchAnalysisReport> {
  return apiFetch<MatchAnalysisReport>(
    `/api/v1/analytics/matches/${encodeURIComponent(id)}`,
  )
}

/** GET /api/v1/analytics/matches/{id}/odds. */
export async function getMatchOdds(id: string): Promise<OddsSnapshot[]> {
  return apiFetch<OddsSnapshot[]>(
    `/api/v1/analytics/matches/${encodeURIComponent(id)}/odds`,
  )
}

/** POST /api/v1/analytics/patterns/query. */
export async function postPatternQuery(
  body: PatternQueryRequest,
): Promise<PatternResult> {
  return apiPost<PatternResult>('/api/v1/analytics/patterns/query', body)
}

/** GET /api/v1/analytics/matches/{id}/similar. */
export async function getSimilarMatches(
  id: string,
  topK = 10,
): Promise<SimilarMatch[]> {
  return apiFetch<SimilarMatch[]>(
    `/api/v1/analytics/matches/${encodeURIComponent(id)}/similar?topK=${topK}`,
  )
}

/** GET /api/v1/analytics/matches/{id}/movement. */
export async function getMatchMovement(id: string): Promise<OddsMovement[]> {
  return apiFetch<OddsMovement[]>(
    `/api/v1/analytics/matches/${encodeURIComponent(id)}/movement`,
  )
}

/** GET /api/v1/analytics/matches/{id}/bookmakers. */
export async function getMatchBookmakers(
  id: string,
): Promise<BookmakerDispersion[]> {
  return apiFetch<BookmakerDispersion[]>(
    `/api/v1/analytics/matches/${encodeURIComponent(id)}/bookmakers`,
  )
}

/** GET /api/v1/analytics/high-odds. */
export async function getHighOddsAnalytics(params: {
  fromUtc?: string
  toUtc?: string
  minOdds?: number
  competitionId?: string
  bookmakerId?: string
  result?: string
  page?: number
  pageSize?: number
}): Promise<HighOddsAnalytics> {
  const query = new URLSearchParams()
  if (params.fromUtc) query.set('fromUtc', params.fromUtc)
  if (params.toUtc) query.set('toUtc', params.toUtc)
  if (params.minOdds !== undefined) query.set('minOdds', String(params.minOdds))
  if (params.competitionId) query.set('competitionId', params.competitionId)
  if (params.bookmakerId) query.set('bookmakerId', params.bookmakerId)
  if (params.result) query.set('result', params.result)
  query.set('page', String(params.page ?? 1))
  query.set('pageSize', String(params.pageSize ?? 100))
  return apiFetch<HighOddsAnalytics>('/api/v1/analytics/high-odds?' + query.toString())
}

/** GET /api/v1/analytics/high-odds/catalog. */
export async function getHighOddsCatalog(
  fromUtc?: string,
  toUtc?: string,
): Promise<HighOddsCatalog> {
  const query = new URLSearchParams()
  if (fromUtc) query.set('fromUtc', fromUtc)
  if (toUtc) query.set('toUtc', toUtc)
  return apiFetch<HighOddsCatalog>('/api/v1/analytics/high-odds/catalog?' + query.toString())
}

/** POST /api/v1/ingestion/catalog. */
export async function postIngestCatalog(
  body: IngestionRequest,
): Promise<unknown> {
  return apiPost<unknown>('/api/v1/ingestion/catalog', body)
}

/** POST /api/v1/ingestion/fixture. */
export async function postIngestFixture(
  body: IngestionRequest,
): Promise<unknown> {
  return apiPost<unknown>('/api/v1/ingestion/fixture', body)
}

/** POST /api/v1/ingestion/odds. */
export async function postIngestOdds(body: IngestionRequest): Promise<unknown> {
  return apiPost<unknown>('/api/v1/ingestion/odds', body)
}

/** POST /api/v1/ingestion/statistics. */
export async function postIngestStatistics(
  body: IngestionRequest,
): Promise<unknown> {
  return apiPost<unknown>('/api/v1/ingestion/statistics', body)
}

/** GET /api/v1/dashboard/summary. */
export async function getDashboardSummary(): Promise<DashboardSummary> {
  return apiFetch<DashboardSummary>('/api/v1/dashboard/summary')
}

/** GET /api/v1/dashboard/recent — most recent matches (defaults to 10). */
export async function getRecentMatches(take = 10): Promise<RecentMatch[]> {
  return apiFetch<RecentMatch[]>(`/api/v1/dashboard/recent?take=${take}`)
}

/** GET /api/v1/dataquality/matches/{id}. */
export async function getDataQuality(id: string): Promise<DataQualityReport> {
  return apiFetch<DataQualityReport>(
    `/api/v1/dataquality/matches/${encodeURIComponent(id)}`,
  )
}

/**
 * Raised when the AI summary cannot be produced because the analytics
 * service is offline (e.g. a 502 from the proxy) or otherwise unreachable.
 */
export class AiSummaryUnavailableError extends Error {
  constructor(message = 'AI summary unavailable (analytics service offline)') {
    super(message)
    this.name = 'AiSummaryUnavailableError'
  }
}

/**
 * GET /api/v1/analytics/matches/{id}/summary.
 *
 * The response is proxied from the Python analytics service. When that service
 * is offline the proxy returns 502 (or the request fails to connect); in those
 * cases this throws {@link AiSummaryUnavailableError} so the caller can show a
 * friendly "AI summary unavailable" message rather than a generic error.
 */
export async function getMatchAiSummary(id: string): Promise<MatchAiSummary> {
  try {
    return await apiFetch<MatchAiSummary>(
      `/api/v1/analytics/matches/${encodeURIComponent(id)}/summary`,
    )
  } catch (error) {
    if (error instanceof ApiError) {
      // 502 Bad Gateway / 503 / 504 indicate the upstream analytics service
      // is unreachable through the proxy.
      if (error.status === 502 || error.status === 503 || error.status === 504) {
        throw new AiSummaryUnavailableError()
      }
      throw error
    }
    // A TypeError from fetch means the network request never completed.
    if (error instanceof TypeError) {
      throw new AiSummaryUnavailableError()
    }
    throw error
  }
}


/** GET /api/v1/analytics/market-outcomes. */
export async function getMarketOutcomeAnalytics(params: {
  matchId?: string
  fromUtc?: string
  toUtc?: string
  marketCode?: string
  minFavoriteOdds?: number
  maxFavoriteOdds?: number
  upsetThreshold?: number
  bookmakerId?: string
  classification?: string
  page?: number
  pageSize?: number
}): Promise<MarketOutcomeAnalytics> {
  const query = new URLSearchParams()
  if (params.matchId) query.set('matchId', params.matchId)
  if (params.fromUtc) query.set('fromUtc', params.fromUtc)
  if (params.toUtc) query.set('toUtc', params.toUtc)
  if (params.marketCode) query.set('marketCode', params.marketCode)
  if (params.minFavoriteOdds !== undefined) query.set('minFavoriteOdds', String(params.minFavoriteOdds))
  if (params.maxFavoriteOdds !== undefined) query.set('maxFavoriteOdds', String(params.maxFavoriteOdds))
  if (params.upsetThreshold !== undefined) query.set('upsetThreshold', String(params.upsetThreshold))
  if (params.bookmakerId) query.set('bookmakerId', params.bookmakerId)
  if (params.classification) query.set('classification', params.classification)
  query.set('page', String(params.page ?? 1))
  query.set('pageSize', String(params.pageSize ?? 50))
  return apiFetch<MarketOutcomeAnalytics>(
    '/api/v1/analytics/market-outcomes?' + query.toString(),
  )
}

/** GET /api/v1/analytics/upcoming. */
export async function getUpcomingAnalysis(params: {
  window?: string
  marketCode?: string
  minimumComparableSamples?: number
} = {}): Promise<UpcomingAnalysis> {
  const query = new URLSearchParams()
  query.set('window', params.window ?? 'today')
  query.set('marketCode', params.marketCode ?? '1X2')
  query.set('minimumComparableSamples', String(params.minimumComparableSamples ?? 5))
  return apiFetch<UpcomingAnalysis>('/api/v1/analytics/upcoming?' + query.toString())
}

/** GET /api/v1/analytics/daily-report. */
export async function getDailyMarketReport(dateUtc?: string): Promise<DailyMarketReport> {
  const query = dateUtc ? `?dateUtc=${encodeURIComponent(dateUtc)}` : ''
  return apiFetch<DailyMarketReport>('/api/v1/analytics/daily-report' + query)
}
