import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { DataTable, type Column } from '../components/DataTable'
import { MatchPicker } from '../components/MatchPicker'
import { Spinner } from '../components/Spinner'
import { ApiError, getMatchBookmakers } from '../lib/apiClient'
import type { BookmakerDispersion } from '../lib/types'

type State =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'ready'; rows: BookmakerDispersion[] }
  | { kind: 'empty' }
  | { kind: 'error'; message: string }

type SortKey =
  | 'marketLineId'
  | 'selectionId'
  | 'bookmakerCount'
  | 'bestOdds'
  | 'worstOdds'
  | 'averageOdds'
  | 'dispersion'
  | 'spread'

type SortDirection = 'asc' | 'desc'

const SORT_OPTIONS: ReadonlyArray<{ value: SortKey; label: string }> = [
  { value: 'spread', label: 'Spread (best − worst)' },
  { value: 'marketLineId', label: 'Market' },
  { value: 'selectionId', label: 'Selection' },
  { value: 'bookmakerCount', label: 'Bookmakers' },
  { value: 'bestOdds', label: 'Best' },
  { value: 'worstOdds', label: 'Worst' },
  { value: 'averageOdds', label: 'Average' },
  { value: 'dispersion', label: 'Dispersion' },
]

function num(value: number, digits = 2): string {
  return Number.isFinite(value) ? value.toFixed(digits) : '—'
}

/** Spread between the best and worst available odds for a selection. */
function spread(row: BookmakerDispersion): number {
  return row.dispersion.bestOdds - row.dispersion.worstOdds
}

function sortValue(row: BookmakerDispersion, key: SortKey): string | number {
  switch (key) {
    case 'marketLineId':
      return row.marketLineId
    case 'selectionId':
      return row.selectionId
    case 'spread':
      return spread(row)
    default:
      return row.dispersion[key]
  }
}

function BookmakerComparison() {
  const [searchParams, setSearchParams] = useSearchParams()
  const matchId = searchParams.get('matchId') ?? ''
  const [state, setState] = useState<State>({ kind: 'idle' })
  const [sortKey, setSortKey] = useState<SortKey>('spread')
  const [sortDir, setSortDir] = useState<SortDirection>('desc')

  const setMatchId = useCallback(
    (id: string) => {
      const next = new URLSearchParams(searchParams)
      if (id) next.set('matchId', id)
      else next.delete('matchId')
      setSearchParams(next, { replace: true })
    },
    [searchParams, setSearchParams],
  )

  const load = useCallback(async (id: string) => {
    setState({ kind: 'loading' })
    try {
      const rows = await getMatchBookmakers(id)
      setState(rows.length === 0 ? { kind: 'empty' } : { kind: 'ready', rows })
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setState({ kind: 'empty' })
        return
      }
      setState({ kind: 'error', message: describeError(error) })
    }
  }, [])

  useEffect(() => {
    if (matchId) void load(matchId)
    else setState({ kind: 'idle' })
  }, [matchId, load])

  const sorted = useMemo(() => {
    if (state.kind !== 'ready') return []
    const rows = [...state.rows]
    rows.sort((a, b) => {
      const av = sortValue(a, sortKey)
      const bv = sortValue(b, sortKey)
      const cmp =
        typeof av === 'number' && typeof bv === 'number'
          ? av - bv
          : String(av).localeCompare(String(bv))
      return sortDir === 'asc' ? cmp : -cmp
    })
    return rows
  }, [state, sortKey, sortDir])

  const columns: ReadonlyArray<Column<BookmakerDispersion>> = [
    { header: 'Market', render: (r) => r.marketLineId },
    { header: 'Selection', render: (r) => r.selectionId },
    {
      header: 'Bookmakers',
      render: (r) => r.dispersion.bookmakerCount,
      className: 'text-right',
    },
    {
      header: 'Best',
      render: (r) => num(r.dispersion.bestOdds),
      className: 'text-right',
    },
    {
      header: 'Worst',
      render: (r) => num(r.dispersion.worstOdds),
      className: 'text-right',
    },
    {
      header: 'Average',
      render: (r) => num(r.dispersion.averageOdds),
      className: 'text-right',
    },
    {
      header: 'Spread',
      render: (r) => num(spread(r)),
      className: 'text-right',
    },
    {
      header: 'Dispersion',
      render: (r) => num(r.dispersion.dispersion, 4),
      className: 'text-right',
    },
  ]

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-white">Bookmaker Comparison</h1>
        <p className="text-sm text-slate-400">
          Compare best, worst and average odds across bookmakers per selection,
          with the best-minus-worst spread. Sorted by widest spread by default.
        </p>
      </header>

      <Card title="Match">
        <MatchPicker value={matchId} onChange={setMatchId} />
      </Card>

      <Card
        title="Bookmaker dispersion"
        actions={
          state.kind === 'ready' ? (
            <div className="flex items-center gap-2">
              <label className="flex items-center gap-2 text-sm text-slate-300">
                <span className="text-slate-400">Sort by</span>
                <select
                  value={sortKey}
                  onChange={(event) =>
                    setSortKey(event.target.value as SortKey)
                  }
                  className="rounded-md border border-slate-700 bg-slate-800 px-3 py-1.5 text-sm text-slate-200 focus:border-green-500 focus:outline-none"
                >
                  {SORT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <button
                type="button"
                onClick={() =>
                  setSortDir((dir) => (dir === 'asc' ? 'desc' : 'asc'))
                }
                className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
                title="Toggle sort direction"
              >
                {sortDir === 'asc' ? 'Asc ▲' : 'Desc ▼'}
              </button>
            </div>
          ) : undefined
        }
      >
        {state.kind === 'idle' && (
          <p className="text-sm text-slate-400">
            Select a match to compare bookmaker odds.
          </p>
        )}

        {state.kind === 'loading' && (
          <Spinner label="Loading bookmaker comparison…" />
        )}

        {state.kind === 'empty' && (
          <p className="text-sm text-slate-400">
            No bookmaker dispersion data for this match.
          </p>
        )}

        {state.kind === 'error' && (
          <div className="flex items-center justify-between">
            <p className="text-sm text-red-400">
              Could not load bookmaker comparison: {state.message}
            </p>
            <button
              type="button"
              onClick={() => void load(matchId)}
              className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
            >
              Retry
            </button>
          </div>
        )}

        {state.kind === 'ready' && (
          <DataTable
            columns={columns}
            rows={sorted}
            rowKey={(r, i) => `${r.marketLineId}-${r.selectionId}-${i}`}
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

export default BookmakerComparison
