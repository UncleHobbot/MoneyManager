import { useState, useCallback, type ReactNode } from 'react'
import { ArrowUp, ArrowDown, ArrowUpDown } from 'lucide-react'

export interface Column<T> {
  key: string
  header: string
  render?: (row: T) => ReactNode
  sortable?: boolean
  className?: string
}

type SortDir = 'asc' | 'desc'

interface DataTableProps<T> {
  columns: Column<T>[]
  data: T[]
  onRowClick?: (row: T) => void
  emptyMessage?: string
  className?: string
  /** Stable React key for each row. Falls back to the row index when omitted. */
  rowKey?: (row: T, index: number) => string | number
  /**
   * When provided, sorting is controlled by the parent (e.g. server-side sorting).
   * The table renders `data` as-is and reports header clicks via `onSortChange`
   * instead of sorting locally.
   */
  sortKey?: string | null
  sortDir?: SortDir
  onSortChange?: (key: string, dir: SortDir) => void
}

export function DataTable<T>({
  columns,
  data,
  onRowClick,
  emptyMessage = 'No data found',
  className = '',
  rowKey,
  sortKey: controlledSortKey,
  sortDir: controlledSortDir,
  onSortChange,
}: DataTableProps<T>) {
  const isControlled = onSortChange !== undefined
  const [localSortKey, setLocalSortKey] = useState<string | null>(null)
  const [localSortDir, setLocalSortDir] = useState<SortDir>('asc')

  const sortKey = isControlled ? controlledSortKey ?? null : localSortKey
  const sortDir = isControlled ? controlledSortDir ?? 'asc' : localSortDir

  const handleSort = useCallback(
    (key: string) => {
      const nextDir: SortDir = sortKey === key && sortDir === 'asc' ? 'desc' : 'asc'
      if (isControlled) {
        onSortChange(key, sortKey === key ? nextDir : 'asc')
      } else {
        setLocalSortKey(key)
        setLocalSortDir(sortKey === key ? nextDir : 'asc')
      }
    },
    [isControlled, onSortChange, sortKey, sortDir],
  )

  // In controlled mode the parent already returns sorted data; only sort locally otherwise.
  const sorted =
    !isControlled && sortKey
      ? [...data].sort((a, b) => {
          const av = (a as Record<string, unknown>)[sortKey]
          const bv = (b as Record<string, unknown>)[sortKey]
          if (av == null && bv == null) return 0
          if (av == null) return 1
          if (bv == null) return -1
          const cmp = av < bv ? -1 : av > bv ? 1 : 0
          return sortDir === 'asc' ? cmp : -cmp
        })
      : data

  const SortIcon = ({ colKey }: { colKey: string }) => {
    if (sortKey !== colKey) return <ArrowUpDown size={14} className="opacity-40" />
    return sortDir === 'asc' ? <ArrowUp size={14} /> : <ArrowDown size={14} />
  }

  return (
    <div className={`overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead className="bg-gray-50 dark:bg-gray-800/50">
          <tr>
            {columns.map((col) => (
              <th
                key={col.key}
                className={`px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500 dark:text-gray-400 ${
                  col.sortable ? 'cursor-pointer select-none hover:text-gray-700 dark:hover:text-gray-200 transition-colors duration-150' : ''
                } ${col.className ?? ''}`}
                onClick={col.sortable ? () => handleSort(col.key) : undefined}
              >
                <span className="inline-flex items-center gap-1">
                  {col.header}
                  {col.sortable && <SortIcon colKey={col.key} />}
                </span>
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-200 dark:divide-gray-700 bg-white dark:bg-gray-900">
          {sorted.length === 0 ? (
            <tr>
              <td
                colSpan={columns.length}
                className="px-4 py-8 text-center text-sm text-gray-400 dark:text-gray-500"
              >
                {emptyMessage}
              </td>
            </tr>
          ) : (
            sorted.map((row, i) => (
              <tr
                key={rowKey ? rowKey(row, i) : i}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                onKeyDown={
                  onRowClick
                    ? (e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault()
                          onRowClick(row)
                        }
                      }
                    : undefined
                }
                tabIndex={onRowClick ? 0 : undefined}
                role={onRowClick ? 'button' : undefined}
                className={`transition-colors duration-150 ${
                  onRowClick
                    ? 'cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 focus:outline-none focus:bg-gray-100 dark:focus:bg-gray-800'
                    : ''
                }`}
              >
                {columns.map((col) => (
                  <td
                    key={col.key}
                    className={`px-4 py-3 text-sm text-gray-900 dark:text-gray-100 ${col.className ?? ''}`}
                  >
                    {col.render
                      ? col.render(row)
                      : String((row as Record<string, unknown>)[col.key] ?? '')}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}
