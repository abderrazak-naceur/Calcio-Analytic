import { useEffect, useState } from 'react'
import { Card } from '../components/Card'
import { Spinner } from '../components/Spinner'
import { getDataQualitySummary } from '../lib/apiClient'
import type { DataQualitySummary as DataQualitySummaryData } from '../lib/types'

export default function DataQuality() {
  const [data, setData] = useState<DataQualitySummaryData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void getDataQualitySummary()
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : 'Unable to load data quality.'))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-rose-400">Operations</p>
        <h1 className="mt-1 text-3xl font-bold text-white">Data Quality</h1>
        <p className="mt-2 text-sm text-slate-400">Coverage and integrity checks for matches, odds, results and settlements.</p>
      </header>

      {loading && <Card><Spinner label="Checking data quality…" /></Card>}
      {error && <Card><p className="text-sm text-red-400">{error}</p></Card>}
      {data && !loading && (
        <>
          <Card title="Overall quality">
            <div className="flex items-end justify-between gap-4">
              <div><p className="text-4xl font-bold text-white">{data.qualityScorePercentage.toFixed(2)}%</p><p className="mt-1 text-sm text-slate-500">visibility score based on current integrity checks</p></div>
              <p className="text-xs text-slate-500">Generated {new Date(data.generatedAtUtc).toLocaleString()}</p>
            </div>
          </Card>

          <div className="grid grid-cols-2 gap-4 md:grid-cols-3">
            <Issue label="Missing odds" value={data.missingOddsMatches} />
            <Issue label="Missing results" value={data.missingResults} />
            <Issue label="Missing settlements" value={data.missingSettlements} />
            <Issue label="Duplicate odds" value={data.duplicateOddsMatches} />
            <Issue label="Stale scheduled" value={data.staleScheduledMatches} />
            <Issue label="Finished matches" value={data.finishedMatches} neutral />
          </div>

          <Card title="Odds provenance coverage">
            <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
              <Issue label="Snapshots" value={data.oddsSnapshotCount} neutral />
              <Issue label="Providers" value={data.oddsProviderCount} neutral />
              <Issue label="Bookmakers" value={data.oddsBookmakerCount} neutral />
              <Issue label="Markets" value={data.oddsMarketCount} neutral />
            </div>
          </Card>
        </>
      )}
    </div>
  )
}

function Issue({ label, value, neutral = false }: { label: string; value: number; neutral?: boolean }) {
  return <div className="rounded-xl border border-slate-800 bg-slate-900/60 p-4"><p className="text-xs uppercase tracking-wide text-slate-500">{label}</p><p className={neutral ? 'mt-1 text-2xl font-bold text-white' : value > 0 ? 'mt-1 text-2xl font-bold text-red-400' : 'mt-1 text-2xl font-bold text-emerald-400'}>{value.toLocaleString()}</p></div>
}
