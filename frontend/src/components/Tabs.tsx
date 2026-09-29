interface TabsProps {
  tabs: readonly string[]
  active: string
  onChange: (tab: string) => void
}

/** Horizontal tab strip. Controlled via `active` / `onChange`. */
export function Tabs({ tabs, active, onChange }: TabsProps) {
  return (
    <div
      role="tablist"
      className="flex flex-wrap gap-1 border-b border-slate-800"
    >
      {tabs.map((tab) => {
        const isActive = tab === active
        return (
          <button
            key={tab}
            type="button"
            role="tab"
            aria-selected={isActive}
            onClick={() => onChange(tab)}
            className={`-mb-px rounded-t-md border-b-2 px-4 py-2 text-sm font-medium transition ${
              isActive
                ? 'border-green-500 text-white'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            {tab}
          </button>
        )
      })}
    </div>
  )
}

export default Tabs
