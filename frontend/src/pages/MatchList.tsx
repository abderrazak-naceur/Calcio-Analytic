import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card } from '../components/Card'
import { DataTable, type Column } from '../components/DataTable'
import { Spinner } from '../components/Spinner'
import { StatusBadge } from '../components/StatusBadge'
import { ApiError, getMatches } from '../lib/apiClient'
import type { MatchSummary } from '../lib/types'

type ListState =
  | { kind: 'loading' }
  | { kind: 'ready'; matches: MatchSummary[] }
  | { kind: 'error'; message: string }

const STATUS_FILTERS = [
  { value: '', label: 'All statuses' },
  { value: 'Scheduled', label: 'Scheduled' },
  { value: 'Live', label: 'Live' },
  { value: 'Finished', label: 'Finished' },
] as const

function formatKickoff(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString()
}

function formatScore(match: MatchSummary): string {
  if (match.homeScore === null || match.awayScore === null) return '—'
  return `${match.homeScore} – ${match.awayScore}`
}

function MatchList() {
  const navigate = useNavigate()
  const [state, setState] = useState<ListState>({ kind: 'loading' })
  const [status, setStatus] = useState<string>('')

  const load = useCallback(async (statusFilter: string) => {
    setState({ kind: 'loading' })
    try {
      const matches = await getMatches(statusFilter || undefined)
      setState({ kind: 'ready', matches })
    } catch (error) {
      setState({ kind: 'error', message: describeError(error) })
    }
  }, [])

  useEffect(() => {
    void load(status)
  }, [load, status])

  const columns: ReadonlyArray<Column<MatchSummary>> = [
    { header: 'Kickoff', render: (m) => formatKickoff(m.kickoffUtc) },
    { header: 'Home', render: (m) => m.homeTeamId },
    { header: 'Away', render: (m) => m.awayTeamId },
    {
      header: 'Score',
      render: (m) => formatScore(m),
      className: 'text-center',
    },
    { header: 'Status', render: (m) => <StatusBadge status={m.status} /> },
  ]

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-white">Matches</h1>
          <p className="text-sm text-slate-400">
            Browse matches and open one for full analytics.
          </p>
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-300">
          <span className="text-slate-400">Status</span>
          <select
            value={status}
            onChange={(event) => setStatus(event.target.value)}
            className="rounded-md border border-slate-700 bg-slate-800 px-3 py-1.5 text-sm text-slate-200 focus:border-green-500 focus:outline-none"
          >
            {STATUS_FILTERS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
      </header>

      <Card>
        {state.kind === 'loading' && <Spinner label="Loading matches…" />}

        {state.kind === 'error' && (
          <div className="flex items-center justify-between">
            <p className="text-sm text-red-400">
              Could not load matches: {state.message}
            </p>
            <button
              type="button"
              onClick={() => void load(status)}
              className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
            >
              Retry
            </button>
          </div>
        )}

        {state.kind === 'ready' && (
          <DataTable
            columns={columns}
            rows={state.matches}
            rowKey={(m) => m.id}
            emptyMessage="No matches found. Run ingestion from the Dashboard."
            onRowClick={(m) => navigate(`/matches/${m.id}`)}
          />
        )}
      </Card>
    </div>
  )
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

export default MatchList
