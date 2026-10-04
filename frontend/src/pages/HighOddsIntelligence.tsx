import { useCallback, useEffect, useMemo, useState } from 'react'
import { Card } from '../components/Card'
import { DataTable, type Column } from '../components/DataTable'
import { Spinner } from '../components/Spinner'
import {
  API_BASE_URL,
  ApiError,
  getHighOddsAnalytics,
  getHighOddsCatalog,
} from '../lib/apiClient'
import type {
  HighOddsAnalytics,
  HighOddsCatalog,
  HighOddsGroupStats,
  HighOddsRangeStats,
  HighOddsSelection,
} from '../lib/types'

type PeriodPreset = '7' | '30' | '90' | '365' | 'custom'

type Filters = {
  preset: PeriodPreset
  from: string
  to: string
  minOdds: string
  competitionId: string
  bookmakerId: string
  result: string
}

function dateInput(date: Date): string {
  return date.toISOString().slice(0, 10)
}

function defaultFilters(): Filters {
  const to = new Date()
  const from = new Date(to)
  from.setUTCDate(from.getUTCDate() - 30)
  return {
    preset: '30',
    from: dateInput(from),
    to: dateInput(to),
    minOdds: '6',
    competitionId: '',
    bookmakerId: '',
    result: '',
  }
}

function utcStart(value: string): string {
  return value ? value + 'T00:00:00.000Z' : ''
}

function utcEnd(value: string): string {
  return value ? value + 'T23:59:59.999Z' : ''
}

function money(value: number): string {
  return (value >= 0 ? '+' : '') + value.toFixed(2) + 'u'
}

function pct(value: number): string {
  return value.toFixed(1) + '%'
}

function odds(value: number): string {
  return value.toFixed(2)
}

function HighOddsIntelligence() {
  const [filters, setFilters] = useState<Filters>(defaultFilters)
  const [catalog, setCatalog] = useState<HighOddsCatalog | null>(null)
  const [data, setData] = useState<HighOddsAnalytics | null>(null)
  const [loading, setLoading] = useState(true)
  const [catalogLoading, setCatalogLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [page, setPage] = useState(1)

  const loadCatalog = useCallback(async () => {
    setCatalogLoading(true)
    try {
      setCatalog(await getHighOddsCatalog(utcStart(filters.from), utcEnd(filters.to)))
    } catch {
      setCatalog(null)
    } finally {
      setCatalogLoading(false)
    }
  }, [filters.from, filters.to])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await getHighOddsAnalytics({
        fromUtc: utcStart(filters.from),
        toUtc: utcEnd(filters.to),
        minOdds: Number(filters.minOdds) || 6,
        competitionId: filters.competitionId || undefined,
        bookmakerId: filters.bookmakerId || undefined,
        result: filters.result || undefined,
        page,
        pageSize: 100,
      })
      setData(result)
    } catch (err) {
      setData(null)
      setError(describeError(err))
    } finally {
      setLoading(false)
    }
  }, [filters, page])

  useEffect(() => {
    void loadCatalog()
  }, [loadCatalog])

  useEffect(() => {
    void load()
  }, [load])

  const applyPreset = (preset: PeriodPreset) => {
    if (preset === 'custom') {
      setFilters((current) => ({ ...current, preset }))
      return
    }
    const to = new Date()
    const from = new Date(to)
    from.setUTCDate(from.getUTCDate() - Number(preset))
    setFilters((current) => ({
      ...current,
      preset,
      from: dateInput(from),
      to: dateInput(to),
    }))
  }

  const exportUrl = useMemo(() => {
    const params = new URLSearchParams({
      fromUtc: utcStart(filters.from),
      toUtc: utcEnd(filters.to),
      minOdds: filters.minOdds || '6',
    })
    if (filters.competitionId) params.set('competitionId', filters.competitionId)
    if (filters.bookmakerId) params.set('bookmakerId', filters.bookmakerId)
    if (filters.result) params.set('result', filters.result)
    return API_BASE_URL + '/api/v1/analytics/high-odds/export?' + params.toString()
  }, [filters])

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-[0.18em] text-green-400">
            <span className="h-2 w-2 rounded-full bg-green-500" />
            Historical Intelligence
          </div>
          <h1 className="text-3xl font-bold tracking-tight text-white">
            High Odds Intelligence
          </h1>
          <p className="mt-1 max-w-3xl text-sm text-slate-400">
            Find historical 1X2 selections with high odds, measure hit rate,
            flat-stake ROI, drawdown, streaks and opening-to-closing movement.
          </p>
        </div>
        <a
          href={exportUrl}
          className="rounded-lg border border-slate-700 bg-slate-900 px-4 py-2 text-sm font-semibold text-slate-200 transition hover:border-green-500/50 hover:text-white"
        >
          Export CSV / Excel
        </a>
      </header>

      <FilterPanel
        filters={filters}
        catalog={catalog}
        catalogLoading={catalogLoading}
        onChange={(next) => { setPage(1); setFilters(next) }}
        onPreset={(preset) => { setPage(1); applyPreset(preset) }}
      />

      {loading && <Card><Spinner label="Analysing historical odds…" /></Card>}

      {!loading && error && (
        <Card>
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="font-medium text-red-400">Analytics unavailable</p>
              <p className="mt-1 text-sm text-slate-400">{error}</p>
            </div>
            <button
              type="button"
              onClick={() => void load()}
              className="rounded-md bg-slate-800 px-3 py-2 text-sm text-slate-200 hover:bg-slate-700"
            >
              Retry
            </button>
          </div>
        </Card>
      )}

      {!loading && !error && data && (
        <>
          <KpiGrid data={data} />
          <div className="grid gap-6 xl:grid-cols-[1.35fr_0.65fr]">
            <OddsRangeChart ranges={data.byOddsRange} />
            <SelectionBreakdown groups={data.bySelection} />
          </div>
          <HighOddsTable rows={data.results} total={data.totalResults} page={page} pageSize={data.pageSize} onPageChange={setPage} />
          <div className="grid gap-6 xl:grid-cols-2">
            <BookmakerTable groups={data.byBookmaker} />
            <ResearchNotes data={data} />
          </div>
        </>
      )}
    </div>
  )
}

function FilterPanel({
  filters,
  catalog,
  catalogLoading,
  onChange,
  onPreset,
}: {
  filters: Filters
  catalog: HighOddsCatalog | null
  catalogLoading: boolean
  onChange: (next: Filters) => void
  onPreset: (preset: PeriodPreset) => void
}) {
  const input =
    'mt-1 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-slate-200 outline-none transition focus:border-green-500'

  return (
    <Card>
      <div className="space-y-4">
        <div className="flex flex-wrap gap-2">
          {(['7', '30', '90', '365'] as const).map((preset) => (
            <button
              key={preset}
              type="button"
              onClick={() => onPreset(preset)}
              className={
                'rounded-full px-3 py-1.5 text-xs font-semibold ' +
                (filters.preset === preset
                  ? 'bg-green-500 text-slate-950'
                  : 'bg-slate-800 text-slate-300 hover:bg-slate-700')
              }
            >
              {preset === '365' ? '1 year' : preset + ' days'}
            </button>
          ))}
          <button
            type="button"
            onClick={() => onPreset('custom')}
            className={
              'rounded-full px-3 py-1.5 text-xs font-semibold ' +
              (filters.preset === 'custom'
                ? 'bg-green-500 text-slate-950'
                : 'bg-slate-800 text-slate-300 hover:bg-slate-700')
            }
          >
            Custom
          </button>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-6">
          <label className="text-xs font-medium text-slate-400">
            From
            <input
              type="date"
              className={input}
              value={filters.from}
              onChange={(e) =>
                onChange({ ...filters, preset: 'custom', from: e.target.value })
              }
            />
          </label>
          <label className="text-xs font-medium text-slate-400">
            To
            <input
              type="date"
              className={input}
              value={filters.to}
              onChange={(e) =>
                onChange({ ...filters, preset: 'custom', to: e.target.value })
              }
            />
          </label>
          <label className="text-xs font-medium text-slate-400">
            Minimum odds
            <select
              className={input}
              value={filters.minOdds}
              onChange={(e) => onChange({ ...filters, minOdds: e.target.value })}
            >
              {[6, 7, 8, 10, 15, 20].map((value) => (
                <option key={value} value={value}>{'≥ ' + value.toFixed(2)}</option>
              ))}
            </select>
          </label>
          <label className="text-xs font-medium text-slate-400">
            Competition
            <select
              className={input}
              value={filters.competitionId}
              onChange={(e) => onChange({ ...filters, competitionId: e.target.value })}
            >
              <option value="">All competitions</option>
              {catalog?.competitions.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </label>
          <label className="text-xs font-medium text-slate-400">
            Bookmaker
            <select
              className={input}
              value={filters.bookmakerId}
              onChange={(e) => onChange({ ...filters, bookmakerId: e.target.value })}
            >
              <option value="">All bookmakers</option>
              {catalog?.bookmakers.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </label>
          <label className="text-xs font-medium text-slate-400">
            Result
            <select
              className={input}
              value={filters.result}
              onChange={(e) => onChange({ ...filters, result: e.target.value })}
            >
              <option value="">All results</option>
              <option value="Home">Home win</option>
              <option value="Draw">Draw</option>
              <option value="Away">Away win</option>
            </select>
          </label>
        </div>

        {catalogLoading && (
          <p className="text-xs text-slate-500">Loading competition/bookmaker filters…</p>
        )}
      </div>
    </Card>
  )
}

function KpiGrid({ data }: { data: HighOddsAnalytics }) {
  const items = [
    ['Matches', data.summary.uniqueMatches.toLocaleString()],
    ['High-odds selections', data.summary.qualifyingSelections.toLocaleString()],
    ['Win rate', pct(data.summary.winRatePercentage)],
    ['Average odds', odds(data.summary.averageOdds)],
    ['Profit', money(data.summary.profitUnits)],
    ['ROI', pct(data.summary.roiPercentage)],
    ['Max drawdown', '-' + data.summary.maxDrawdownUnits.toFixed(2) + 'u'],
    ['Max losing streak', String(data.summary.maxLosingStreak)],
  ]

  return (
    <div className="grid grid-cols-2 gap-3 md:grid-cols-4 xl:grid-cols-8">
      {items.map(([label, value]) => (
        <div key={label} className="rounded-xl border border-slate-800 bg-slate-900/70 p-4">
          <p className="text-xs text-slate-500">{label}</p>
          <p
            className={
              'mt-1 text-xl font-bold ' +
              ((label === 'ROI' || label === 'Profit') &&
              data.summary.profitUnits < 0
                ? 'text-red-400'
                : label === 'ROI' || label === 'Profit'
                  ? 'text-green-400'
                  : 'text-white')
            }
          >
            {value}
          </p>
        </div>
      ))}
    </div>
  )
}

function OddsRangeChart({ ranges }: { ranges: HighOddsRangeStats[] }) {
  const maxCount = Math.max(1, ...ranges.map((x) => x.selections))
  return (
    <Card title="Performance by odds range">
      <div className="space-y-4">
        {ranges.map((range) => (
          <div key={range.range}>
            <div className="mb-1 flex items-center justify-between text-xs">
              <span className="font-semibold text-slate-300">{range.range}</span>
              <span className="text-slate-500">
                {range.selections} · {pct(range.winRatePercentage)} · {money(range.profitUnits)}
              </span>
            </div>
            <div className="h-3 overflow-hidden rounded-full bg-slate-800">
              <div
                className="h-full rounded-full bg-green-500/80"
                style={{ width: (range.selections / maxCount) * 100 + '%' }}
              />
            </div>
          </div>
        ))}
      </div>
      <p className="mt-5 text-xs text-slate-500">
        Rare high-odds ranges are inherently noisy; use the sample size before
        interpreting ROI.
      </p>
    </Card>
  )
}

function SelectionBreakdown({ groups }: { groups: HighOddsGroupStats[] }) {
  return (
    <Card title="1X2 breakdown">
      <div className="space-y-3">
        {groups.map((group) => (
          <div key={group.group} className="rounded-lg border border-slate-800 bg-slate-950/60 p-3">
            <div className="flex items-center justify-between">
              <span className="font-semibold text-slate-200">{group.group}</span>
              <span className="text-sm font-semibold text-white">{pct(group.winRatePercentage)}</span>
            </div>
            <div className="mt-2 flex justify-between text-xs text-slate-500">
              <span>{group.wins + '/' + group.selections + ' wins'}</span>
              <span>{money(group.profitUnits)}</span>
            </div>
          </div>
        ))}
      </div>
    </Card>
  )
}

function HighOddsTable({ rows, total, page, pageSize, onPageChange }: { rows: HighOddsSelection[]; total: number; page: number; pageSize: number; onPageChange: (page: number) => void }) {
  const columns: ReadonlyArray<Column<HighOddsSelection>> = [
    {
      header: 'Date',
      render: (row) => new Date(row.kickoffUtc).toLocaleDateString(),
    },
    {
      header: 'Match',
      render: (row) => (
        <div>
          <div className="font-medium text-slate-200">{row.homeTeamName}</div>
          <div className="text-xs text-slate-500">{row.awayTeamName}</div>
        </div>
      ),
    },
    { header: 'Pick', render: (row) => row.selection },
    {
      header: 'Odds',
      render: (row) => <span className="font-bold text-green-400">{odds(row.odds)}</span>,
      className: 'text-right',
    },
    { header: 'Bookmaker', render: (row) => row.bookmakerName },
    {
      header: 'Result',
      render: (row) => (
        <span className={row.won ? 'font-semibold text-green-400' : 'text-red-400'}>
          {row.result + (row.won ? ' ✓' : ' ✕')}
        </span>
      ),
    },
    {
      header: 'P/L',
      render: (row) => (
        <span className={row.profitUnits >= 0 ? 'text-green-400' : 'text-red-400'}>
          {money(row.profitUnits)}
        </span>
      ),
      className: 'text-right',
    },
    {
      header: 'Move',
      render: (row) =>
        row.movementPercentage === null
          ? '—'
          : (row.movementPercentage >= 0 ? '+' : '') +
            row.movementPercentage.toFixed(1) +
            '%',
      className: 'text-right',
    },
  ]

  return (
    <Card title={'Historical high-odds matches (' + total.toLocaleString() + ')'}>
      <DataTable
        columns={columns}
        rows={rows}
        rowKey={(row) => row.matchId + '-' + row.selection + '-' + row.bookmakerId}
        emptyMessage="No high-odds selections found for the selected period."
      />
      <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
        <p className="text-xs text-slate-500">
          Displayed odds are the best pre-kickoff closing price available in the
          stored bookmaker snapshots. One flat unit is used for the historical P/L simulation.
        </p>
        <div className="flex items-center gap-2">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => onPageChange(Math.max(1, page - 1))}
            className="rounded-md bg-slate-800 px-3 py-1.5 text-xs text-slate-200 disabled:opacity-40"
          >
            Previous
          </button>
          <span className="text-xs text-slate-500">
            Page {page} / {Math.max(1, Math.ceil(total / pageSize))}
          </span>
          <button
            type="button"
            disabled={page >= Math.ceil(total / pageSize)}
            onClick={() => onPageChange(page + 1)}
            className="rounded-md bg-slate-800 px-3 py-1.5 text-xs text-slate-200 disabled:opacity-40"
          >
            Next
          </button>
        </div>
      </div>
    </Card>
  )
}

function BookmakerTable({ groups }: { groups: HighOddsGroupStats[] }) {
  const columns: ReadonlyArray<Column<HighOddsGroupStats>> = [
    { header: 'Bookmaker', render: (row) => row.group },
    { header: 'Selections', render: (row) => row.selections, className: 'text-right' },
    { header: 'Wins', render: (row) => row.wins, className: 'text-right' },
    { header: 'Hit rate', render: (row) => pct(row.winRatePercentage), className: 'text-right' },
    { header: 'ROI', render: (row) => pct(row.roiPercentage), className: 'text-right' },
    { header: 'Profit', render: (row) => money(row.profitUnits), className: 'text-right' },
  ]
  return (
    <Card title="Performance by bookmaker">
      <DataTable columns={columns} rows={groups} rowKey={(row) => row.group} />
    </Card>
  )
}

function ResearchNotes({ data }: { data: HighOddsAnalytics }) {
  return (
    <Card title="Research guardrails">
      <ul className="space-y-3 text-sm leading-relaxed text-slate-400">
        <li>
          <strong className="text-slate-200">Historical, not predictive:</strong>{' '}
          a high odd is a price observation, not proof of value or future profitability.
        </li>
        <li>
          <strong className="text-slate-200">Point-in-time:</strong>{' '}
          only pre-kickoff odds snapshots are eligible for this screen.
        </li>
        <li>
          <strong className="text-slate-200">Flat staking:</strong>{' '}
          ROI is simulated at one unit per qualifying selection; no bankroll growth is assumed.
        </li>
        <li>
          <strong className="text-slate-200">Sample size:</strong>{' '}
          {data.summary.qualifyingSelections.toLocaleString()} qualifying selections are
          currently in the selected slice.
        </li>
      </ul>
    </Card>
  )
}

function describeError(error: unknown): string {
  if (error instanceof ApiError) return error.message + ' (HTTP ' + error.status + ')'
  if (error instanceof Error) return error.message
  return 'Unknown error'
}

export default HighOddsIntelligence
