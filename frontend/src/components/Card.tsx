import type { ReactNode } from 'react'

interface CardProps {
  title?: ReactNode
  actions?: ReactNode
  children: ReactNode
  className?: string
}

/** Panel container matching the landing-page card styling. */
export function Card({ title, actions, children, className }: CardProps) {
  return (
    <section
      className={`rounded-xl border border-slate-800 bg-slate-900/60 p-6 shadow-lg ${
        className ?? ''
      }`}
    >
      {(title || actions) && (
        <div className="mb-4 flex items-center justify-between">
          {title ? (
            <h2 className="text-lg font-semibold text-slate-200">{title}</h2>
          ) : (
            <span />
          )}
          {actions}
        </div>
      )}
      {children}
    </section>
  )
}

export default Card
