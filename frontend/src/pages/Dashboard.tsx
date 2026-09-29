import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Spinner } from '../components/Spinner'
import {
  ApiError,
  getHealth,
  getMatches,
  postIngestCatalog,
  postIngestFixture,
  postIngestOdds,
  postIngestStatistics,
} from '../lib/apiClient'
import type { IngestionRequest, MatchSummary } from '../lib/types'

interface Kpis {
  total: number
  finished: number
  analyzed: number
  live: number
}

type MatchesState =
  | { kind: 'loading' }
  | { kind: 'ready'; kpis: Kpis }
  | { kind: 'error'; message: string }

type HealthState =
  | { kind: 'loading' }
  | { kind: 'healthy'; status: string }
  | { kind: 'error'; message: string }

function computeKpis(matches: MatchSummary[]): Kpis {
  let finished = 0
  let live = 0
  let analyzed = 0
  for (const match of matches) {
    const status = match.status.toLowerCase()
    if (status.includes('finish') || status.includes('final')) finished += 1
    if (status.includes('live')) live += 1
    // "Analyzed" = a match that has a recorded score to analyze.
    if (match.homeScore !== null && match.awayScore !== null) analyzed += 1
  }
  return { total: matches.length, finished, live, analyzed }
}

const INGEST_BODY: IngestionRequest = {
  providerCode: 'mock',
  competitionExternalId: 'serie-a',
  seasonExternalId: 'serie-a-2024-2025',
  matchExternalId: 'match-001',
}

interface IngestStep {
  label: string
  run: (body: IngestionRequest) => Promise<unknown>
}

const INGEST_STEPS: readonly IngestStep[] = [
  { label: 'Catalog', run: postIngestCatalog },
  { label: 'Fixture', run: postIngestFixture },
  { label: 'Odds', run: postIngestOdds },
  { label: 'Statistics', run: postIngestStatistics },
]

type StepStatus =
  | { kind: 'pending' }
  | { kind: 'running' }
  | { kind: 'success' }
  | { kind: 'error'; message: string }

function Dashboard() {
  const [matches, setMatches] = useState<MatchesState>({ kind: 'loading' })
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })
  const [stepStatuses, setStepStatuses] = useState<StepStatus[]>(
    INGEST_STEPS.map(() => ({ kind: 'pending' })),
  )
  const [ingesting, setIngesting] = useState(false)

  const loadKpis = useCallback(async () => {
    setMatches({ kind: 'loading' })
    try {
      const data = await getMatches()
      setMatches({ kind: 'ready', kpis: computeKpis(data) })
    } catch (error) {
      setMatches({ kind: 'error', message: describeError(error) })
    }
  }, [])

  const loadHealth = useCallback(async () => {
    setHealth({ kind: 'loading' })
    try {
      const result = await getHealth()
      setHealth({ kind: 'healthy', status: result.status })
    } catch (error) {
      setHealth({ kind: 'error', message: describeError(error) })
    }
  }, [])

  useEffect(() => {
    void loadKpis()
    void loadHealth()
  }, [loadKpis, loadHealth])

  const runIngestion = useCallback(async () => {
    setIngesting(true)
    const statuses: StepStatus[] = INGEST_STEPS.map(() => ({ kind: 'pending' }))
    setStepStatuses([...statuses])

    for (let i = 0; i < INGEST_STEPS.length; i += 1) {
      statuses[i] = { kind: 'running' }
      setStepStatuses([...statuses])
      try {
        await INGEST_STEPS[i].run(INGEST_BODY)
        statuses[i] = { kind: 'success' }
        setStepStatuses([...statuses])
      } catch (error) {
        statuses[i] = { kind: 'error', message: describeError(error) }
        setStepStatuses([...statuses])
        break
      }
    }

    setIngesting(false)
    await loadKpis()
  }, [loadKpis])

  return (
    <div className="space-y-6">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Dashboard</h1>
          <p className="text-sm text-slate-400">
            Overview of matches and analytics coverage.
          </p>
        </div>
        <HealthChip state={health} />
      </header>

      <KpiRow state={matches} onRetry={() => void loadKpis()} />

      <Card
        title="Ingest demo data"
        actions={
          <button
            type="button"
            onClick={() => void runIngestion()}
            disabled={ingesting}
            className="rounded-md bg-green-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-green-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {ingesting ? 'Running…' : 'Run ingestion'}
          </button>
        }
      >
        <p className="mb-4 text-sm text-slate-400">
          Runs catalog → fixture → odds → statistics for provider{' '}
          <code className="rounded bg-slate-800 px-1.5 py-0.5 text-slate-300">
            mock
          </code>{' '}
          (Serie A 2024–2025, match-001).
        </p>
        <ol className="space-y-2">
          {INGEST_STEPS.map((step, index) => (
            <li key={step.label} className="flex items-center gap-3">
              <StepIcon status={stepStatuses[index]} />
              <span className="w-24 text-sm text-slate-200">{step.label}</span>
              <StepMessage status={stepStatuses[index]} />
            </li>
          ))}
        </ol>
      </Card>
    </div>
  )
}

function KpiRow({
  state,
  onRetry,
}: {
  state: MatchesState
  onRetry: () => void
}) {
  if (state.kind === 'loading') {
    return (
      <Card>
        <Spinner label="Loading KPIs…" />
      </Card>
    )
  }
  if (state.kind === 'error') {
    return (
      <Card>
        <div className="flex items-center justify-between">
          <p className="text-sm text-red-400">
            Could not load matches: {state.message}
          </p>
          <button
            type="button"
            onClick={onRetry}
            className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
          >
            Retry
          </button>
        </div>
      </Card>
    )
  }

  const { kpis } = state
  const items: ReadonlyArray<{ label: string; value: number }> = [
    { label: 'Total matches', value: kpis.total },
    { label: 'Finished', value: kpis.finished },
    { label: 'Analyzed', value: kpis.analyzed },
    { label: 'Live', value: kpis.live },
  ]

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
      {items.map((item) => (
        <div
          key={item.label}
          className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 shadow-lg"
        >
          <p className="text-sm text-slate-400">{item.label}</p>
          <p className="mt-1 text-3xl font-bold text-white">{item.value}</p>
        </div>
      ))}
    </div>
  )
}

function HealthChip({ state }: { state: HealthState }) {
  if (state.kind === 'loading') {
    return (
      <span className="inline-flex items-center gap-2 rounded-full border border-slate-700 bg-slate-800/60 px-3 py-1 text-xs text-slate-300">
        <span className="h-2 w-2 animate-pulse rounded-full bg-amber-400" />
        Checking…
      </span>
    )
  }
  if (state.kind === 'healthy') {
    return (
      <span className="inline-flex items-center gap-2 rounded-full border border-green-500/30 bg-green-500/15 px-3 py-1 text-xs text-green-400">
        <span className="h-2 w-2 rounded-full bg-green-500" />
        API {state.status}
      </span>
    )
  }
  return (
    <span
      title={state.message}
      className="inline-flex items-center gap-2 rounded-full border border-red-500/30 bg-red-500/15 px-3 py-1 text-xs text-red-400"
    >
      <span className="h-2 w-2 rounded-full bg-red-500" />
      API unreachable
    </span>
  )
}

function StepIcon({ status }: { status: StepStatus }) {
  const base = 'flex h-5 w-5 items-center justify-center rounded-full text-xs'
  switch (status.kind) {
    case 'running':
      return (
        <span
          className="h-4 w-4 animate-spin rounded-full border-2 border-slate-600 border-t-green-500"
          aria-hidden="true"
        />
      )
    case 'success':
      return (
        <span className={`${base} bg-green-500/20 text-green-400`}>✓</span>
      )
    case 'error':
      return <span className={`${base} bg-red-500/20 text-red-400`}>✕</span>
    default:
      return <span className={`${base} bg-slate-700/50 text-slate-500`}>•</span>
  }
}

function StepMessage({ status }: { status: StepStatus }) {
  switch (status.kind) {
    case 'running':
      return <span className="text-sm text-slate-400">Running…</span>
    case 'success':
      return <span className="text-sm text-green-400">Done</span>
    case 'error':
      return <span className="text-sm text-red-400">{status.message}</span>
    default:
      return <span className="text-sm text-slate-500">Pending</span>
  }
}

function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    return `${error.message} (HTTP ${error.status})`
  }
  if (error instanceof Error) {
    return error.message
  }
  return 'Unknown error'
}

export default Dashboard
