interface SpinnerProps {
  label?: string
}

/** Inline loading indicator consistent with the dark theme. */
export function Spinner({ label = 'Loading…' }: SpinnerProps) {
  return (
    <div className="flex items-center gap-3 text-slate-300">
      <span
        className="h-4 w-4 animate-spin rounded-full border-2 border-slate-600 border-t-green-500"
        aria-hidden="true"
      />
      <span>{label}</span>
    </div>
  )
}

export default Spinner
