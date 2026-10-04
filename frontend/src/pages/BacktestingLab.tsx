import { useCallback, useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Spinner } from '../components/Spinner'
import { getHighOddsAnalytics } from '../lib/apiClient'
import type { HighOddsSelection } from '../lib/types'

type State = 'loading' | 'ready' | 'error'

function BacktestingLab() {
  const [minOdds, setMinOdds] = useState('6')
  const [period, setPeriod] = useState('365')
  const [selection, setSelection] = useState('')
  const [stake, setStake] = useState('1')
  const [rows, setRows] = useState<HighOddsSelection[]>([])
  const [state, setState] = useState<State>('loading')
  const [error, setError] = useState('')

  const run = useCallback(async () => {
    setState('loading')
    setError('')
    const to = new Date()
    const from = new Date(to)
    from.setUTCDate(from.getUTCDate() - Number(period))
    try {
      const result = await getHighOddsAnalytics({
        fromUtc: from.toISOString(),
        toUtc: to.toISOString(),
        minOdds: Number(minOdds) || 6,
        result: selection || undefined,
        page: 1,
        pageSize: 5000,
      })
      setRows(result.results)
      setState('ready')
    } catch (err) {
      setState('error')
      setError(err instanceof Error ? err.message : 'Backtest failed')
    }
  }, [minOdds, period, selection])

  useEffect(() => {
    void run()
  }, [run])

  const unit = Math.max(0.01, Number(stake) || 1)
  const profit = rows.reduce((sum, row) => sum + row.profitUnits * unit, 0)
  const wins = rows.filter((row) => row.won).length
  const roi = rows.length ? (profit / (rows.length * unit)) * 100 : 0

  let equity = 0
  let peak = 0
  let maxDrawdown = 0
  const curve: number[] = []
  for (const row of [...rows].reverse()) {
    equity += row.profitUnits * unit
    peak = Math.max(peak, equity)
    maxDrawdown = Math.max(maxDrawdown, peak - equity)
    curve.push(equity)
  }

  const maxCurve = Math.max(1, ...curve.map((x) => Math.abs(x)))
  const sampleWarning = rows.length < 500

  return (
    <div className="space-y-6">
      <header>
        <p className="mb-2 text-xs font-semibold uppercase tracking-[0.18em] text-green-400">
          Research Lab
        </p>
        <h1 className="text-3xl font-bold text-white">Backtesting Lab</h1>
        <p className="mt-1 max-w-3xl text-sm text-slate-400">
          Replay the High Odds strategy on stored historical 1X2 selections using
          the same pre-kickoff evidence as the explorer.
        </p>
      </header>

      <Card title="Strategy builder">
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <label className="text-xs font-medium text-slate-400">
            Minimum odds
            <select
              value={minOdds}
              onChange={(e) => setMinOdds(e.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-slate-200"
            >
              {[6, 7, 8, 10, 15, 20].map((value) => (
                <option key={value} value={value}>≥ {value.toFixed(2)}</option>
              ))}
            </select>
          </label>
          <label className="text-xs font-medium text-slate-400">
            Historical window
            <select
              value={period}
              onChange={(e) => setPeriod(e.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-slate-200"
            >
              <option value="30">30 days</option>
              <option value="90">90 days</option>
              <option value="365">1 year</option>
            </select>
          </label>
          <label className="text-xs font-medium text-slate-400">
            Selection
            <select
              value={selection}
              onChange={(e) => setSelection(e.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-slate-200"
            >
              <option value="">All 1X2</option>
              <option value="Home">Home</option>
              <option value="Draw">Draw</option>
              <option value="Away">Away</option>
            </select>
          </label>
          <label className="text-xs font-medium text-slate-400">
            Flat stake
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={stake}
              onChange={(e) => setStake(e.target.value)}
              className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-slate-200"
            />
          </label>
        </div>
        <button
          type="button"
          onClick={() => void run()}
          className="mt-4 rounded-lg bg-green-500 px-4 py-2 text-sm font-semibold text-slate-950 hover:bg-green-400"
        >
          Run backtest
        </button>
      </Card>

      {state === 'loading' && <Card><Spinner label="Running historical replay…" /></Card>}

      {state === 'error' && (
        <Card>
          <p className="text-red-400">{error}</p>
        </Card>
      )}

      {state === 'ready' && (
        <>
          <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
            <Metric label="Bets" value={rows.length.toLocaleString()} />
            <Metric label="Win rate" value={(rows.length ? wins / rows.length * 100 : 0).toFixed(1) + '%'} />
            <Metric label="Profit" value={(profit >= 0 ? '+' : '') + profit.toFixed(2) + 'u'} />
            <Metric label="ROI" value={(roi >= 0 ? '+' : '') + roi.toFixed(1) + '%'} />
            <Metric label="Max drawdown" value={'-' + maxDrawdown.toFixed(2) + 'u'} />
          </div>

          {sampleWarning && (
            <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-300">
              Small sample: {rows.length} bets. Treat the result as exploratory,
              not as evidence of a stable strategy.
            </div>
          )}

          <Card title="Equity curve">
            <div className="flex h-56 items-end gap-1 overflow-hidden rounded-lg bg-slate-950 p-4">
              {curve.slice(-120).map((value, index) => (
                <div
                  key={index}
                  title={value.toFixed(2) + 'u'}
                  className={value >= 0 ? 'flex-1 rounded-t bg-green-500/80' : 'flex-1 rounded-t bg-red-500/70'}
                  style={{ height: Math.max(3, Math.abs(value) / maxCurve * 100) + '%' }}
                />
              ))}
            </div>
          </Card>
        </>
      )}
    </div>
  )
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/70 p-4">
      <p className="text-xs text-slate-500">{label}</p>
      <p className="mt-1 text-xl font-bold text-white">{value}</p>
    </div>
  )
}

export default BacktestingLab
