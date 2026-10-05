import { useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Spinner } from '../components/Spinner'
import { getDailyMarketReport } from '../lib/apiClient'
import type { DailyMarketReport as DailyMarketReportData } from '../lib/types'

export default function DailyReport() {
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10))
  const [data, setData] = useState<DailyMarketReportData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setLoading(true)
    setError(null)
    void getDailyMarketReport(date)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : 'Unable to load daily report.'))
      .finally(() => setLoading(false))
  }, [date])

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-violet-400">Market Intelligence</p>
          <h1 className="mt-1 text-3xl font-bold text-white">Daily Market Report</h1>
          <p className="mt-2 text-sm text-slate-400">Report giornaliero delle aspettative del mercato e degli upset osservati.</p>
        </div>
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100" />
      </header>

      {loading && <Card><Spinner label="Generating daily report…" /></Card>}
      {error && <Card><p className="text-sm text-red-400">{error}</p></Card>}

      {data && !loading && (
        <>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-5">
            <Kpi label="Settled markets" value={data.settledMarkets} />
            <Kpi label="Favorite HIT" value={data.favoriteHits} />
            <Kpi label="Favorite MISS" value={data.favoriteFailures} />
            <Kpi label="Upsets" value={data.upsets} />
            <Kpi label="Flat ROI" value={data.roiPercentage ?? 0} suffix="%" />
          </div>

          <Card title="Next day">
            <p className="text-3xl font-bold text-white">{data.upcomingMatches}</p>
            <p className="mt-1 text-sm text-slate-500">scheduled matches after the report date</p>
          </Card>

          <Card title="Biggest upsets">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[900px] text-left text-sm">
                <thead className="text-xs uppercase tracking-wide text-slate-500">
                  <tr><th className="pb-3">Match</th><th className="pb-3">Competition</th><th className="pb-3">Favorite</th><th className="pb-3">Winner</th><th className="pb-3">Winner odds</th></tr>
                </thead>
                <tbody>
                  {data.biggestUpsets.map((row) => (
                    <tr key={`${row.matchId}-${row.favoriteSelection}`} className="border-t border-slate-800">
                      <td className="py-3 text-slate-200">{row.homeTeamName} – {row.awayTeamName}</td>
                      <td className="py-3 text-slate-400">{row.competitionName}</td>
                      <td className="py-3 text-red-300">{row.favoriteSelection} @ {row.favoriteOdds.toFixed(2)}</td>
                      <td className="py-3 text-green-300">{row.winnerSelection ?? '—'}</td>
                      <td className="py-3 font-semibold text-amber-400">{row.winnerOdds?.toFixed(2) ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {data.biggestUpsets.length === 0 && <p className="py-8 text-center text-sm text-slate-500">No qualifying upsets for this date.</p>}
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
