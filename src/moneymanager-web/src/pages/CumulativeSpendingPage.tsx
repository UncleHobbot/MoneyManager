import { useMemo } from 'react'
import { useCumulativeSpending } from '@/hooks/useCharts'
import { useTheme } from '@/components/layout/useTheme'
import { Spinner, Card, EChart } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { chartAxis } from '@/lib/chartTheme'
import type { EChartsOption } from 'echarts'

export default function CumulativeSpendingPage() {
  const { data, isLoading, error } = useCumulativeSpending()
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  const option = useMemo<EChartsOption>(() => {
    const points = (data ?? []).filter(
      (d) => Number.isFinite(d.lastMonthExpenses) || Number.isFinite(d.thisMonthExpenses),
    )
    const axis = chartAxis(isDark)

    return {
      grid: { left: 8, right: 16, top: 36, bottom: 36, containLabel: true },
      legend: { top: 0 },
      tooltip: {
        trigger: 'axis',
        valueFormatter: (val) =>
          val == null ? '—' : formatCAD(Number(val), { fractionDigits: 0 }),
      },
      xAxis: {
        type: 'category',
        data: points.map((d) => d.dayNumber),
        name: 'Day of Month',
        nameLocation: 'middle',
        nameGap: 28,
        nameTextStyle: { color: axis.label },
        axisLabel: { color: axis.label },
        axisLine: { lineStyle: { color: axis.line } },
      },
      yAxis: {
        type: 'value',
        name: 'Cumulative $ Spent',
        nameTextStyle: { color: axis.label },
        axisLabel: {
          color: axis.label,
          formatter: (val: number) => formatCAD(val, { fractionDigits: 0 }),
        },
        splitLine: { lineStyle: { color: axis.split } },
      },
      series: [
        {
          name: 'Last Month',
          type: 'line',
          smooth: true,
          showSymbol: false,
          lineStyle: { width: 2, color: '#9CA3AF' },
          itemStyle: { color: '#9CA3AF' },
          areaStyle: { opacity: 0.12 },
          data: points.map((d) =>
            Number.isFinite(d.lastMonthExpenses) ? d.lastMonthExpenses : null,
          ),
        },
        {
          name: 'This Month',
          type: 'line',
          smooth: true,
          showSymbol: false,
          lineStyle: { width: 2, color: '#34D399' },
          itemStyle: { color: '#34D399' },
          areaStyle: { opacity: 0.2 },
          data: points.map((d) =>
            Number.isFinite(d.thisMonthExpenses) ? d.thisMonthExpenses : null,
          ),
        },
      ],
    }
  }, [data, isDark])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Spinner size="lg" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="p-8 text-red-500 dark:text-red-400">
        Failed to load cumulative spending data.
      </div>
    )
  }

  return (
    <div className="space-y-6 p-8">
      <h1 className="text-2xl font-semibold dark:text-white">
        Cumulative Spending: This Month vs Last Month
      </h1>
      <Card>
        <EChart option={option} height={420} />
      </Card>
    </div>
  )
}
