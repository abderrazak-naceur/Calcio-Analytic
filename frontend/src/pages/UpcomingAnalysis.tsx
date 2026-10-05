import { useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Spinner } from '../components/Spinner'
import { getUpcomingAnalysis } from '../lib/apiClient'
import type { UpcomingAnalysis as UpcomingAnalysisData } from '../lib/types'

const WINDOWS = [
  ['today', 'Today'],
  ['tomorrow', 'Tomorrow'],
  ['2d', 'Next 2 days'],
  ['7d', 'Next 7 days'],
  ['14d', 'Next 14 days'],
  ['30d', 'Next 30 days'],
] as const

export default function UpcomingAnalysis() {
  const [window, setWindow] = useState('today')
  const [data, setData] = useState<UpcomingAnalysisData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    void getUpcomingAnalysis({ window, minimumComparableSamples: 5 })
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : 'Unable to load upcoming analysis.'))
      .finally(() => setLoading(false))
  }, [window])

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-cyan-400">Market Intelligence</p>
        <h1 className="mt-1 text-3xl font-bold text-white">Upcoming Analysis</h1>
        <p className="mt-2 max-w-3xl text-sm text-slate-400">
          Confrontiamo le quote delle prossime partite con situazioni storiche comparabili. Le statistiche descrivono lo storico e non garantiscono il risultato futuro.
        </p>
      </header>

      <Card>
        <div className="flex flex-wrap gap-2">
          {WINDOWS.map(([value, label]) => (
            <button key={value} type="button" onClick={() => setWindow(value)} className={window === value ? 'rounded-lg bg-cyan-400 px-4 py-2 text-sm font-semibold text-slate-950' : 'rounded-lg border border-slate-700 px-4 py-2 text-sm text-slate-300 hover:border-slate-500'}>
              {label}
            </button>
          ))}
        </div>
      </Card>

      {loading && <Card><Spinner label="Loading upcoming analysis…" /></Card>}
      {error && <Card><p className="text-sm text-red-400">{error}</p></Card>}

      {data && !loading && (
        <Card title={`${data.results.length} comparable upcoming matches`}>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1100px] text-left text-sm">
              <thead className="text-xs uppercase tracking-wide text-slate-500">
                <tr>
                  <th className="pb-3">Match</th>
                  <th className="pb-3">Kickoff</th>
                  <th className="pb-3">Favorite</th>
                  <th className="pb-3">Comparable</th>
                  <th className="pb-3">Historical HIT</th>
                  <th className="pb-3">Failure</th>
                  <th className="pb-3">Upsets</th>
                  <th className="pb-3">ROI</th>
                </tr>
              </thead>
              <tbody>
                {data.results.map((row) => (
                  <tr key={`${row.matchId}-${row.bookmakerId}`} className="border-t border-slate-800">
                    <td className="py-3 text-slate-200">{row.homeTeamName} – {row.awayTeamName}</td>
                    <td className="py-3 text-slate-400">{new Date(row.kickoffUtc).toLocaleString()}</td>
                    <td className="py-3">{row.favoriteSelection} @ <strong>{row.favoriteOdds.toFixed(2)}</strong></td>
                    <td className="py-3">{row.comparableSampleSize}</td>
                    <td className="py-3 text-green-400">{row.comparableHistory.hitRatePercentage ?? 0}%</td>
                    <td className="py-3 text-red-400">{row.comparableHistory.failureRatePercentage ?? 0}%</td>
                    <td className="py-3 text-amber-400">{row.comparableHistory.upsetCount}</td>
                    <td className={row.comparableHistory.roiPercentage !== null && row.comparableHistory.roiPercentage >= 0 ? 'py-3 text-green-400' : 'py-3 text-red-400'}>{row.comparableHistory.roiPercentage ?? 0}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {data.results.length === 0 && <p className="py-10 text-center text-sm text-slate-500">No upcoming matches with enough historical comparable situations.</p>}
          </div>
        </Card>
      )}
    </div>
  )
}
