import { useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useSpendingTrend, useChartPeriods } from '@/hooks/useCharts'
import { useTheme } from '@/components/layout/useTheme'
import { Select, Spinner, ChartCard, EChart } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { CHART_PALETTE, chartAxis } from '@/lib/chartTheme'
import type { EChartsOption } from 'echarts'

/** Neutral color for the synthetic "Other" series (categoryId === null). */
const OTHER_COLOR = '#9CA3AF'

export default function SpendingTrendPage() {
  const [period, setPeriod] = useState('12')
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
    const axis = chartAxis(isDark)
    return {
      grid: { left: 8, right: 16, top: 40, bottom: 8, containLabel: true },
      legend: { top: 0, type: 'scroll' },
      tooltip: {
        trigger: 'axis',
        valueFormatter: (v) => formatCAD(Number(v), { fractionDigits: 0 }),
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: months.map(m => m.label),
        axisLabel: { color: axis.label },
        axisLine: { lineStyle: { color: axis.line } },
      },
      yAxis: {
        type: 'value',
        axisLabel: {
          color: axis.label,
          formatter: (v: number) => formatCAD(v, { fractionDigits: 0 }),
        },
        splitLine: { lineStyle: { color: axis.split } },
      },
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
        const qs = new URLSearchParams({ from: month.from.slice(0, 10), to: month.to.slice(0, 10) })
        if (s && s.categoryId != null) qs.set('categoryId', String(s.categoryId))
        navigate(`/transactions?${qs.toString()}`)
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
