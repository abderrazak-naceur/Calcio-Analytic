import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { DataTable, type Column } from '../components/DataTable'
import { Spinner } from '../components/Spinner'
import { ApiError, getSimilarMatches } from '../lib/apiClient'
import type { SimilarMatch } from '../lib/types'

const TOP_K_OPTIONS = [5, 10, 20, 50] as const

type State =
  | { kind: 'loading' }
  | { kind: 'ready'; matches: SimilarMatch[] }
  | { kind: 'empty' }
  | { kind: 'error'; message: string }

function pct(value: number): string {
  return `${(value * 100).toFixed(1)}%`
}

function SimilarMatches() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [topK, setTopK] = useState<number>(10)
  const [state, setState] = useState<State>({ kind: 'loading' })

  const load = useCallback(async (matchId: string, k: number) => {
    setState({ kind: 'loading' })
    try {
      const matches = await getSimilarMatches(matchId, k)
      setState(
        matches.length === 0
          ? { kind: 'empty' }
          : { kind: 'ready', matches },
      )
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setState({ kind: 'empty' })
        return
      }
      setState({ kind: 'error', message: describeError(error) })
    }
  }, [])

  useEffect(() => {
    if (id) void load(id, topK)
  }, [id, topK, load])

  if (!id) {
    return <p className="text-sm text-red-400">Missing match id.</p>
  }

  const columns: ReadonlyArray<Column<SimilarMatch>> = [
    {
      header: 'Match',
      render: (m) => (
        <Link
          to={`/matches/${m.matchId}`}
          className="text-green-400 transition hover:text-green-300"
        >
          {m.matchId}
        </Link>
      ),
    },
    {
      header: 'Score',
      render: (m) => pct(m.similarityScore),
      className: 'text-right',
    },
    {
      header: 'Explanation',
      render: (m) => m.explanation.join('; '),
    },
  ]

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-white">Similar matches</h1>
          <p className="text-sm text-slate-400">
            Matches most similar to{' '}
            <code className="rounded bg-slate-800 px-1.5 py-0.5 text-slate-300">
              {id}
            </code>
          </p>
        </div>
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-2 text-sm text-slate-300">
            <span className="text-slate-400">Top K</span>
            <select
              value={topK}
              onChange={(e) => setTopK(Number(e.target.value))}
              className="rounded-md border border-slate-700 bg-slate-800 px-3 py-1.5 text-sm text-slate-200 focus:border-green-500 focus:outline-none"
            >
              {TOP_K_OPTIONS.map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </select>
          </label>
          <Link
            to={`/matches/${id}`}
            className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
          >
            ← Back to match
          </Link>
        </div>
      </header>

      <Card>
        {state.kind === 'loading' && (
          <Spinner label="Finding similar matches…" />
        )}

        {state.kind === 'empty' && (
          <p className="text-sm text-slate-400">
            No similar matches found for this match.
          </p>
        )}

        {state.kind === 'error' && (
          <div className="flex items-center justify-between">
            <p className="text-sm text-red-400">
              Could not load similar matches: {state.message}
            </p>
            <button
              type="button"
              onClick={() => void load(id, topK)}
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
            rowKey={(m, i) => `${m.matchId}-${i}`}
            onRowClick={(m) => navigate(`/matches/${m.matchId}`)}
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

export default SimilarMatches
