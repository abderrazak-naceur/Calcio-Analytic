interface StatusBadgeProps {
  status: string
}

/** Maps a match status to a colored pill. Unknown statuses fall back to slate. */
function toneFor(status: string): string {
  const normalized = status.toLowerCase()
  if (normalized.includes('live')) {
    return 'bg-red-500/15 text-red-400 border-red-500/30'
  }
  if (normalized.includes('finish') || normalized.includes('final')) {
    return 'bg-green-500/15 text-green-400 border-green-500/30'
  }
  if (normalized.includes('sched') || normalized.includes('upcoming')) {
    return 'bg-sky-500/15 text-sky-400 border-sky-500/30'
  }
  if (normalized.includes('cancel') || normalized.includes('postpon')) {
    return 'bg-amber-500/15 text-amber-400 border-amber-500/30'
  }
  return 'bg-slate-700/40 text-slate-300 border-slate-600/40'
}

/** Small pill that renders a match/analysis status with a semantic color. */
export function StatusBadge({ status }: StatusBadgeProps) {
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium ${toneFor(
        status,
      )}`}
    >
      {status}
    </span>
  )
}

export default StatusBadge
