import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { DataTable, type Column } from '../components/DataTable'
import { MatchPicker } from '../components/MatchPicker'
import { Spinner } from '../components/Spinner'
import { ApiError, getMatchMovement } from '../lib/apiClient'
import type { OddsMovement } from '../lib/types'

type State =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'ready'; movements: OddsMovement[] }
  | { kind: 'empty' }
  | { kind: 'error'; message: string }

/** Sortable columns (numeric movement fields, grouping ids, and biggest mover). */
type SortKey =
  | 'bookmakerId'
  | 'marketLineId'
  | 'selectionId'
  | 'openingOdds'
  | 'closingOdds'
  | 'minOdds'
  | 'maxOdds'
  | 'numberOfChanges'
  | 'movementAbsolute'
  | 'movementPercentage'
  | 'volatility'
  | 'biggestMover'

type SortDirection = 'asc' | 'desc'

const SORT_OPTIONS: ReadonlyArray<{ value: SortKey; label: string }> = [
  { value: 'biggestMover', label: 'Biggest movers (|Δ %|)' },
  { value: 'bookmakerId', label: 'Bookmaker' },
  { value: 'marketLineId', label: 'Market' },
  { value: 'selectionId', label: 'Selection' },
  { value: 'openingOdds', label: 'Open' },
  { value: 'closingOdds', label: 'Close' },
  { value: 'minOdds', label: 'Min' },
  { value: 'maxOdds', label: 'Max' },
  { value: 'numberOfChanges', label: 'Changes' },
  { value: 'movementAbsolute', label: 'Δ abs' },
  { value: 'movementPercentage', label: 'Δ %' },
  { value: 'volatility', label: 'Volatility' },
]

function num(value: number, digits = 2): string {
  return Number.isFinite(value) ? value.toFixed(digits) : '—'
}

function pct(value: number): string {
  return Number.isFinite(value) ? `${(value * 100).toFixed(2)}%` : '—'
}

function sortValue(row: OddsMovement, key: SortKey): string | number {
  switch (key) {
    case 'bookmakerId':
      return row.bookmakerId
    case 'marketLineId':
      return row.marketLineId
    case 'selectionId':
      return row.selectionId
    case 'biggestMover':
      return Math.abs(row.movement.movementPercentage)
    default:
      return row.movement[key]
  }
}

function OddsMovementExplorer() {
  const [searchParams, setSearchParams] = useSearchParams()
  const matchId = searchParams.get('matchId') ?? ''
  const [state, setState] = useState<State>({ kind: 'idle' })
  const [sortKey, setSortKey] = useState<SortKey>('biggestMover')
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
      const movements = await getMatchMovement(id)
      setState(
        movements.length === 0
          ? { kind: 'empty' }
          : { kind: 'ready', movements },
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
    if (matchId) void load(matchId)
    else setState({ kind: 'idle' })
  }, [matchId, load])

  const sorted = useMemo(() => {
    if (state.kind !== 'ready') return []
    const rows = [...state.movements]
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

  const columns: ReadonlyArray<Column<OddsMovement>> = [
    { header: 'Bookmaker', render: (m) => m.bookmakerId },
    { header: 'Market', render: (m) => m.marketLineId },
    { header: 'Selection', render: (m) => m.selectionId },
    {
      header: 'Open',
      render: (m) => num(m.movement.openingOdds),
      className: 'text-right',
    },
    {
      header: 'Close',
      render: (m) => num(m.movement.closingOdds),
      className: 'text-right',
    },
    {
      header: 'Min',
      render: (m) => num(m.movement.minOdds),
      className: 'text-right',
    },
    {
      header: 'Max',
      render: (m) => num(m.movement.maxOdds),
      className: 'text-right',
    },
    {
      header: 'Changes',
      render: (m) => m.movement.numberOfChanges,
      className: 'text-right',
    },
    {
      header: 'Δ abs',
      render: (m) => num(m.movement.movementAbsolute),
      className: 'text-right',
    },
    {
      header: 'Δ %',
      render: (m) => pct(m.movement.movementPercentage),
      className: 'text-right',
    },
    {
      header: 'Volatility',
      render: (m) => num(m.movement.volatility, 4),
      className: 'text-right',
    },
  ]

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-white">Odds Movement Explorer</h1>
        <p className="text-sm text-slate-400">
          Track how odds moved between opening and closing for a match. Sorted by
          the biggest movers by default.
        </p>
      </header>

      <Card title="Match">
        <MatchPicker value={matchId} onChange={setMatchId} />
      </Card>

      <Card
        title="Movement"
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
            Select a match to view its odds movement.
          </p>
        )}

        {state.kind === 'loading' && <Spinner label="Loading movement…" />}

        {state.kind === 'empty' && (
          <p className="text-sm text-slate-400">
            No odds movement data for this match.
          </p>
        )}

        {state.kind === 'error' && (
          <div className="flex items-center justify-between">
            <p className="text-sm text-red-400">
              Could not load movement: {state.message}
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
            rowKey={(m, i) =>
              `${m.bookmakerId}-${m.marketLineId}-${m.selectionId}-${i}`
            }
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

export default OddsMovementExplorer
