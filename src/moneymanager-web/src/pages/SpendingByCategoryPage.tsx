import { useState, useMemo } from 'react'
import Chart from 'react-apexcharts'
import { useSpendingByCategory, useChartPeriods } from '@/hooks/useCharts'
import { Select, Spinner, Card, CategoryIcon } from '@/components/ui'
import type { CategoryChart } from '@/types'
import type { ApexOptions } from 'apexcharts'

const PALETTE = [
  '#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6',
  '#EC4899', '#14B8A6', '#F97316', '#6366F1', '#84CC16',
  '#06B6D4', '#D946EF', '#F43F5E', '#22D3EE', '#A3E635',
  '#FB923C', '#818CF8', '#2DD4BF', '#FBBF24', '#C084FC',
]

function formatAmount(value: number): string {
  return value.toLocaleString('en-CA', {
    style: 'currency',
    currency: 'CAD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  })
}

function DonutChart({
  title,
  data,
  highlighted,
  onSliceClick,
}: {
  title: string
  data: CategoryChart[]
  highlighted: number | null
  onSliceClick: (index: number | null) => void
}) {
  const labels = data.map(d => d.name)
  const series = data.map(d => d.amount)
  const total = series.reduce((a, b) => a + b, 0)

  const options = useMemo<ApexOptions>(
    () => ({
      chart: {
        type: 'donut' as const,
        background: 'transparent',
        events: {
          dataPointSelection: (
            _e: unknown,
            _chart: unknown,
            config?: Record<string, unknown>,
          ) => {
            const idx = config?.dataPointIndex as number | undefined
            if (idx != null) onSliceClick(idx === highlighted ? null : idx)
          },
        },
      },
      labels,
      colors: PALETTE.slice(0, data.length),
      theme: { mode: 'dark' as const },
      legend: { show: false },
      dataLabels: { enabled: false },
      plotOptions: {
        pie: {
          donut: {
            size: '60%',
            labels: {
              show: true,
              total: {
                show: true,
                label: 'Total',
                formatter: () => formatAmount(total),
              },
            },
          },
        },
      },
      tooltip: {
        y: {
          formatter: (val: number) => formatAmount(val),
        },
      },
      states: {
        active: { filter: { type: 'darken', value: 0.6 } },
      },
      stroke: { show: false },
    }),
    [data, labels, total, highlighted, onSliceClick],
  )

  return (
    <Card title={title} className="flex-1 min-w-0">
      <div className="flex flex-col items-center">
        {data.length === 0 ? (
          <p className="py-12 text-sm text-gray-400">No data for this period</p>
        ) : (
          <>
            <Chart
              options={options}
              series={series}
              type="donut"
              width="100%"
              height={320}
            />
            <ul className="mt-4 w-full space-y-1.5 max-h-64 overflow-y-auto">
              {data.map((item, i) => (
                <li
                  key={item.name}
                  role="button"
                  tabIndex={0}
                  onClick={() => onSliceClick(i === highlighted ? null : i)}
                  onKeyDown={e => {
                    if (e.key === 'Enter' || e.key === ' ')
                      onSliceClick(i === highlighted ? null : i)
                  }}
                  className={`flex items-center gap-2 rounded-md px-2 py-1.5 text-sm cursor-pointer transition-colors ${
                    highlighted === i
                      ? 'bg-gray-100 dark:bg-gray-700'
                      : 'hover:bg-gray-50 dark:hover:bg-gray-700/50'
                  }`}
                >
                  <span
                    className="h-3 w-3 rounded-full shrink-0"
                    style={{ backgroundColor: PALETTE[i % PALETTE.length] }}
                  />
                  <CategoryIcon icon={item.icon ?? undefined} size={16} className="shrink-0 text-gray-500 dark:text-gray-400" />
                  <span className="flex-1 truncate text-gray-900 dark:text-gray-100">
                    {item.name}
                  </span>
                  <span className="font-medium tabular-nums text-gray-900 dark:text-gray-100">
                    {formatAmount(item.amount)}
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
  const [incomeHighlight, setIncomeHighlight] = useState<number | null>(null)
  const [expenseHighlight, setExpenseHighlight] = useState<number | null>(null)

  const { data: periods, isLoading: periodsLoading } = useChartPeriods()
  const { data, isLoading, isError } = useSpendingByCategory(period)

  const periodOptions = useMemo(
    () => (periods ?? []).map(p => ({ label: p.label, value: p.code })),
    [periods],
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
        <DonutChart
          title="Income by Category"
          data={income}
          highlighted={incomeHighlight}
          onSliceClick={setIncomeHighlight}
        />
        <DonutChart
          title="Expenses by Category"
          data={expenses}
          highlighted={expenseHighlight}
          onSliceClick={setExpenseHighlight}
        />
      </div>
    </div>
  )
}
