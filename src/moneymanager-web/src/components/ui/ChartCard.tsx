import type { ReactNode } from 'react'
import { Card } from './Card'
import { Spinner } from './Spinner'

interface ChartCardProps {
  title?: string
  /** Show a centered spinner instead of the chart. */
  isLoading?: boolean
  /** Show the empty message instead of the chart. */
  isEmpty?: boolean
  emptyMessage?: string
  /** Height of the loading/empty placeholder; should match the chart height. */
  height?: number
  className?: string
  children: ReactNode
}

/**
 * Card shell for charts (ADR-0006): a titled `Card` that renders a centered
 * spinner while loading, an empty message when there's no data, or the chart.
 * Keeps loading/empty handling out of every chart page.
 */
export function ChartCard({
  title,
  isLoading = false,
  isEmpty = false,
  emptyMessage = 'No data for this period',
  height = 320,
  className,
  children,
}: ChartCardProps) {
  return (
    <Card title={title} className={className}>
      {isLoading ? (
        <div className="flex items-center justify-center" style={{ height }}>
          <Spinner size="lg" />
        </div>
      ) : isEmpty ? (
        <div
          className="flex items-center justify-center text-sm text-gray-400"
          style={{ height }}
        >
          {emptyMessage}
        </div>
      ) : (
        children
      )}
    </Card>
  )
}
