import { useMemo } from 'react'
import { usePersistedState } from '@/hooks/usePersistedState'
import { useNavigate } from 'react-router-dom'
import { useSpendingTrend, useChartPeriods } from '@/hooks/useCharts'
import { useTheme } from '@/components/layout/useTheme'
import { Select, Spinner, ChartCard, EChart } from '@/components/ui'
import { transactionsUrl } from '@/lib/transactionsUrl'
import { CHART_PALETTE } from '@/lib/chartTheme'
import { moneyGrid, categoryAxis, cadValueAxis, cadAxisTooltip } from '@/lib/chartOptions'
import type { EChartsOption } from 'echarts'

/** Neutral color for the synthetic "Other" series (categoryId === null). */
const OTHER_COLOR = '#9CA3AF'

export default function SpendingTrendPage() {
  const [period, setPeriod] = usePersistedState('chart-period:spending-trend', '12')
  const navigate = useNavigate()
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  const { data: periods, isLoading: periodsLoading } = useChartPeriods()
  const { data, isLoading } = useSpendingTrend(period)

  const periodOptions = useMemo(
    () => (periods ?? []).map(p => ({ label: p.label, value: p.code })),
    [periods],
  )

  const months = useMemo(() => data?.months ?? [], [data])
  const series = useMemo(() => data?.series ?? [], [data])

  const option = useMemo<EChartsOption>(() => {
    return {
      grid: moneyGrid({ top: 40 }),
      legend: { top: 0, type: 'scroll' },
      tooltip: cadAxisTooltip(),
      xAxis: categoryAxis(isDark, months.map(m => m.label), { boundaryGap: false }),
      yAxis: cadValueAxis(isDark),
      series: series.map((s, i) => ({
        name: s.name,
        type: 'line',
        stack: 'total',
        areaStyle: {},
        showSymbol: false,
        emphasis: { focus: 'series' },
        lineStyle: { width: 1 },
        itemStyle: {
          color: s.categoryId === null ? OTHER_COLOR : CHART_PALETTE[i % CHART_PALETTE.length],
        },
        data: s.data,
      })),
    }
  }, [months, series, isDark])

  // Drill a clicked segment into the Transactions surface for that month and
  // category subtree (ADR-0005). "Other" (categoryId null) drills by month only.
  const onEvents = useMemo(
    () => ({
      click: (params: { seriesName?: string; dataIndex: number }) => {
        const month = months[params.dataIndex]
        if (!month) return
        const s = series.find(x => x.name === params.seriesName)
        navigate(transactionsUrl({
          from: month.from.slice(0, 10),
          to: month.to.slice(0, 10),
          categoryId: s?.categoryId != null ? s.categoryId : undefined,
        }))
      },
    }),
    [months, series, navigate],
  )

  if (isLoading || periodsLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="space-y-6 p-6 lg:p-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">
          Spending Trend
        </h1>
        <div className="w-48">
          <Select label="Period" options={periodOptions} value={period} onChange={setPeriod} />
        </div>
      </div>

      <ChartCard isEmpty={series.length === 0} height={440}>
        <EChart option={option} height={440} onEvents={onEvents} />
      </ChartCard>
    </div>
  )
}
