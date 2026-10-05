import { NavLink, Outlet } from 'react-router-dom'

const sections = [
  {
    title: 'Overview',
    items: [
      { to: '/', label: 'Dashboard', end: true, icon: '◈' },
      { to: '/matches', label: 'Results', end: false, icon: '▤' },
      { to: '/movement', label: 'Odds Movement', end: false, icon: '〜' },
    ],
  },
  {
    title: 'Markets',
    items: [
      { to: '/high-odds', label: 'High Odds Intelligence', end: false, icon: '▲' },
      { to: '/market-outcomes', label: 'Market Failures', end: false, icon: '◆' },
      { to: '/bookmakers', label: 'Bookmakers', end: false, icon: '◐' },
      { to: '/patterns', label: 'Patterns', end: false, icon: '⬣' },
    ],
  },
  {
    title: 'Lab',
    items: [
      { to: '/backtesting', label: 'Backtesting Lab', end: false, icon: '⬢' },
    ],
  },
] as const

/** Premium trading-terminal shell: left sidebar + routed content. */
export function Layout() {
  return (
    <div className="min-h-screen bg-[#060a13] text-slate-200">
      <div className="flex min-h-screen">
        {/* Sidebar */}
        <aside className="hidden w-64 shrink-0 flex-col border-r border-white/5 bg-[#0a0f1c] lg:flex">
          <div className="flex items-center gap-2.5 px-5 pb-5 pt-6">
            <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-gradient-to-br from-emerald-400 to-teal-600 text-lg font-black text-[#060a13]">
              C
            </span>
            <div>
              <p className="text-[15px] font-bold tracking-tight text-white">
                Calcio<span className="text-emerald-400">-Analytic</span>
              </p>
              <p className="text-[10px] font-medium uppercase tracking-[0.18em] text-slate-500">
                Market Terminal
              </p>
            </div>
          </div>
          <div className="mx-5 mb-4 flex items-center gap-2 rounded-lg border border-emerald-500/20 bg-emerald-500/10 px-3 py-2">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-60" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-400" />
            </span>
            <span className="text-xs font-semibold text-emerald-300">LIVE FEED · REAL DATA</span>
          </div>
          <nav className="flex-1 space-y-5 overflow-y-auto px-3 pb-6">
            {sections.map((section) => (
              <div key={section.title}>
                <p className="px-2 pb-2 text-[10px] font-bold uppercase tracking-[0.2em] text-slate-500">
                  {section.title}
                </p>
                <div className="space-y-1">
                  {section.items.map((item) => (
                    <NavLink
                      key={item.to + item.label}
                      to={item.to}
                      end={item.end}
                      className={({ isActive }) =>
                        `group flex items-center gap-3 rounded-lg px-3 py-2 text-[13px] font-medium transition ${
                          isActive
                            ? 'bg-emerald-500/15 text-emerald-300 shadow-[inset_0_0_0_1px_rgba(52,211,153,0.25)]'
                            : 'text-slate-400 hover:bg-white/5 hover:text-slate-100'
                        }`
                      }
                    >
                      <span className="w-4 text-center text-xs opacity-70">{item.icon}</span>
                      {item.label}
                    </NavLink>
                  ))}
                </div>
              </div>
            ))}
          </nav>
          <div className="border-t border-white/5 p-4">
            <div className="rounded-lg bg-white/[0.03] p-3 text-[11px] leading-relaxed text-slate-500">
              Historical market analysis.
              <br />
              No predictions — only stats.
            </div>
          </div>
        </aside>

        {/* Main column */}
        <div className="flex min-w-0 flex-1 flex-col">
          {/* Mobile top nav */}
          <header className="border-b border-white/5 bg-[#0a0f1c]/80 lg:hidden">
            <nav className="flex items-center gap-1 overflow-x-auto px-3 py-2.5">
              <span className="mr-2 whitespace-nowrap text-sm font-bold text-white">
                Calcio<span className="text-emerald-400">-Analytic</span>
              </span>
              {sections
                .flatMap((s) => [...s.items])
                .map((item) => (
                  <NavLink
                    key={item.to + item.label}
                    to={item.to}
                    end={item.end}
                    className={({ isActive }) =>
                      `whitespace-nowrap rounded-md px-2.5 py-1.5 text-xs font-medium transition ${
                        isActive ? 'bg-emerald-500/15 text-emerald-300' : 'text-slate-400'
                      }`
                    }
                  >
                    {item.label}
                  </NavLink>
                ))}
            </nav>
          </header>
          <main className="min-w-0 flex-1 px-4 py-6 sm:px-6 lg:px-8">
            <div className="mx-auto w-full max-w-[1400px]">
              <Outlet />
            </div>
          </main>
        </div>
      </div>
    </div>
  )
}

export default Layout

