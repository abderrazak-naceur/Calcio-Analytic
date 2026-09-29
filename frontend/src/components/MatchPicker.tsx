import { useEffect, useMemo, useState } from 'react'
import { ApiError, getMatches } from '../lib/apiClient'
import type { MatchSummary } from '../lib/types'

interface MatchPickerProps {
  /** Currently selected match id (from the querystring). */
  value: string
  /** Called when the user picks a match id (dropdown or text input). */
  onChange: (matchId: string) => void
}

type PickerState =
  | { kind: 'loading' }
  | { kind: 'ready'; matches: MatchSummary[] }
  | { kind: 'error'; message: string }

function formatKickoff(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return iso
  return date.toLocaleString()
}

function matchLabel(match: MatchSummary): string {
  return `${formatKickoff(match.kickoffUtc)} · ${match.homeTeamId} vs ${match.awayTeamId} · ${match.status}`
}

/**
 * Reusable match selector: a dropdown populated from GET /api/v1/matches plus
 * a free-text input so an arbitrary match id can be entered/shared directly.
 */
export function MatchPicker({ value, onChange }: MatchPickerProps) {
  const [state, setState] = useState<PickerState>({ kind: 'loading' })
  const [text, setText] = useState<string>(value)

  useEffect(() => {
    setText(value)
  }, [value])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      setState({ kind: 'loading' })
      try {
        const matches = await getMatches()
        if (!cancelled) setState({ kind: 'ready', matches })
      } catch (error) {
        if (!cancelled) {
          setState({ kind: 'error', message: describeError(error) })
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const matches = useMemo(
    () => (state.kind === 'ready' ? state.matches : []),
    [state],
  )

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end">
      <label className="flex flex-col gap-1 text-sm text-slate-300">
        <span className="text-slate-400">Match</span>
        <select
          value={value}
          disabled={state.kind !== 'ready'}
          onChange={(event) => onChange(event.target.value)}
          className="min-w-[22rem] rounded-md border border-slate-700 bg-slate-800 px-3 py-1.5 text-sm text-slate-200 focus:border-green-500 focus:outline-none disabled:opacity-60"
        >
          <option value="">
            {state.kind === 'loading'
              ? 'Loading matches…'
              : state.kind === 'error'
                ? 'Could not load matches'
                : 'Select a match…'}
          </option>
          {matches.map((match) => (
            <option key={match.id} value={match.id}>
              {matchLabel(match)}
            </option>
          ))}
        </select>
      </label>

      <form
        className="flex items-end gap-2"
        onSubmit={(event) => {
          event.preventDefault()
          onChange(text.trim())
        }}
      >
        <label className="flex flex-col gap-1 text-sm text-slate-300">
          <span className="text-slate-400">Or match id</span>
          <input
            type="text"
            value={text}
            onChange={(event) => setText(event.target.value)}
            placeholder="Paste a match id"
            className="min-w-[16rem] rounded-md border border-slate-700 bg-slate-800 px-3 py-1.5 text-sm text-slate-200 focus:border-green-500 focus:outline-none"
          />
        </label>
        <button
          type="submit"
          className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700"
        >
          Load
        </button>
      </form>

      {state.kind === 'error' && (
        <p className="w-full text-sm text-red-400">
          Match list unavailable: {state.message}. You can still enter a match id
          above.
        </p>
      )}
    </div>
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

export default MatchPicker
