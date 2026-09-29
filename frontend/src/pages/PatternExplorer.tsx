import { useCallback, useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Card } from '../components/Card'
import { DataTable, type Column } from '../components/DataTable'
import { Spinner } from '../components/Spinner'
import { ApiError, postPatternQuery } from '../lib/apiClient'
import type { PatternQueryRequest, PatternResult } from '../lib/types'

/** Filter fields synced to the querystring so a query is shareable. */
const FILTER_KEYS = [
  'competitionId',
  'seasonId',
  'fromUtc',
  'toUtc',
  'homeOrAway',
  'minClosingHomeOdds',
  'maxClosingHomeOdds',
  'minMovementPercentage',
  'maxMovementPercentage',
] as const

type FilterKey = (typeof FILTER_KEYS)[number]

type FormState = Record<FilterKey, string>

const EMPTY_FORM: FormState = {
  competitionId: '',
  seasonId: '',
  fromUtc: '',
  toUtc: '',
  homeOrAway: '',
  minClosingHomeOdds: '',
  maxClosingHomeOdds: '',
  minMovementPercentage: '',
  maxMovementPercentage: '',
}

type QueryState =
  | { kind: 'idle' }
  | { kind: 'loading' }
  | { kind: 'ready'; result: PatternResult }
  | { kind: 'error'; message: string }

const NUMERIC_KEYS: ReadonlySet<FilterKey> = new Set<FilterKey>([
  'minClosingHomeOdds',
  'maxClosingHomeOdds',
  'minMovementPercentage',
  'maxMovementPercentage',
])

function num(value: number, digits = 2): string {
  return Number.isFinite(value) ? value.toFixed(digits) : '—'
}

function pct(value: number): string {
  return Number.isFinite(value) ? `${value.toFixed(1)}%` : '—'
}

/** Builds a request body from the form, omitting blank fields. */
function toRequest(form: FormState): PatternQueryRequest {
  const body: PatternQueryRequest = {}
  for (const key of FILTER_KEYS) {
    const raw = form[key].trim()
    if (!raw) continue
    if (NUMERIC_KEYS.has(key)) {
      const parsed = Number(raw)
      if (!Number.isNaN(parsed)) {
        ;(body as Record<string, number>)[key] = parsed
      }
    } else {
      ;(body as Record<string, string>)[key] = raw
    }
  }
  return body
}

function formFromParams(params: URLSearchParams): FormState {
  const next: FormState = { ...EMPTY_FORM }
  for (const key of FILTER_KEYS) {
    const value = params.get(key)
    if (value !== null) next[key] = value
  }
  return next
}

interface DistributionRow {
  outcome: string
  count: number
  percentage: number
}

function PatternExplorer() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [form, setForm] = useState<FormState>(() =>
    formFromParams(searchParams),
  )
  const [state, setState] = useState<QueryState>({ kind: 'idle' })

  const hasQueryParams = useMemo(
    () => FILTER_KEYS.some((key) => searchParams.get(key) !== null),
    [searchParams],
  )

  const runQuery = useCallback(async (body: PatternQueryRequest) => {
    setState({ kind: 'loading' })
    try {
      const result = await postPatternQuery(body)
      setState({ kind: 'ready', result })
    } catch (error) {
      setState({ kind: 'error', message: describeError(error) })
    }
  }, [])

  // Run automatically on mount when the URL already carries filters (shared link).
  useEffect(() => {
    if (hasQueryParams) {
      void runQuery(toRequest(formFromParams(searchParams)))
    }
    // Only run on mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const handleChange = (key: FilterKey, value: string) => {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const nextParams = new URLSearchParams()
    for (const key of FILTER_KEYS) {
      const value = form[key].trim()
      if (value) nextParams.set(key, value)
    }
    setSearchParams(nextParams)
    void runQuery(toRequest(form))
  }

  const handleReset = () => {
    setForm(EMPTY_FORM)
    setSearchParams(new URLSearchParams())
    setState({ kind: 'idle' })
  }

  return (
    <div className="space-y-6">
      <header>
        <h1 className="text-2xl font-bold text-white">Pattern explorer</h1>
        <p className="text-sm text-slate-400">
          Query historical matches by competition, season, date range, closing
          odds, and movement to see how patterns resolved.
        </p>
      </header>

      <Card title="Filters">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <TextField
              label="Competition ID"
              placeholder="UUID"
              value={form.competitionId}
              onChange={(v) => handleChange('competitionId', v)}
            />
            <TextField
              label="Season ID"
              placeholder="UUID"
              value={form.seasonId}
              onChange={(v) => handleChange('seasonId', v)}
            />
            <SelectField
              label="Home or away"
              value={form.homeOrAway}
              onChange={(v) => handleChange('homeOrAway', v)}
              options={[
                { value: '', label: 'Any' },
                { value: 'Home', label: 'Home' },
                { value: 'Away', label: 'Away' },
              ]}
            />
            <DateField
              label="From (UTC)"
              value={form.fromUtc}
              onChange={(v) => handleChange('fromUtc', v)}
            />
            <DateField
              label="To (UTC)"
              value={form.toUtc}
              onChange={(v) => handleChange('toUtc', v)}
            />
            <div />
            <NumberField
              label="Min closing home odds"
              value={form.minClosingHomeOdds}
              onChange={(v) => handleChange('minClosingHomeOdds', v)}
            />
            <NumberField
              label="Max closing home odds"
              value={form.maxClosingHomeOdds}
              onChange={(v) => handleChange('maxClosingHomeOdds', v)}
            />
            <div />
            <NumberField
              label="Min movement %"
              value={form.minMovementPercentage}
              onChange={(v) => handleChange('minMovementPercentage', v)}
            />
            <NumberField
              label="Max movement %"
              value={form.maxMovementPercentage}
              onChange={(v) => handleChange('maxMovementPercentage', v)}
            />
          </div>

          <div className="flex items-center gap-3">
            <button
              type="submit"
              className="rounded-md bg-green-600 px-4 py-2 text-sm font-medium text-white transition hover:bg-green-500"
            >
              Run query
            </button>
            <button
              type="button"
              onClick={handleReset}
              className="rounded-md bg-slate-800 px-4 py-2 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
            >
              Reset
            </button>
          </div>
        </form>
      </Card>

      <ResultSection state={state} onRetry={() => void runQuery(toRequest(form))} />
    </div>
  )
}

function ResultSection({
  state,
  onRetry,
}: {
  state: QueryState
  onRetry: () => void
}) {
  if (state.kind === 'idle') {
    return (
      <Card>
        <p className="text-sm text-slate-400">
          Set your filters and run a query to see aggregated pattern results.
        </p>
      </Card>
    )
  }

  if (state.kind === 'loading') {
    return (
      <Card>
        <Spinner label="Running pattern query…" />
      </Card>
    )
  }

  if (state.kind === 'error') {
    return (
      <Card>
        <div className="flex items-center justify-between">
          <p className="text-sm text-red-400">
            Could not run query: {state.message}
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

  const { result } = state

  if (result.sampleSize === 0) {
    return (
      <Card title="Results">
        <p className="text-sm text-slate-400">
          No matches matched these filters. Try widening the range.
        </p>
      </Card>
    )
  }

  const distributionRows: DistributionRow[] = [
    {
      outcome: 'Home',
      count: result.resultDistribution.Home,
      percentage: result.resultPercentages.Home,
    },
    {
      outcome: 'Draw',
      count: result.resultDistribution.Draw,
      percentage: result.resultPercentages.Draw,
    },
    {
      outcome: 'Away',
      count: result.resultDistribution.Away,
      percentage: result.resultPercentages.Away,
    },
  ]

  const distributionColumns: ReadonlyArray<Column<DistributionRow>> = [
    { header: 'Outcome', render: (r) => r.outcome },
    {
      header: 'Count',
      render: (r) => String(r.count),
      className: 'text-right',
    },
    {
      header: 'Percentage',
      render: (r) => pct(r.percentage),
      className: 'text-right',
    },
  ]

  const over = result.overUnderDistribution['Over2.5']
  const under = result.overUnderDistribution['Under2.5']

  return (
    <div className="space-y-6">
      <Card title={`Results (sample size ${result.sampleSize})`}>
        <dl className="grid grid-cols-1 gap-2 text-sm sm:grid-cols-2">
          <Field label="Average total goals" value={num(result.averageTotalGoals)} />
          <Field
            label="Average closing home odds"
            value={num(result.averageClosingHomeOdds)}
          />
          <Field
            label="Average movement %"
            value={num(result.averageMovementPercentage)}
          />
          <Field
            label="Data completeness"
            value={pct(result.dataCompleteness * 100)}
          />
          {result.homeWinConfidenceInterval && (
            <Field
              label="Home-win 95% CI"
              value={`${pct(result.homeWinConfidenceInterval.lower * 100)} – ${pct(
                result.homeWinConfidenceInterval.upper * 100,
              )}`}
            />
          )}
        </dl>
      </Card>

      <Card title="Result distribution">
        <DataTable
          columns={distributionColumns}
          rows={distributionRows}
          rowKey={(r) => r.outcome}
        />
      </Card>

      <Card title="Over / Under 2.5 goals">
        <dl className="grid grid-cols-1 gap-2 text-sm sm:grid-cols-2">
          <Field label="Over 2.5" value={String(over)} />
          <Field label="Under 2.5" value={String(under)} />
        </dl>
      </Card>

      <Card title="Methodology">
        <dl className="grid grid-cols-1 gap-2 text-sm">
          <Field label="Methodology" value={result.methodology} />
          <Field label="Query hash" value={result.queryHash} />
        </dl>
      </Card>
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 border-b border-slate-800/60 py-1.5">
      <dt className="text-slate-400">{label}</dt>
      <dd className="break-all text-right text-slate-200">{value}</dd>
    </div>
  )
}

function FieldLabel({
  label,
  children,
}: {
  label: string
  children: React.ReactNode
}) {
  return (
    <label className="flex flex-col gap-1 text-sm">
      <span className="text-slate-400">{label}</span>
      {children}
    </label>
  )
}

const INPUT_CLASS =
  'rounded-md border border-slate-700 bg-slate-800 px-3 py-1.5 text-sm text-slate-200 focus:border-green-500 focus:outline-none'

function TextField({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  placeholder?: string
}) {
  return (
    <FieldLabel label={label}>
      <input
        type="text"
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        className={INPUT_CLASS}
      />
    </FieldLabel>
  )
}

function NumberField({
  label,
  value,
  onChange,
}: {
  label: string
  value: string
  onChange: (value: string) => void
}) {
  return (
    <FieldLabel label={label}>
      <input
        type="number"
        step="any"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className={INPUT_CLASS}
      />
    </FieldLabel>
  )
}

function DateField({
  label,
  value,
  onChange,
}: {
  label: string
  value: string
  onChange: (value: string) => void
}) {
  return (
    <FieldLabel label={label}>
      <input
        type="datetime-local"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className={INPUT_CLASS}
      />
    </FieldLabel>
  )
}

function SelectField({
  label,
  value,
  onChange,
  options,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  options: ReadonlyArray<{ value: string; label: string }>
}) {
  return (
    <FieldLabel label={label}>
      <select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className={INPUT_CLASS}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </FieldLabel>
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

export default PatternExplorer
