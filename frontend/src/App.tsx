import { useCallback, useEffect, useState } from 'react'
import { API_BASE_URL, getHealth } from './lib/apiClient'

type HealthState =
  | { kind: 'loading' }
  | { kind: 'healthy'; status: string }
  | { kind: 'error'; message: string }

function App() {
  const [health, setHealth] = useState<HealthState>({ kind: 'loading' })

  const checkHealth = useCallback(async () => {
    setHealth({ kind: 'loading' })
    try {
      const result = await getHealth()
      setHealth({ kind: 'healthy', status: result.status })
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Unknown error contacting the API'
      setHealth({ kind: 'error', message })
    }
  }, [])

  useEffect(() => {
    void checkHealth()
  }, [checkHealth])

  return (
    <main className="min-h-screen bg-slate-950 text-slate-100 flex items-center justify-center px-4">
      <div className="w-full max-w-xl">
        <header className="mb-8 text-center">
          <h1 className="text-4xl font-bold tracking-tight text-white">
            Calcio<span className="text-green-500">-Analytic</span>
          </h1>
          <p className="mt-2 text-slate-400">
            Football odds &amp; match analytics platform
          </p>
        </header>

        <section className="rounded-xl border border-slate-800 bg-slate-900/60 p-6 shadow-lg">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-200">Backend status</h2>
            <button
              type="button"
              onClick={() => void checkHealth()}
              disabled={health.kind === 'loading'}
              className="rounded-md bg-slate-800 px-3 py-1.5 text-sm font-medium text-slate-200 transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              Refresh
            </button>
          </div>

          <div className="mt-4">
            <HealthBadge state={health} />
          </div>

          <p className="mt-4 text-xs text-slate-500">
            API base URL:{' '}
            <code className="rounded bg-slate-800 px-1.5 py-0.5 text-slate-300">
              {API_BASE_URL}
            </code>
          </p>
        </section>
      </div>
    </main>
  )
}

function HealthBadge({ state }: { state: HealthState }) {
  if (state.kind === 'loading') {
    return (
      <div className="flex items-center gap-3 text-slate-300">
        <span className="h-3 w-3 animate-pulse rounded-full bg-amber-400" />
        <span>Checking backend health…</span>
      </div>
    )
  }

  if (state.kind === 'healthy') {
    return (
      <div className="flex items-center gap-3 text-green-400">
        <span className="h-3 w-3 rounded-full bg-green-500" />
        <span className="font-medium">
          Backend is reachable — {state.status}
        </span>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-1">
      <div className="flex items-center gap-3 text-red-400">
        <span className="h-3 w-3 rounded-full bg-red-500" />
        <span className="font-medium">Backend unreachable</span>
      </div>
      <p className="pl-6 text-sm text-slate-400">{state.message}</p>
    </div>
  )
}

export default App
