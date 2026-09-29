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
