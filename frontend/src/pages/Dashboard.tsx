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

function formatTimestamp(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString()
}

function formatKickoff(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString()
}

function formatScore(match: RecentMatch): string {
  if (match.homeScore === null || match.awayScore === null) return '—'
  return `${match.homeScore} – ${match.awayScore}`
}

function Dashboard() {
  const [summary, setSummary] = useState<SummaryState>({ kind: 'loading' })
  const [recent, setRecent] = useState<RecentState>({ kind: 'loading' })
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })
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
      const data = await getRecentMatches(10)
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

  useEffect(() => {
    void loadSummary()
    void loadRecent()
    void loadHealth()
  }, [loadSummary, loadRecent, loadHealth])

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
    <div className="space-y-6">
      <header className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Dashboard</h1>
          <p className="text-sm text-slate-400">
            Overview of matches and analytics coverage.
          </p>
        </div>
        <HealthChip state={health} />
      </header>

      <KpiRow state={summary} onRetry={() => void loadSummary()} />

      <Card title="Recent matches">
        <RecentMatches state={recent} onRetry={() => void loadRecent()} />
      </Card>

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
    </div>
  )
}

function KpiRow({
  state,
  onRetry,
}: {
  state: SummaryState
  onRetry: () => void
}) {
  if (state.kind === 'loading') {
    return (
      <Card>
        <Spinner label="Loading KPIs…" />
      </Card>
    )
  }
  if (state.kind === 'error') {
    return (
      <Card>
        <div className="flex items-center justify-between">
          <p className="text-sm text-red-400">
            Could not load summary: {state.message}
          </p>
          <button
            type="button"
            onClick={onRetry}
            className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
          >
            Retry
          </button>
        </div>
      </Card>
    )
  }

  const { summary } = state
  const items: ReadonlyArray<{ label: string; value: string }> = [
    { label: 'Total matches', value: String(summary.totalMatches) },
    { label: 'Finished', value: String(summary.finishedMatches) },
    { label: 'Analyzed', value: String(summary.analyzedMatches) },
    { label: 'Analysis backlog', value: String(summary.analysisBacklog) },
    { label: 'Odds snapshots', value: String(summary.oddsSnapshotsCount) },
    { label: 'Bookmakers', value: String(summary.bookmakersCount) },
    { label: 'Markets', value: String(summary.marketsCount) },
    { label: 'Competitions', value: String(summary.competitionsCount) },
    { label: 'Teams', value: String(summary.teamsCount) },
    {
      label: 'Data freshness',
      value: summary.dataFreshnessUtc
        ? formatTimestamp(summary.dataFreshnessUtc)
        : '—',
    },
  ]

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      {items.map((item) => (
        <div
          key={item.label}
          className="rounded-xl border border-slate-800 bg-slate-900/60 p-5 shadow-lg"
        >
          <p className="text-sm text-slate-400">{item.label}</p>
          <p className="mt-1 text-2xl font-bold text-white">{item.value}</p>
        </div>
      ))}
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
