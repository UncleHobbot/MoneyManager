import { useState, useMemo, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSpendingByCategory, useChartPeriods } from '@/hooks/useCharts'
import { useCategories } from '@/hooks/useCategories'
import { useTheme } from '@/components/layout/useTheme'
import { Select, Spinner, Card, CategoryIcon, EChart } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { CHART_PALETTE } from '@/lib/chartTheme'
import type { CategoryChart } from '@/types'
import type { EChartsOption } from 'echarts'

function DonutChart({
  title,
  data,
  onDrill,
}: {
  title: string
  data: CategoryChart[]
  onDrill: (name: string) => void
}) {
  const { theme } = useTheme()
  const isDark = theme === 'dark'
  const total = data.reduce((a, d) => a + d.amount, 0)

  const option = useMemo<EChartsOption>(
    () => ({
      tooltip: {
        trigger: 'item',
        valueFormatter: (val) => formatCAD(Number(val), { fractionDigits: 0 }),
      },
      title: {
        text: formatCAD(total, { fractionDigits: 0 }),
        subtext: 'Total',
        left: 'center',
        top: 'center',
        textStyle: { color: isDark ? '#F3F4F6' : '#111827', fontSize: 18 },
        subtextStyle: { color: isDark ? '#9CA3AF' : '#6B7280', fontSize: 12 },
      },
      series: [
        {
          type: 'pie',
          radius: ['62%', '90%'],
          center: ['50%', '50%'],
          avoidLabelOverlap: false,
          label: { show: false },
          labelLine: { show: false },
          data: data.map((d, i) => ({
            value: d.amount,
            name: d.name,
            itemStyle: { color: CHART_PALETTE[i % CHART_PALETTE.length] },
          })),
        },
      ],
    }),
    [data, total, isDark],
  )

  const onEvents = useMemo(
    () => ({ click: (params: { name: string }) => onDrill(params.name) }),
    [onDrill],
  )

  return (
    <Card title={title} className="flex-1 min-w-0">
      <div className="flex flex-col items-center">
        {data.length === 0 ? (
          <p className="py-12 text-sm text-gray-400">No data for this period</p>
        ) : (
          <>
            <EChart option={option} height={320} onEvents={onEvents} className="w-full" />
            <ul className="mt-4 w-full space-y-1.5 max-h-64 overflow-y-auto">
              {data.map((item, i) => (
                <li
                  key={item.name}
                  role="button"
                  tabIndex={0}
                  title={`View ${item.name} transactions`}
                  onClick={() => onDrill(item.name)}
                  onKeyDown={e => {
                    if (e.key === 'Enter' || e.key === ' ') onDrill(item.name)
                  }}
                  className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm cursor-pointer transition-colors hover:bg-gray-50 dark:hover:bg-gray-700/50"
                >
                  <span
                    className="h-3 w-3 rounded-full shrink-0"
                    style={{ backgroundColor: CHART_PALETTE[i % CHART_PALETTE.length] }}
                  />
                  <CategoryIcon icon={item.icon ?? undefined} size={16} className="shrink-0 text-gray-500 dark:text-gray-400" />
                  <span className="flex-1 truncate text-gray-900 dark:text-gray-100">
                    {item.name}
                  </span>
                  <span className="font-medium tabular-nums text-gray-900 dark:text-gray-100">
                    {formatCAD(item.amount, { fractionDigits: 0 })}
                  </span>
                  <span className="w-14 text-right text-gray-500 dark:text-gray-400 tabular-nums">
                    {item.percentage.toFixed(1)}%
                  </span>
                </li>
              ))}
            </ul>
          </>
        )}
      </div>
    </Card>
  )
}

export default function SpendingByCategoryPage() {
  const [period, setPeriod] = useState('12')
  const navigate = useNavigate()

  const { data: periods, isLoading: periodsLoading } = useChartPeriods()
  const { data: categories } = useCategories()
  const { data, isLoading, isError } = useSpendingByCategory(period)

  const periodOptions = useMemo(
    () => (periods ?? []).map(p => ({ label: p.label, value: p.code })),
    [periods],
  )

  // Drill a (parent-rolled-up) slice into the canonical Transactions surface,
  // filtered to that category subtree for the same period (ADR-0005). Falls back
  // to a description search if the name can't be resolved to a category id.
  const drillToCategory = useCallback(
    (name: string) => {
      const cat = categories?.find(c => c.name === name)
      const params = new URLSearchParams({ period })
      if (cat) params.set('categoryId', String(cat.id))
      else params.set('search', name)
      navigate(`/transactions?${params.toString()}`)
    },
    [categories, period, navigate],
  )

  if (isLoading || periodsLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner size="lg" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="p-8 text-red-500 dark:text-red-400">
        Failed to load spending data. Please try again.
      </div>
    )
  }

  const income = data?.income ?? []
  const expenses = data?.expenses ?? []

  return (
    <div className="space-y-6 p-6 lg:p-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">
          Spending by Category
        </h1>
        <div className="w-48">
          <Select
            label="Period"
            options={periodOptions}
            value={period}
            onChange={setPeriod}
          />
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <DonutChart title="Income by Category" data={income} onDrill={drillToCategory} />
        <DonutChart title="Expenses by Category" data={expenses} onDrill={drillToCategory} />
      </div>
    </div>
  )
}
