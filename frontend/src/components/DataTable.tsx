import type { ReactNode } from 'react'

export interface Column<T> {
  /** Header label. */
  header: string
  /** Cell renderer for a given row. */
  render: (row: T) => ReactNode
  /** Optional extra classes for the cell/header (e.g. text alignment). */
  className?: string
}

interface DataTableProps<T> {
  columns: ReadonlyArray<Column<T>>
  rows: ReadonlyArray<T>
  rowKey: (row: T, index: number) => string
  /** Message shown when there are no rows. */
  emptyMessage?: ReactNode
  /** Optional per-row click handler (renders rows as interactive). */
  onRowClick?: (row: T) => void
}

/** Generic table with a dark theme, header row, and empty state. */
export function DataTable<T>({
  columns,
  rows,
  rowKey,
  emptyMessage = 'No data.',
  onRowClick,
}: DataTableProps<T>) {
  if (rows.length === 0) {
    return <p className="py-6 text-sm text-slate-400">{emptyMessage}</p>
  }

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full border-collapse text-sm">
        <thead>
          <tr className="border-b border-slate-800 text-left text-slate-400">
            {columns.map((column) => (
              <th
                key={column.header}
                className={`px-3 py-2 font-medium ${column.className ?? ''}`}
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr
              key={rowKey(row, index)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              className={`border-b border-slate-800/60 text-slate-200 ${
                onRowClick
                  ? 'cursor-pointer transition hover:bg-slate-800/40'
                  : ''
              }`}
            >
              {columns.map((column) => (
                <td
                  key={column.header}
                  className={`px-3 py-2 ${column.className ?? ''}`}
                >
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default DataTable
