import { useMemo } from 'react'
import { useCumulativeSpending } from '@/hooks/useCharts'
import { useBudgets } from '@/hooks/useBudgets'
import { useTheme } from '@/components/layout/useTheme'
import { Spinner, Card, EChart } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { chartAxis } from '@/lib/chartTheme'
import type { EChartsOption } from 'echarts'

export default function CumulativeSpendingPage() {
  const { data, isLoading, error } = useCumulativeSpending()
  const { data: budgets } = useBudgets()
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  const totalBudget = useMemo(
    () => (budgets ?? []).reduce((sum, b) => sum + b.amount, 0),
    [budgets],
  )

  const option = useMemo<EChartsOption>(() => {
    const points = (data ?? []).filter(
      (d) => Number.isFinite(d.lastMonthExpenses) || Number.isFinite(d.thisMonthExpenses),
    )
    const axis = chartAxis(isDark)

    // Expected spend by day d if the whole monthly budget is spent evenly.
    const now = new Date()
    const daysInMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate()

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
        // Budget pace: a straight ramp from 0 to the total monthly budget. Only
        // shown when budgets are set. Above this line means spending faster than
        // an even pace toward the budget.
        ...(totalBudget > 0
          ? [
              {
                name: 'Budget pace',
                type: 'line' as const,
                smooth: false,
                showSymbol: false,
                lineStyle: { width: 2, color: '#F59E0B', type: 'dashed' as const },
                itemStyle: { color: '#F59E0B' },
                // Cap at the total: the line is flat once the month is fully spent,
                // so days beyond the current month's length don't overshoot.
                data: points.map((d) =>
                  Math.round(Math.min(totalBudget, (totalBudget * d.dayNumber) / daysInMonth)),
                ),
              },
            ]
          : []),
      ],
    }
  }, [data, isDark, totalBudget])

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
