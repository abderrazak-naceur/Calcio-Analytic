import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Card } from '../components/Card'
import { DataTable, type Column } from '../components/DataTable'
import { Spinner } from '../components/Spinner'
import { StatusBadge } from '../components/StatusBadge'
import {
  ApiError,
  getDashboardSummary,
  getHealth,
  getHighOddsAnalytics,
  getHighOddsCatalog,
  getMarketOutcomeAnalytics,
  getRecentMatches,
  getSportmonksLeagues,
  getSportmonksSeasons,
  postSeedDemoOdds,
  postSportmonksBulkSeasonImport,
  postSportmonksSeasonImport,
} from '../lib/apiClient'
import type {
  DashboardSummary,
  DemoOddsSeedResult,
  HighOddsAnalytics,
  HighOddsCatalog,
  MarketOutcomeAnalytics,
  ProviderCompetition,
  ProviderSeason,
  RecentMatch,
  SportmonksBulkSeasonImportResult,
  SportmonksSeasonImportResult,
} from '../lib/types'

type SummaryState =
  | { kind: 'loading' }
  | { kind: 'ready'; summary: DashboardSummary }
  | { kind: 'error'; message: string }

type RecentState =
  | { kind: 'loading' }
  | { kind: 'ready'; matches: RecentMatch[] }
  | { kind: 'error'; message: string }

type HealthState =
  | { kind: 'loading' }
  | { kind: 'healthy'; status: string }
  | { kind: 'error'; message: string }

type HighOddsState =
  | { kind: 'loading' }
  | { kind: 'ready'; data: HighOddsAnalytics }
  | { kind: 'error'; message: string }

type MarketOutcomeState =
  | { kind: 'loading' }
  | { kind: 'ready'; data: MarketOutcomeAnalytics }
  | { kind: 'error'; message: string }

const UPSET_THRESHOLDS = [3, 5, 6, 7, 8, 10] as const

function toIsoRange(days: number): { fromUtc: string; toUtc: string } {
  const to = new Date()
  const from = new Date(to)
  from.setUTCDate(from.getUTCDate() - days)
  return { fromUtc: from.toISOString(), toUtc: to.toISOString() }
}

function formatNum(value: number): string {
  return value.toLocaleString('en-US')
}

function formatPct(value: number, digits = 1): string {
  return `${value.toFixed(digits)}%`
}

function formatOdds(value: number): string {
  return value.toFixed(2)
}

function formatMoney(value: number): string {
  return `${value >= 0 ? '+' : ''}${value.toFixed(2)}u`
}

function formatTimestamp(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString()
}

function formatKickoff(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString(undefined, {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatScore(match: RecentMatch): string {
  if (match.homeScore === null || match.awayScore === null) return '—'
  return `${match.homeScore} – ${match.awayScore}`
}

function resultLabel(match: RecentMatch): string {
  if (match.homeScore === null || match.awayScore === null) return match.status
  if (match.homeScore > match.awayScore) return 'HOME'
  if (match.homeScore < match.awayScore) return 'AWAY'
  return 'DRAW'
}

function Dashboard() {
  const [summary, setSummary] = useState<SummaryState>({ kind: 'loading' })
  const [recent, setRecent] = useState<RecentState>({ kind: 'loading' })
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })
  const [highOdds, setHighOdds] = useState<HighOddsState>({ kind: 'loading' })
  const [marketOutcome, setMarketOutcome] = useState<MarketOutcomeState>({ kind: 'loading' })
  const [catalog, setCatalog] = useState<HighOddsCatalog | null>(null)
  const [minOdds, setMinOdds] = useState<number>(6)
  const [periodDays, setPeriodDays] = useState<number>(30)
  const [leagues, setLeagues] = useState<ProviderCompetition[]>([])
  const [seasons, setSeasons] = useState<ProviderSeason[]>([])
  const [selectedLeagueId, setSelectedLeagueId] = useState('')
  const [selectedSeasonId, setSelectedSeasonId] = useState('')
  const [leagueError, setLeagueError] = useState<string | null>(null)
  const [seasonError, setSeasonError] = useState<string | null>(null)
  const [loadingLeagues, setLoadingLeagues] = useState(true)
  const [loadingSeasons, setLoadingSeasons] = useState(false)
  const [importingSeason, setImportingSeason] = useState(false)
  const [bulkSeasonLabel, setBulkSeasonLabel] = useState('')
  const [bulkMaxLeagues, setBulkMaxLeagues] = useState('10')
  const [bulkSkipLeagues, setBulkSkipLeagues] = useState('0')
  const [importingBulk, setImportingBulk] = useState(false)
  const [seedingDemo, setSeedingDemo] = useState(false)
  const [importError, setImportError] = useState<string | null>(null)
  const [importResult, setImportResult] =
    useState<SportmonksSeasonImportResult | null>(null)
  const [bulkResult, setBulkResult] =
    useState<SportmonksBulkSeasonImportResult | null>(null)
  const [demoResult, setDemoResult] =
    useState<DemoOddsSeedResult | null>(null)
  const [showIngestion, setShowIngestion] = useState(false)

  const loadSummary = useCallback(async () => {
    setSummary({ kind: 'loading' })
    try {
      const data = await getDashboardSummary()
      setSummary({ kind: 'ready', summary: data })
    } catch (error) {
      setSummary({ kind: 'error', message: describeError(error) })
    }
  }, [])

  const loadRecent = useCallback(async () => {
    setRecent({ kind: 'loading' })
    try {
      const data = await getRecentMatches(12)
      setRecent({ kind: 'ready', matches: data })
    } catch (error) {
      setRecent({ kind: 'error', message: describeError(error) })
    }
  }, [])

  const loadHealth = useCallback(async () => {
    setHealth({ kind: 'loading' })
    try {
      const result = await getHealth()
      setHealth({ kind: 'healthy', status: result.status })
    } catch (error) {
      setHealth({ kind: 'error', message: describeError(error) })
    }
  }, [])

  const loadMarketOutcome = useCallback(async () => {
    setMarketOutcome({ kind: 'loading' })
    try {
      const data = await getMarketOutcomeAnalytics({
        fromUtc: toIsoRange(periodDays).fromUtc,
        toUtc: toIsoRange(periodDays).toUtc,
        marketCode: '1X2',
        upsetThreshold: minOdds,
        page: 1,
        pageSize: 1,
      })
      setMarketOutcome({ kind: 'ready', data })
    } catch (error) {
      setMarketOutcome({ kind: 'error', message: describeError(error) })
    }
  }, [minOdds, periodDays])

  useEffect(() => {
    void loadSummary()
    void loadRecent()
    void loadHealth()
    void getHighOddsCatalog().then(setCatalog).catch(() => setCatalog(null))
  }, [loadSummary, loadRecent, loadHealth])

  useEffect(() => {
    void loadMarketOutcome()
  }, [loadMarketOutcome])

  useEffect(() => {
    let cancelled = false
    async function run() {
      setHighOdds({ kind: 'loading' })
      try {
        const range = toIsoRange(periodDays)
        const data = await getHighOddsAnalytics({
          fromUtc: range.fromUtc,
          toUtc: range.toUtc,
          minOdds,
          page: 1,
          pageSize: 8,
        })
        if (!cancelled) setHighOdds({ kind: 'ready', data })
      } catch (error) {
        if (!cancelled) setHighOdds({ kind: 'error', message: describeError(error) })
      }
    }
    void run()
    return () => {
      cancelled = true
    }
  }, [minOdds, periodDays])

  useEffect(() => {
    let active = true
    setLoadingLeagues(true)
    void getSportmonksLeagues()
      .then((data) => {
        if (!active) return
        setLeagues(data)
        setLeagueError(null)
      })
      .catch((error: unknown) => {
        if (!active) return
        setLeagueError(describeError(error))
      })
      .finally(() => {
        if (active) setLoadingLeagues(false)
      })

    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    if (!selectedLeagueId) {
      setSeasons([])
      setSelectedSeasonId('')
      setSeasonError(null)
      setLoadingSeasons(false)
      return
    }

    let active = true
    setLoadingSeasons(true)
    setSeasons([])
    setSelectedSeasonId('')
    void getSportmonksSeasons(selectedLeagueId)
      .then((data) => {
        if (!active) return
        setSeasons(data)
        setSeasonError(null)
      })
      .catch((error: unknown) => {
        if (!active) return
        setSeasonError(describeError(error))
      })
      .finally(() => {
        if (active) setLoadingSeasons(false)
      })

    return () => {
      active = false
    }
  }, [selectedLeagueId])

  const importSeason = useCallback(async () => {
    if (!selectedLeagueId || !selectedSeasonId) return
    setImportingSeason(true)
    setImportError(null)
    setImportResult(null)
    setBulkResult(null)
    setDemoResult(null)
    try {
      const result = await postSportmonksSeasonImport(
        selectedLeagueId,
        selectedSeasonId,
        true,
      )
      setImportResult(result)
      await loadSummary()
      await loadRecent()
    } catch (error) {
      setImportError(describeError(error))
    } finally {
      setImportingSeason(false)
    }
  }, [loadRecent, loadSummary, selectedLeagueId, selectedSeasonId])

  const importAllLeagues = useCallback(async () => {
    const label = bulkSeasonLabel.trim()
    if (!label) return
    const maxLeagues = Math.min(250, Math.max(1, Number(bulkMaxLeagues) || 10))
    const skipLeagues = Math.max(0, Number(bulkSkipLeagues) || 0)
    setImportingBulk(true)
    setImportError(null)
    setImportResult(null)
    setBulkResult(null)
    setDemoResult(null)
    try {
      const result = await postSportmonksBulkSeasonImport(label, {
        seedDemoOdds: true,
        maxLeagues,
        skipLeagues,
      })
      setBulkResult(result)
      await loadSummary()
      await loadRecent()
    } catch (error) {
      setImportError(describeError(error))
    } finally {
      setImportingBulk(false)
    }
  }, [bulkMaxLeagues, bulkSeasonLabel, bulkSkipLeagues, loadRecent, loadSummary])

  const seedDemoOdds = useCallback(async () => {
    setSeedingDemo(true)
    setImportError(null)
    setImportResult(null)
    setBulkResult(null)
    setDemoResult(null)
    try {
      const result = await postSeedDemoOdds(
        selectedLeagueId || undefined,
        selectedSeasonId || undefined,
      )
      setDemoResult(result)
      await loadSummary()
    } catch (error) {
      setImportError(describeError(error))
    } finally {
      setSeedingDemo(false)
    }
  }, [loadSummary, selectedLeagueId, selectedSeasonId])

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-xl font-bold tracking-tight text-white sm:text-2xl">
              Market Intelligence Terminal
            </h1>
            <HealthChip state={health} />
          </div>
          <p className="mt-1.5 max-w-2xl text-[13px] leading-relaxed text-slate-400">
            What did the market expect — and what actually happened? Every figure
            is computed from stored SportMonks matches and pre-kickoff odds.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <div className="flex rounded-lg border border-white/10 bg-white/[0.03] p-1 text-xs font-semibold">
            {[7, 30, 90, 365].map((d) => (
              <button
                key={d}
                type="button"
                onClick={() => setPeriodDays(d)}
                className={`rounded-md px-2.5 py-1.5 transition ${
                  periodDays === d
                    ? 'bg-emerald-500/20 text-emerald-300'
                    : 'text-slate-400 hover:text-slate-200'
                }`}
              >
                {d}D
              </button>
            ))}
          </div>
          <button
            type="button"
            onClick={() => {
              void loadSummary()
              void loadRecent()
              void loadHealth()
            }}
            className="rounded-lg border border-white/10 bg-white/[0.03] px-3 py-2 text-xs font-semibold text-slate-300 transition hover:bg-white/[0.07] hover:text-white"
          >
            Refresh
          </button>
          <button
            type="button"
            onClick={() => setShowIngestion((v) => !v)}
            className="rounded-lg bg-emerald-500 px-3 py-2 text-xs font-bold text-[#060a13] transition hover:bg-emerald-400"
          >
            {showIngestion ? 'Hide ingestion' : '+ Import data'}
          </button>
        </div>
      </div>

      <KpiStrip
        summary={summary}
        highOdds={highOdds}
        threshold={minOdds}
        onRetry={() => void loadSummary()}
      />

      <MarketRealityKpi state={marketOutcome} />

      <div className="flex flex-wrap items-center gap-2 rounded-xl border border-white/5 bg-[#0a0f1c] p-3">
        <span className="px-1 text-[11px] font-bold uppercase tracking-[0.16em] text-slate-500">
          Upset threshold · winner ≥
        </span>
        {UPSET_THRESHOLDS.map((t) => (
          <button
            key={t}
            type="button"
            onClick={() => setMinOdds(t)}
            className={`rounded-lg px-3 py-1.5 text-xs font-bold transition ${
              minOdds === t
                ? 'bg-amber-400 text-[#060a13]'
                : 'border border-white/10 bg-white/[0.03] text-slate-300 hover:border-amber-400/40 hover:text-amber-300'
            }`}
          >
            {t}.00
          </button>
        ))}
        <span className="ml-auto hidden text-[11px] text-slate-500 sm:block">
          {catalog
            ? `${catalog.competitions.length} competitions · ${catalog.bookmakers.length} bookmakers`
            : 'Loading catalog…'}
        </span>
      </div>

      <div className="grid gap-5 xl:grid-cols-5">
        <div className="xl:col-span-3">
          <Card title="Upset distribution">
            <UpsetDistribution
              state={highOdds}
              threshold={minOdds}
              periodDays={periodDays}
            />
          </Card>
        </div>
        <div className="xl:col-span-2">
          <Card title="Market coverage">
            <MarketCoverage summary={summary} />
          </Card>
        </div>
      </div>

      <div className="grid gap-5 xl:grid-cols-5">
        <div className="xl:col-span-3">
          <Card
            title="Biggest upsets"
            actions={
              <Link
                to="/high-odds"
                className="text-xs font-semibold text-emerald-400 transition hover:text-emerald-300"
              >
                Open High Odds Intelligence →
              </Link>
            }
          >
            <TopUpsets state={highOdds} />
          </Card>
        </div>
        <div className="xl:col-span-2">
          <Card
            title="Latest results"
            actions={
              <Link
                to="/matches"
                className="text-xs font-semibold text-emerald-400 transition hover:text-emerald-300"
              >
                All matches →
              </Link>
            }
          >
            <RecentMatches state={recent} onRetry={() => void loadRecent()} />
          </Card>
        </div>
      </div>

      {showIngestion && (
      <Card title="Import Sportmonks season">
        <p className="mb-4 text-sm text-slate-400">
          Select a league and season to import its fixtures and available
          pre-match odds for finished matches. Sportmonks Odds add-on is
          required. Standard odds provide the available price snapshots, not
          the complete historical movement timeline.
        </p>

        <div className="grid gap-4 md:grid-cols-[1fr_1fr_auto] md:items-end">
          <label className="block text-sm text-slate-300">
            League
            <select
              value={selectedLeagueId}
              onChange={(event) => setSelectedLeagueId(event.target.value)}
              disabled={loadingLeagues || importingSeason}
              className="mt-1 block w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-white disabled:opacity-60"
            >
              <option value="">
                {loadingLeagues ? 'Loading leagues…' : 'Select a league'}
              </option>
              {leagues.map((league) => (
                <option key={league.externalId} value={league.externalId}>
                  {league.name}
                  {league.countryName ? ` — ${league.countryName}` : ''}
                </option>
              ))}
            </select>
          </label>

          <label className="block text-sm text-slate-300">
            Season
            <select
              value={selectedSeasonId}
              onChange={(event) => setSelectedSeasonId(event.target.value)}
              disabled={!selectedLeagueId || loadingSeasons || importingSeason}
              className="mt-1 block w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-white disabled:opacity-60"
            >
              <option value="">
                {loadingSeasons
                  ? 'Loading seasons…'
                  : selectedLeagueId
                    ? 'Select a season'
                    : 'Select a league first'}
              </option>
              {seasons.map((season) => (
                <option key={season.externalId} value={season.externalId}>
                  {season.label}
                </option>
              ))}
            </select>
          </label>

          <button
            type="button"
            onClick={() => void importSeason()}
            disabled={!selectedLeagueId || !selectedSeasonId || importingSeason}
            className="rounded-md bg-green-600 px-3 py-2 text-sm font-medium text-white transition hover:bg-green-500 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {importingSeason ? 'Importing season…' : 'Import season'}
          </button>
        </div>

        {leagueError && (
          <p className="mt-3 text-sm text-red-400">
            Could not load Sportmonks leagues: {leagueError}. Check the local
            Sportmonks API token configuration.
          </p>
        )}
        {seasonError && (
          <p className="mt-3 text-sm text-red-400">
            Could not load seasons: {seasonError}
          </p>
        )}
        {importingSeason && (
          <p className="mt-4 text-sm text-amber-300">
            Importing fixtures and querying odds for completed matches. This
            may take several minutes for a full season.
          </p>
        )}
        {importError && (
          <p className="mt-4 text-sm text-red-400">
            Season import failed: {importError}
          </p>
        )}
        {importResult && (
          <div className="mt-4 space-y-2 text-sm" role="status">
            <p className="text-emerald-300">
              Imported {importResult.fixturesUpserted} fixtures; requested odds
              for {importResult.oddsRequestsAttempted} of{' '}
              {importResult.completedFixturesWithOddsRequested} finished matches;
              saved {importResult.oddsSnapshotsInserted} odds snapshots
              {importResult.duplicateOddsSnapshotsSkipped > 0
                ? ` (${importResult.duplicateOddsSnapshotsSkipped} duplicates skipped)`
                : ''}
              {importResult.demoOddsSnapshotsInserted > 0
                ? ` (+${importResult.demoOddsSnapshotsInserted} synthetic demo snapshots so analysis screens have data)`
                : ''}
              .
            </p>
            {importResult.oddsWarning && (
              <p className="text-amber-300">{importResult.oddsWarning}</p>
            )}
          </div>
        )}
        <div className="mt-6 border-t border-slate-800 pt-6">
          <h3 className="text-base font-semibold text-white">
            Import all leagues for one season
          </h3>
          <p className="mt-1 text-sm text-slate-400">
            Enter a season label or ID once (for example{' '}
            <span className="text-slate-200">2024/2025</span>) and import it
            across Sportmonks leagues in safe batches of 10 by default, instead
            of repeating the single-league import league by league. Finished
            matches also get synthetic demo odds when real odds are unavailable,
            so the High Odds Intelligence page has data to analyse.
          </p>
          <div className="mt-3 grid gap-4 md:grid-cols-[1fr_auto_auto_auto] md:items-end">
            <label className="block text-sm text-slate-300">
              Season label or ID
              <input
                type="text"
                value={bulkSeasonLabel}
                onChange={(event) => setBulkSeasonLabel(event.target.value)}
                placeholder="2024/2025"
                disabled={importingBulk}
                maxLength={64}
                className="mt-1 block w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-white placeholder:text-slate-500 disabled:opacity-60"
              />
            </label>
            <label className="block text-sm text-slate-300">
              Max leagues
              <input
                type="number"
                min={1}
                max={250}
                value={bulkMaxLeagues}
                onChange={(event) => setBulkMaxLeagues(event.target.value)}
                disabled={importingBulk}
                className="mt-1 block w-28 rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-white disabled:opacity-60"
              />
            </label>
            <label className="block text-sm text-slate-300">
              Skip leagues
              <input
                type="number"
                min={0}
                value={bulkSkipLeagues}
                onChange={(event) => setBulkSkipLeagues(event.target.value)}
                disabled={importingBulk}
                className="mt-1 block w-28 rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-white disabled:opacity-60"
              />
            </label>
            <button
              type="button"
              onClick={() => void importAllLeagues()}
              disabled={!bulkSeasonLabel.trim() || importingBulk}
              className="rounded-md bg-green-600 px-3 py-2 text-sm font-medium text-white transition hover:bg-green-500 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {importingBulk ? 'Importing all leagues…' : 'Import all leagues'}
            </button>
          </div>
          {importingBulk && (
            <p className="mt-3 text-sm text-amber-300">
              Importing every league for this season. This may take several
              minutes.
            </p>
          )}
          {bulkResult && (
            <div className="mt-3 space-y-2 text-sm" role="status">
              <p className="text-emerald-300">
                Imported {bulkResult.leaguesImported} league
                {bulkResult.leaguesImported === 1 ? '' : 's'} for season{' '}
                {bulkResult.seasonLabelOrId}: {bulkResult.fixturesUpserted}{' '}
                fixtures, {bulkResult.oddsSnapshotsInserted} real odds
                snapshots, {bulkResult.demoOddsSnapshotsInserted} synthetic
                demo snapshots.
              </p>
              {bulkResult.leaguesSkipped > 0 && (
                <p className="text-amber-300">
                  Skipped {bulkResult.leaguesSkipped} league
                  {bulkResult.leaguesSkipped === 1 ? '' : 's'}:
                </p>
              )}
              {bulkResult.skipped.length > 0 && (
                <ul className="list-disc space-y-1 pl-5 text-slate-400">
                  {bulkResult.skipped.map((skip) => (
                    <li key={skip.leagueId}>
                      {skip.leagueName} ({skip.leagueId}): {skip.reason}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>
        <div className="mt-6 border-t border-slate-800 pt-6">
          <h3 className="text-base font-semibold text-white">
            Seed demo odds for analysis
          </h3>
          <p className="mt-1 text-sm text-slate-400">
            Sportmonks only exposes its odds endpoints with the Odds add-on.
            Until that is enabled, this seeds clearly-labelled synthetic 1X2
            odds (bookmaker{' '}
            <span className="text-slate-200">Demo High Odds Lab</span>) for the
            finished matches already in the database, so pages like High Odds
            Intelligence, match detail and movement actually render data.
          </p>
          <button
            type="button"
            onClick={() => void seedDemoOdds()}
            disabled={seedingDemo}
            className="mt-3 rounded-md bg-slate-700 px-3 py-2 text-sm font-medium text-white transition hover:bg-slate-600 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {seedingDemo ? 'Seeding demo odds…' : 'Seed demo odds'}
          </button>
          {demoResult && (
            <p className="mt-3 text-sm text-emerald-300" role="status">
              Processed {demoResult.matchesProcessed} finished matches and
              inserted {demoResult.snapshotsInserted} synthetic demo snapshots.
            </p>
          )}
        </div>
      </Card>
      )}
    </div>
  )
}

function MarketRealityKpi({ state }: { state: MarketOutcomeState }) {
  if (state.kind === 'loading') {
    return <Card><Spinner label="Loading Market vs Reality…" /></Card>
  }
  if (state.kind === 'error') {
    return <Card><p className="text-sm text-red-400">Market vs Reality unavailable: {state.message}</p></Card>
  }
  const { summary } = state.data
  return (
    <Card
      title="Market vs Reality · 1X2"
      actions={<Link to="/market-outcomes" className="text-xs font-semibold text-emerald-400 hover:text-emerald-300">Open analysis →</Link>}
    >
      <div className="grid grid-cols-2 gap-3 md:grid-cols-5">
        <Metric label="Markets" value={formatNum(summary.totalMarkets)} />
        <Metric label="Favorite hit" value={formatPct(summary.actualProbabilityPercentage ?? 0)} />
        <Metric label="Failure" value={formatPct(summary.favoriteFailureRatePercentage ?? 0)} />
        <Metric label="Upsets" value={formatNum(summary.upsetCount)} />
        <Metric label="Flat ROI" value={`${summary.roiPercentage.toFixed(2)}%`} />
      </div>
    </Card>
  )
}

function Metric({ label, value }: { label: string; value: string }) {
  return <div className="rounded-lg border border-white/5 bg-white/[0.02] p-3"><p className="text-[10px] font-bold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-1 text-lg font-bold text-white">{value}</p></div>
}

function KpiStrip({
  summary,
  highOdds,
  threshold,
  onRetry,
}: {
  summary: SummaryState
  highOdds: HighOddsState
  threshold: number
  onRetry: () => void
}) {
  if (summary.kind === 'loading') {
    return (
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-6">
        {Array.from({ length: 6 }).map((_, index) => (
          <div
            key={index}
            className="rounded-xl border border-white/5 bg-[#0a0f1c] p-4"
          >
            <div className="h-3 w-20 animate-pulse rounded bg-white/10" />
            <div className="mt-2 h-6 w-24 animate-pulse rounded bg-white/10" />
          </div>
        ))}
      </div>
    )
  }
  if (summary.kind === 'error') {
    return (
      <div className="flex items-center justify-between rounded-xl border border-red-500/30 bg-red-500/10 p-4">
        <p className="text-sm text-red-400">
          Could not load summary: {summary.message}
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

  const data = summary.summary
  const perf = highOdds.kind === 'ready' ? highOdds.data.summary : null
  const items: ReadonlyArray<{ label: string; value: string; tone: string }> = [
    { label: 'Matches', value: formatNum(data.totalMatches), tone: 'text-white' },
    {
      label: 'Finished',
      value: formatNum(data.finishedMatches),
      tone: 'text-white',
    },
    {
      label: 'Odds snapshots',
      value: formatNum(data.oddsSnapshotsCount),
      tone: 'text-white',
    },
    {
      label: `Upsets ≥ ${threshold}.00`,
      value: perf ? formatNum(perf.qualifyingSelections) : '—',
      tone: 'text-amber-300',
    },
    {
      label: 'Upset win rate',
      value: perf ? formatPct(perf.winRatePercentage) : '—',
      tone: 'text-sky-300',
    },
    {
      label: 'Flat ROI',
      value: perf ? formatPct(perf.roiPercentage) : '—',
      tone:
        perf && perf.roiPercentage >= 0 ? 'text-emerald-400' : 'text-red-400',
    },
  ]

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-6">
      {items.map((item) => (
        <div
          key={item.label}
          className="rounded-xl border border-white/5 bg-[#0a0f1c] p-4"
        >
          <p className="text-[11px] font-semibold uppercase tracking-wide text-slate-500">
            {item.label}
          </p>
          <p className={`mt-1.5 text-xl font-bold tabular-nums ${item.tone}`}>
            {item.value}
          </p>
        </div>
      ))}
    </div>
  )
}

function UpsetDistribution({
  state,
  threshold,
  periodDays,
}: {
  state: HighOddsState
  threshold: number
  periodDays: number
}) {
  if (state.kind === 'loading') {
    return <Spinner label="Loading upset distribution…" />
  }
  if (state.kind === 'error') {
    return <p className="text-sm text-red-400">{state.message}</p>
  }

  const { data } = state
  const ranges = data.byOddsRange
  if (ranges.length === 0) {
    return (
      <p className="text-sm text-slate-400">
        No selections with winner odds ≥ {threshold}.00 in the last{' '}
        {periodDays} days. Import a season or seed demo odds from the ingestion
        panel.
      </p>
    )
  }

  const maxSelections = Math.max(1, ...ranges.map((r) => r.selections))

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-x-6 gap-y-1 text-xs text-slate-500">
        <span>
          Sample:{' '}
          <span className="font-semibold text-slate-300">
            {formatNum(data.summary.qualifyingSelections)}
          </span>{' '}
          selections
        </span>
        <span>
          Avg odds:{' '}
          <span className="font-semibold text-amber-300">
            {formatOdds(data.summary.averageOdds)}
          </span>
        </span>
        <span>
          P/L:{' '}
          <span
            className={`font-semibold ${
              data.summary.profitUnits >= 0 ? 'text-emerald-400' : 'text-red-400'
            }`}
          >
            {formatMoney(data.summary.profitUnits)}
          </span>
        </span>
      </div>
      {ranges.map((range) => (
        <div key={range.range}>
          <div className="mb-1 flex items-center justify-between text-xs">
            <span className="font-semibold text-slate-300">{range.range}</span>
            <span className="tabular-nums text-slate-500">
              {formatNum(range.selections)} picks ·{' '}
              {formatPct(range.winRatePercentage)} wins ·{' '}
              <span
                className={
                  range.profitUnits >= 0 ? 'text-emerald-400' : 'text-red-400'
                }
              >
                {formatMoney(range.profitUnits)}
              </span>
            </span>
          </div>
          <div className="h-3 overflow-hidden rounded-full bg-white/5">
            <div
              className="h-full rounded-full bg-gradient-to-r from-amber-500/80 to-amber-300"
              style={{
                width: `${(range.selections / maxSelections) * 100}%`,
              }}
            />
          </div>
        </div>
      ))}
      <p className="text-[11px] leading-relaxed text-slate-500">
        Rare high-odds ranges are inherently noisy; check the sample size
        before reading into ROI.
      </p>
    </div>
  )
}

function MarketCoverage({ summary }: { summary: SummaryState }) {
  if (summary.kind === 'loading') {
    return <Spinner label="Loading coverage…" />
  }
  if (summary.kind === 'error') {
    return <p className="text-sm text-red-400">{summary.message}</p>
  }

  const data = summary.summary
  const statuses = Object.entries(data.matchesByStatus).sort(
    (a, b) => b[1] - a[1],
  )
  const maxStatus = Math.max(1, ...statuses.map(([, count]) => count))

  const facts: ReadonlyArray<{ label: string; value: string }> = [
    { label: 'Bookmakers', value: formatNum(data.bookmakersCount) },
    { label: 'Markets', value: formatNum(data.marketsCount) },
    { label: 'Competitions', value: formatNum(data.competitionsCount) },
    { label: 'Teams', value: formatNum(data.teamsCount) },
    { label: 'Analyzed', value: formatNum(data.analyzedMatches) },
    { label: 'Backlog', value: formatNum(data.analysisBacklog) },
  ]

  return (
    <div className="space-y-4">
      <div className="space-y-2.5">
        {statuses.map(([status, count]) => (
          <div key={status}>
            <div className="mb-1 flex items-center justify-between text-xs">
              <span className="font-semibold uppercase tracking-wide text-slate-400">
                {status}
              </span>
              <span className="tabular-nums text-slate-500">
                {formatNum(count)}
              </span>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-white/5">
              <div
                className={`h-full rounded-full ${
                  status === 'Finished' ? 'bg-emerald-400' : 'bg-sky-400/70'
                }`}
                style={{ width: `${(count / maxStatus) * 100}%` }}
              />
            </div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-3 gap-2 border-t border-white/5 pt-3">
        {facts.map((fact) => (
          <div key={fact.label}>
            <p className="text-[10px] uppercase tracking-wide text-slate-500">
              {fact.label}
            </p>
            <p className="text-sm font-bold tabular-nums text-slate-200">
              {fact.value}
            </p>
          </div>
        ))}
      </div>

      <p className="text-[11px] text-slate-500">
        Data freshness:{' '}
        <span className="text-slate-400">
          {data.dataFreshnessUtc ? formatTimestamp(data.dataFreshnessUtc) : '—'}
        </span>
      </p>
    </div>
  )
}

function TopUpsets({ state }: { state: HighOddsState }) {
  if (state.kind === 'loading') {
    return <Spinner label="Loading biggest upsets…" />
  }
  if (state.kind === 'error') {
    return <p className="text-sm text-red-400">{state.message}</p>
  }

  const rows = [...state.data.results]
    .sort((a, b) => b.odds - a.odds)
    .slice(0, 8)
  if (rows.length === 0) {
    return (
      <p className="text-sm text-slate-400">
        No qualifying selections in this window.
      </p>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-white/10 text-[11px] uppercase tracking-wide text-slate-500">
            <th className="py-2 pr-3 font-semibold">Kickoff</th>
            <th className="py-2 pr-3 font-semibold">Match</th>
            <th className="py-2 pr-3 font-semibold">Pick</th>
            <th className="py-2 pr-3 text-right font-semibold">Odds</th>
            <th className="py-2 pr-3 text-right font-semibold">Result</th>
            <th className="py-2 text-right font-semibold">P/L</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={`${row.matchId}-${row.selection}`}
              className="border-b border-white/5 last:border-0"
            >
              <td className="py-2 pr-3 text-xs text-slate-500">
                {formatKickoff(row.kickoffUtc)}
              </td>
              <td className="py-2 pr-3">
                <Link
                  to={`/matches/${row.matchId}`}
                  className="font-medium text-slate-200 transition hover:text-emerald-300"
                >
                  {row.homeTeamName}
                </Link>
                <div className="text-xs text-slate-500">
                  {row.awayTeamName}
                </div>
              </td>
              <td className="py-2 pr-3 text-xs font-bold text-slate-300">
                {row.selection}
              </td>
              <td className="py-2 pr-3 text-right font-bold tabular-nums text-amber-300">
                {formatOdds(row.odds)}
              </td>
              <td
                className={`py-2 pr-3 text-right text-xs font-semibold ${
                  row.won ? 'text-emerald-400' : 'text-red-400'
                }`}
              >
                {row.result}
              </td>
              <td
                className={`py-2 text-right text-xs font-bold tabular-nums ${
                  row.profitUnits >= 0 ? 'text-emerald-400' : 'text-red-400'
                }`}
              >
                {formatMoney(row.profitUnits)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function RecentMatches({
  state,
  onRetry,
}: {
  state: RecentState
  onRetry: () => void
}) {
  if (state.kind === 'loading') return <Spinner label="Loading recent matches…" />
  if (state.kind === 'error') {
    return (
      <div className="flex items-center justify-between">
        <p className="text-sm text-red-400">
          Could not load recent matches: {state.message}
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

  const columns: ReadonlyArray<Column<RecentMatch>> = [
    { header: 'Kickoff', render: (m) => formatKickoff(m.kickoffUtc) },
    { header: 'Home', render: (m) => m.homeTeamName || m.homeTeamId },
    { header: 'Away', render: (m) => m.awayTeamName || m.awayTeamId },
    {
      header: 'Score',
      render: (m) => formatScore(m),
      className: 'text-center',
    },
    { header: 'Result', render: (m) => resultLabel(m) },
    { header: 'Status', render: (m) => <StatusBadge status={m.status} /> },
    {
      header: '',
      render: (m) => (
        <Link
          to={`/matches/${m.id}`}
          className="text-sm font-medium text-green-400 transition hover:text-green-300"
        >
          View
        </Link>
      ),
      className: 'text-right',
    },
  ]

  return (
    <DataTable
      columns={columns}
      rows={state.matches}
      rowKey={(m) => m.id}
      emptyMessage="No matches yet. Run ingestion below."
    />
  )
}

function HealthChip({ state }: { state: HealthState }) {
  if (state.kind === 'loading') {
    return (
      <span className="inline-flex items-center gap-2 rounded-full border border-slate-700 bg-slate-800/60 px-3 py-1 text-xs text-slate-300">
        <span className="h-2 w-2 animate-pulse rounded-full bg-amber-400" />
        Checking…
      </span>
    )
  }
  if (state.kind === 'healthy') {
    return (
      <span className="inline-flex items-center gap-2 rounded-full border border-green-500/30 bg-green-500/15 px-3 py-1 text-xs text-green-400">
        <span className="h-2 w-2 rounded-full bg-green-500" />
        API {state.status}
      </span>
    )
  }
  return (
    <span
      title={state.message}
      className="inline-flex items-center gap-2 rounded-full border border-red-500/30 bg-red-500/15 px-3 py-1 text-xs text-red-400"
    >
      <span className="h-2 w-2 rounded-full bg-red-500" />
      API unreachable
    </span>
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

export default Dashboard
