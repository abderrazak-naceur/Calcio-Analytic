import { NavLink, Outlet } from 'react-router-dom'

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/matches', label: 'Matches', end: false },
  { to: '/movement', label: 'Movement', end: false },
  { to: '/bookmakers', label: 'Bookmakers', end: false },
  { to: '/patterns', label: 'Patterns', end: false },
  { to: '/high-odds', label: 'High Odds Intelligence', end: false },
  { to: '/market-outcomes', label: 'Market Failures', end: false },
  { to: '/backtesting', label: 'Backtesting Lab', end: false },
] as const

/** App shell: top navigation plus the routed page content. */
export function Layout() {
  return (
    <div className="min-h-screen bg-slate-950 text-slate-100">
      <header className="border-b border-slate-800 bg-slate-900/60">
        <nav className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <NavLink to="/" className="text-lg font-bold tracking-tight text-white">
            Calcio<span className="text-green-500">-Analytic</span>
          </NavLink>
          <div className="flex items-center gap-1">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  `rounded-md px-3 py-1.5 text-sm font-medium transition ${
                    isActive
                      ? 'bg-slate-800 text-white'
                      : 'text-slate-400 hover:bg-slate-800/60 hover:text-slate-200'
                  }`
                }
              >
                {item.label}
              </NavLink>
            ))}
          </div>
        </nav>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}

export default Layout
