import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { DataTable, type Column } from '../components/DataTable'
import { Spinner } from '../components/Spinner'
import { StatusBadge } from '../components/StatusBadge'
import { Tabs } from '../components/Tabs'
import {
  ApiError,
  getMatchAnalysis,
  getMatchOdds,
} from '../lib/apiClient'
import type {
  BookmakerDispersion,
  MarketAnalysis,
  MatchAnalysisReport,
  OddsMovement,
  OddsSnapshot,
} from '../lib/types'

const TABS = [
  'Overview',
  'Odds',
  'Movement',
  'Bookmakers',
  'Markets',
  'Statistics',
] as const

type Tab = (typeof TABS)[number]

type ReportState =
  | { kind: 'loading' }
  | { kind: 'ready'; report: MatchAnalysisReport }
  | { kind: 'empty' }
  | { kind: 'error'; message: string }

type OddsState =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'ready'; odds: OddsSnapshot[] }
  | { kind: 'empty' }
  | { kind: 'error'; message: string }

const EMPTY_MESSAGE = 'No data yet — run ingestion from the Dashboard.'

function pct(value: number): string {
  return `${(value * 100).toFixed(1)}%`
}

function num(value: number, digits = 2): string {
  return value.toFixed(digits)
}

function formatTimestamp(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString()
}

function MatchDetail() {
  const { id } = useParams<{ id: string }>()
  const [tab, setTab] = useState<Tab>('Overview')
  const [report, setReport] = useState<ReportState>({ kind: 'loading' })
  const [odds, setOdds] = useState<OddsState>({ kind: 'idle' })

  const loadReport = useCallback(async (matchId: string) => {
    setReport({ kind: 'loading' })
    try {
      const data = await getMatchAnalysis(matchId)
      setReport({ kind: 'ready', report: data })
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setReport({ kind: 'empty' })
        return
      }
      setReport({ kind: 'error', message: describeError(error) })
    }
  }, [])

  const loadOdds = useCallback(async (matchId: string) => {
    setOdds({ kind: 'loading' })
    try {
      const data = await getMatchOdds(matchId)
      setOdds(
        data.length === 0 ? { kind: 'empty' } : { kind: 'ready', odds: data },
      )
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        setOdds({ kind: 'empty' })
        return
      }
      setOdds({ kind: 'error', message: describeError(error) })
    }
  }, [])

  useEffect(() => {
    if (id) void loadReport(id)
  }, [id, loadReport])

  useEffect(() => {
    if (id && tab === 'Odds' && odds.kind === 'idle') {
      void loadOdds(id)
    }
  }, [id, tab, odds.kind, loadOdds])

  if (!id) {
    return <p className="text-sm text-red-400">Missing match id.</p>
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-white">Match detail</h1>
          <p className="text-sm text-slate-400">
            <code className="rounded bg-slate-800 px-1.5 py-0.5 text-slate-300">
              {id}
            </code>
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Link
            to={`/matches/${id}/similar`}
            className="rounded-md bg-green-600 px-3 py-1.5 text-sm font-medium text-white transition hover:bg-green-500"
          >
            Find similar matches
          </Link>
          <Link
            to="/matches"
            className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
          >
            ← Back to matches
          </Link>
        </div>
      </header>

      <Card>
        <Tabs tabs={TABS} active={tab} onChange={(t) => setTab(t as Tab)} />
        <div className="pt-6">
          {tab === 'Odds' ? (
            <OddsTab state={odds} onRetry={() => void loadOdds(id)} />
          ) : (
            <ReportTab
              tab={tab}
              state={report}
              onRetry={() => void loadReport(id)}
            />
          )}
        </div>
      </Card>
    </div>
  )
}

function ReportTab({
  tab,
  state,
  onRetry,
}: {
  tab: Exclude<Tab, 'Odds'>
  state: ReportState
  onRetry: () => void
}) {
  if (state.kind === 'loading') return <Spinner label="Loading analysis…" />
  if (state.kind === 'empty')
    return <p className="text-sm text-slate-400">{EMPTY_MESSAGE}</p>
  if (state.kind === 'error') {
    return (
      <div className="flex items-center justify-between">
        <p className="text-sm text-red-400">
          Could not load analysis: {state.message}
        </p>
        <button
          type="button"
          onClick={onRetry}
          className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
        >
          Retry
        </button>
      </div>
    )
  }

  const { report } = state
  switch (tab) {
    case 'Overview':
      return <OverviewPanel report={report} />
    case 'Movement':
      return <MovementPanel movements={report.oddsMovements} />
    case 'Bookmakers':
      return <BookmakersPanel dispersions={report.bookmakerDispersions} />
    case 'Markets':
      return <MarketsPanel markets={report.markets} />
    case 'Statistics':
      return <StatisticsPanel statistics={report.statistics} />
    default:
      return null
  }
}

function OverviewPanel({ report }: { report: MatchAnalysisReport }) {
  const { result } = report
  const score =
    result.homeScore === null || result.awayScore === null
      ? '—'
      : `${result.homeScore} – ${result.awayScore}`
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-6">
        <div>
          <p className="text-sm text-slate-400">Score</p>
          <p className="text-3xl font-bold text-white">{score}</p>
        </div>
        <div>
          <p className="text-sm text-slate-400">Outcome</p>
          <div className="mt-1">
            <StatusBadge status={result.outcome} />
          </div>
        </div>
      </div>
      <dl className="grid grid-cols-1 gap-2 text-sm sm:grid-cols-2">
        <Field label="Match" value={result.matchId} />
        <Field label="Home team" value={result.homeTeamId} />
        <Field label="Away team" value={result.awayTeamId} />
        <Field label="Methodology" value={report.methodologyVersion} />
        <Field
          label="Generated"
          value={formatTimestamp(report.generatedAtUtc)}
        />
      </dl>
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 border-b border-slate-800/60 py-1.5">
      <dt className="text-slate-400">{label}</dt>
      <dd className="text-right text-slate-200">{value}</dd>
    </div>
  )
}

function MarketsPanel({ markets }: { markets: MarketAnalysis[] }) {
  if (markets.length === 0)
    return <p className="text-sm text-slate-400">{EMPTY_MESSAGE}</p>

  return (
    <div className="space-y-8">
      {markets.map((market) => {
        const columns: ReadonlyArray<Column<MarketAnalysis['selections'][number]>> =
          [
            { header: 'Selection', render: (s) => s.selectionName },
            {
              header: 'Decimal odds',
              render: (s) => num(s.decimalOdds),
              className: 'text-right',
            },
            {
              header: 'Implied',
              render: (s) => pct(s.impliedProbability),
              className: 'text-right',
            },
            {
              header: 'Normalized',
              render: (s) => pct(s.normalizedProbability),
              className: 'text-right',
            },
          ]
        return (
          <div key={market.marketLineId}>
            <div className="mb-2 flex flex-wrap items-center gap-4 text-sm">
              <span className="font-medium text-slate-200">
                {market.marketLineId}
              </span>
              <span className="text-slate-400">
                Overround {num(market.overround)}
              </span>
              <span className="text-slate-400">
                Margin {pct(market.marginPercentage / 100)}
              </span>
            </div>
            <DataTable
              columns={columns}
              rows={market.selections}
              rowKey={(s) => s.selectionId}
              emptyMessage="No selections."
            />
          </div>
        )
      })}
    </div>
  )
}

function MovementPanel({ movements }: { movements: OddsMovement[] }) {
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
      render: (m) => String(m.movement.numberOfChanges),
      className: 'text-right',
    },
    {
      header: 'Δ abs',
      render: (m) => num(m.movement.movementAbsolute),
      className: 'text-right',
    },
    {
      header: 'Δ %',
      render: (m) => num(m.movement.movementPercentage),
      className: 'text-right',
    },
    {
      header: 'Volatility',
      render: (m) => num(m.movement.volatility, 4),
      className: 'text-right',
    },
  ]
  return (
    <DataTable
      columns={columns}
      rows={movements}
      rowKey={(m, i) => `${m.bookmakerId}-${m.marketLineId}-${m.selectionId}-${i}`}
      emptyMessage={EMPTY_MESSAGE}
    />
  )
}

function BookmakersPanel({
  dispersions,
}: {
  dispersions: BookmakerDispersion[]
}) {
  const columns: ReadonlyArray<Column<BookmakerDispersion>> = [
    { header: 'Market', render: (d) => d.marketLineId },
    { header: 'Selection', render: (d) => d.selectionId },
    {
      header: 'Bookmakers',
      render: (d) => String(d.dispersion.bookmakerCount),
      className: 'text-right',
    },
    {
      header: 'Best',
      render: (d) => num(d.dispersion.bestOdds),
      className: 'text-right',
    },
    {
      header: 'Worst',
      render: (d) => num(d.dispersion.worstOdds),
      className: 'text-right',
    },
    {
      header: 'Average',
      render: (d) => num(d.dispersion.averageOdds),
      className: 'text-right',
    },
    {
      header: 'Dispersion',
      render: (d) => num(d.dispersion.dispersion, 4),
      className: 'text-right',
    },
  ]
  return (
    <DataTable
      columns={columns}
      rows={dispersions}
      rowKey={(d, i) => `${d.marketLineId}-${d.selectionId}-${i}`}
      emptyMessage={EMPTY_MESSAGE}
    />
  )
}

function StatisticsPanel({
  statistics,
}: {
  statistics: MatchAnalysisReport['statistics']
}) {
  const statColumns: ReadonlyArray<
    Column<MatchAnalysisReport['statistics']['statistics'][number]>
  > = [
    { header: 'Statistic', render: (s) => s.name },
    {
      header: 'Total',
      render: (s) => num(s.totalValue),
      className: 'text-right',
    },
  ]
  const eventColumns: ReadonlyArray<
    Column<MatchAnalysisReport['statistics']['eventCounts'][number]>
  > = [
    { header: 'Event type', render: (e) => e.type },
    {
      header: 'Count',
      render: (e) => String(e.count),
      className: 'text-right',
    },
  ]
  return (
    <div className="space-y-8">
      <div>
        <h3 className="mb-2 text-sm font-medium text-slate-300">
          Aggregated statistics
        </h3>
        <DataTable
          columns={statColumns}
          rows={statistics.statistics}
          rowKey={(s, i) => `${s.name}-${i}`}
          emptyMessage={EMPTY_MESSAGE}
        />
      </div>
      <div>
        <h3 className="mb-2 text-sm font-medium text-slate-300">
          Event counts (total {statistics.totalEvents})
        </h3>
        <DataTable
          columns={eventColumns}
          rows={statistics.eventCounts}
          rowKey={(e, i) => `${e.type}-${i}`}
          emptyMessage={EMPTY_MESSAGE}
        />
      </div>
    </div>
  )
}

function OddsTab({ state, onRetry }: { state: OddsState; onRetry: () => void }) {
  if (state.kind === 'idle' || state.kind === 'loading')
    return <Spinner label="Loading odds…" />
  if (state.kind === 'empty')
    return <p className="text-sm text-slate-400">{EMPTY_MESSAGE}</p>
  if (state.kind === 'error') {
    return (
      <div className="flex items-center justify-between">
        <p className="text-sm text-red-400">
          Could not load odds: {state.message}
        </p>
        <button
          type="button"
          onClick={onRetry}
          className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
        >
          Retry
        </button>
      </div>
    )
  }

  const columns: ReadonlyArray<Column<OddsSnapshot>> = [
    { header: 'Bookmaker', render: (o) => o.bookmakerId },
    { header: 'Market', render: (o) => o.marketLineId },
    { header: 'Selection', render: (o) => o.selectionId },
    {
      header: 'Decimal odds',
      render: (o) => num(o.decimalOdds),
      className: 'text-right',
    },
    {
      header: 'Implied',
      render: (o) => pct(o.impliedProbability),
      className: 'text-right',
    },
    { header: 'Kind', render: (o) => o.kind },
    {
      header: 'Live',
      render: (o) => (o.isLive ? 'Yes' : 'No'),
      className: 'text-center',
    },
    {
      header: 'Bookmaker time',
      render: (o) => formatTimestamp(o.bookmakerTimestampUtc),
    },
  ]
  return (
    <DataTable
      columns={columns}
      rows={state.odds}
      rowKey={(o) => o.id}
      emptyMessage={EMPTY_MESSAGE}
    />
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

export default MatchDetail
