import { useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Spinner } from '../components/Spinner'
import { getMarketOutcomeAnalytics } from '../lib/apiClient'
import type { MarketOutcomeAnalytics } from '../lib/types'

export default function Upsets() {
  const [marketCode, setMarketCode] = useState('1X2')
  const [threshold, setThreshold] = useState('5')
  const [data, setData] = useState<MarketOutcomeAnalytics | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    void getMarketOutcomeAnalytics({
      marketCode,
      upsetThreshold: Number(threshold) || 5,
      classification: 'UPSET',
      pageSize: 200,
    })
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : 'Unable to load upsets.'))
      .finally(() => setLoading(false))
  }, [marketCode, threshold])

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-amber-400">Market Intelligence</p>
        <h1 className="mt-1 text-3xl font-bold text-white">Upsets</h1>
        <p className="mt-2 max-w-3xl text-sm text-slate-400">
          Partite in cui il favorito è fallito e il vincitore aveva una quota almeno pari alla soglia selezionata.
        </p>
      </header>

      <Card>
        <div className="grid gap-4 md:grid-cols-2">
          <label className="text-sm text-slate-400">
            Market
            <select value={marketCode} onChange={(e) => setMarketCode(e.target.value)} className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100">
              <option value="1X2">1X2</option>
              <option value="OU">Over / Under</option>
              <option value="BTTS">BTTS</option>
            </select>
          </label>
          <label className="text-sm text-slate-400">
            Winner odds threshold
            <select value={threshold} onChange={(e) => setThreshold(e.target.value)} className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100">
              {[5, 6, 7, 8, 10].map((value) => <option key={value} value={value}>{value}.00+</option>)}
            </select>
          </label>
        </div>
      </Card>

      {loading && <Card><Spinner label="Loading upset history…" /></Card>}
      {error && <Card><p className="text-sm text-red-400">{error}</p></Card>}

      {data && !loading && (
        <>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-5">
            <Kpi label="Upsets" value={data.summary.upsetCount} />
            <Kpi label="Market misses" value={data.summary.missCount} />
            <Kpi label="Failure rate" value={data.summary.favoriteFailureRatePercentage ?? 0} suffix="%" />
            <Kpi label="Implied probability" value={data.summary.impliedProbabilityPercentage ?? 0} suffix="%" />
            <Kpi label="ROI" value={data.summary.roiPercentage} suffix="%" />
          </div>

          <Card title={`Winner odds ≥ ${threshold}.00`}>
            <div className="grid gap-3 sm:grid-cols-5">
              {data.summary.upsetThresholds.map((item) => (
                <div key={item.threshold} className={`rounded-lg border p-4 ${String(item.threshold) === threshold ? 'border-amber-400/60 bg-amber-400/10' : 'border-slate-800 bg-slate-950/60'}`}>
                  <p className="text-xs text-slate-500">{item.threshold.toFixed(0)}+</p>
                  <p className="mt-1 text-2xl font-bold text-white">{item.winnerCount}</p>
                  <p className="text-xs text-amber-400">of favorite failures</p>
                </div>
              ))}
            </div>
          </Card>

          <Card title="Upset matches">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[1000px] text-left text-sm">
                <thead className="text-xs uppercase tracking-wide text-slate-500">
                  <tr>
                    <th className="pb-3">Date</th>
                    <th className="pb-3">Match</th>
                    <th className="pb-3">Bookmaker</th>
                    <th className="pb-3">Favorite</th>
                    <th className="pb-3">Winner</th>
                    <th className="pb-3">Winner odds</th>
                  </tr>
                </thead>
                <tbody>
                  {data.results.map((row) => (
                    <tr key={`${row.matchId}-${row.bookmakerId}-${row.marketLineId}`} className="border-t border-slate-800">
                      <td className="py-3 text-slate-400">{new Date(row.kickoffUtc).toLocaleDateString()}</td>
                      <td className="py-3 text-slate-200">{row.homeTeamName} – {row.awayTeamName}</td>
                      <td className="py-3 text-slate-400">{row.bookmakerName}</td>
                      <td className="py-3 text-red-300">{row.favoriteSelection} @ {row.favoriteOdds.toFixed(2)}</td>
                      <td className="py-3 text-green-300">{row.winnerSelection ?? '—'}</td>
                      <td className="py-3 font-semibold text-amber-400">{row.winnerOdds?.toFixed(2) ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {data.results.length === 0 && <p className="py-10 text-center text-sm text-slate-500">No upsets found for this threshold.</p>}
            </div>
          </Card>
        </>
      )}
    </div>
  )
}

function Kpi({ label, value, suffix = '' }: { label: string; value: number; suffix?: string }) {
  return <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-4"><p className="text-xs uppercase tracking-wide text-slate-500">{label}</p><p className="mt-1 text-2xl font-bold text-white">{value}{suffix}</p></div>
}
