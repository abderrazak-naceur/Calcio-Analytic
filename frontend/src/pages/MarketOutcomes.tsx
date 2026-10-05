import { useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Spinner } from '../components/Spinner'
import { getMarketOutcomeAnalytics } from '../lib/apiClient'
import type { MarketOutcomeAnalytics } from '../lib/types'

const MARKET_OPTIONS = [
  { value: '1X2', label: '1X2' },
  { value: 'OU', label: 'Over / Under' },
  { value: 'BTTS', label: 'BTTS' },
]

export default function MarketOutcomes() {
  const [marketCode, setMarketCode] = useState('1X2')
  const [minFavoriteOdds, setMinFavoriteOdds] = useState('')
  const [upsetThreshold, setUpsetThreshold] = useState('5')
  const [classification, setClassification] = useState('')
  const [data, setData] = useState<MarketOutcomeAnalytics | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    void getMarketOutcomeAnalytics({
      marketCode,
      minFavoriteOdds: minFavoriteOdds ? Number(minFavoriteOdds) : undefined,
      upsetThreshold: Number(upsetThreshold) || 5,
      classification: classification || undefined,
      pageSize: 100,
    })
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : 'Unable to load market intelligence.'))
      .finally(() => setLoading(false))
  }, [marketCode, minFavoriteOdds, upsetThreshold, classification])

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-green-400">Market Intelligence</p>
        <h1 className="mt-1 text-3xl font-bold text-white">Market vs Reality</h1>
        <p className="mt-2 max-w-3xl text-sm text-slate-400">
          Misuriamo cosa si aspettava il mercato prima del calcio d'inizio e cosa è realmente successo.
        </p>
      </header>

      <Card>
        <div className="grid gap-4 md:grid-cols-4">
          <label className="text-sm text-slate-400">
            Market
            <select value={marketCode} onChange={(e) => setMarketCode(e.target.value)} className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100">
              {MARKET_OPTIONS.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
            </select>
          </label>
          <label className="text-sm text-slate-400">
            Min favorite odds
            <input value={minFavoriteOdds} onChange={(e) => setMinFavoriteOdds(e.target.value)} type="number" min="1.01" step="0.01" placeholder="es. 1.50" className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100" />
          </label>
          <label className="text-sm text-slate-400">
            Upset threshold
            <input value={upsetThreshold} onChange={(e) => setUpsetThreshold(e.target.value)} type="number" min="1.01" step="0.1" className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100" />
          </label>
          <label className="text-sm text-slate-400">
            Result
            <select value={classification} onChange={(e) => setClassification(e.target.value)} className="mt-1 w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100">
              <option value="">All</option>
              <option value="HIT">HIT</option>
              <option value="MISS">MISS</option>
              <option value="UPSET">UPSET</option>
              <option value="UNKNOWN">UNKNOWN</option>
            </select>
          </label>
        </div>
      </Card>

      {loading && <Card><Spinner label="Loading market outcomes…" /></Card>}
      {error && <Card><p className="text-sm text-red-400">{error}</p></Card>}

      {data && (
        <>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-5">
            <Kpi label="Markets" value={data.summary.totalMarkets} />
            <Kpi label="HIT" value={data.summary.hitCount} />
            <Kpi label="MISS" value={data.summary.missCount} />
            <Kpi label="UPSETS" value={data.summary.upsetCount} />
            <Kpi label="Failure rate" value={data.summary.favoriteFailureRatePercentage ?? 0} suffix="%" />
          </div>

          <Card title="Historical outcomes">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[900px] text-left text-sm">
                <thead className="text-xs uppercase tracking-wide text-slate-500">
                  <tr>
                    <th className="pb-3">Match</th>
                    <th className="pb-3">Bookmaker</th>
                    <th className="pb-3">Favorite</th>
                    <th className="pb-3">Winner</th>
                    <th className="pb-3">Result</th>
                  </tr>
                </thead>
                <tbody>
                  {data.results.map((row) => (
                    <tr key={`${row.matchId}-${row.bookmakerId}-${row.marketLineId}`} className="border-t border-slate-800">
                      <td className="py-3 text-slate-200">{row.homeTeamName} – {row.awayTeamName}</td>
                      <td className="py-3 text-slate-400">{row.bookmakerName}</td>
                      <td className="py-3">{row.favoriteSelection} @ {row.favoriteOdds.toFixed(2)}</td>
                      <td className="py-3">{row.winnerSelection ?? '—'} {row.winnerOdds ? `@${row.winnerOdds.toFixed(2)}` : ''}</td>
                      <td className="py-3">
                        <span className={row.isUpset ? 'font-semibold text-amber-400' : row.classification === 'HIT' ? 'text-green-400' : row.classification === 'MISS' ? 'text-red-400' : 'text-slate-400'}>
                          {row.isUpset ? 'UPSET' : row.classification}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {data.results.length === 0 && <p className="py-8 text-center text-sm text-slate-500">No market outcomes for these filters.</p>}
            </div>
          </Card>
        </>
      )}
    </div>
  )
}

function Kpi({ label, value, suffix = '' }: { label: string; value: number; suffix?: string }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-4">
      <p className="text-xs uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-1 text-2xl font-bold text-white">{value}{suffix}</p>
    </div>
  )
}
