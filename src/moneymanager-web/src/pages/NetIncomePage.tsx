import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useNetIncome, useChartPeriods } from '@/hooks/useCharts'
import { useTheme } from '@/components/layout/useTheme'
import { Select, Spinner, Card, ChartCard, EChart } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { CHART_COLORS, chartAxis } from '@/lib/chartTheme'
import type { BalanceChart } from '@/types'
import type { EChartsOption } from 'echarts'

export default function NetIncomePage() {
  const [period, setPeriod] = useState('12')
  const navigate = useNavigate()
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  const { data: periods, isLoading: periodsLoading } = useChartPeriods()
  const { data: chartData, isLoading: dataLoading } = useNetIncome(period)

  if (periodsLoading || dataLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Spinner size="lg" />
      </div>
    )
  }

  const data: BalanceChart[] = chartData ?? []

  const months = data.map(d => d.month)
  const incomeValues = data.map(d => d.income)
  // Expenses arrive as a signed sum (debits negative), so the expense bars plot
  // downward and net is income + expenses. (Pre-migration code used
  // income - expenses, which double-negated and showed net far above income.)
  const expenseValues = data.map(d => d.expenses)
  const netValues = data.map(d => d.income + d.expenses)

  const axis = chartAxis(isDark)

  const option: EChartsOption = {
    grid: { left: 8, right: 8, top: 40, bottom: 8, containLabel: true },
    legend: { top: 0 },
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      valueFormatter: (val) => formatCAD(Number(val), { fractionDigits: 0 }),
    },
    xAxis: {
      type: 'category',
      data: months,
      axisLabel: { color: axis.label },
      axisLine: { lineStyle: { color: axis.line } },
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        color: axis.label,
        formatter: (val: number) => formatCAD(val, { fractionDigits: 0 }),
      },
      splitLine: { lineStyle: { color: axis.split } },
    },
    series: [
      { name: 'Income', type: 'bar', data: incomeValues, itemStyle: { color: CHART_COLORS.income } },
      { name: 'Expenses', type: 'bar', data: expenseValues, itemStyle: { color: CHART_COLORS.expense } },
      { name: 'Net Income', type: 'line', data: netValues, lineStyle: { width: 3, color: CHART_COLORS.net }, itemStyle: { color: CHART_COLORS.net } },
    ],
  }

  const onEvents = {
    click: (params: { dataIndex: number }) => {
      const item = data[params.dataIndex]
      if (item) navigate(`/charts/month/${item.monthKey}`)
    },
  }

  const periodOptions = (periods ?? []).map(p => ({
    label: p.label,
    value: p.code,
  }))

  return (
    <div className="space-y-6 p-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">
          Net Income
        </h1>
        <Select
          label="Period"
          options={periodOptions}
          value={period}
          onChange={setPeriod}
        />
      </div>

      <ChartCard isEmpty={data.length === 0} height={400}>
        <EChart option={option} height={400} onEvents={onEvents} />
      </ChartCard>

      <Card title="Monthly Breakdown">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-200 dark:border-gray-700 text-left text-gray-500 dark:text-gray-400">
                <th className="pb-2 font-medium">Month</th>
                <th className="pb-2 font-medium text-right">Income</th>
                <th className="pb-2 font-medium text-right">Expenses</th>
                <th className="pb-2 font-medium text-right">Net</th>
              </tr>
            </thead>
            <tbody>
              {data.map(row => {
                const net = row.income + row.expenses
                return (
                  <tr
                    key={row.monthKey}
                    className="border-b border-gray-100 dark:border-gray-700/50 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/40 transition-colors"
                    onClick={() => navigate(`/charts/month/${row.monthKey}`)}
                  >
                    <td className="py-2 text-gray-900 dark:text-gray-100">
                      {row.month}
                    </td>
                    <td className="py-2 text-right text-green-600 dark:text-green-400">
                      {formatCAD(row.income, { fractionDigits: 0 })}
                    </td>
                    <td className="py-2 text-right text-red-600 dark:text-red-400">
                      {formatCAD(row.expenses, { fractionDigits: 0 })}
                    </td>
                    <td
                      className={`py-2 text-right font-medium ${
                        net >= 0
                          ? 'text-blue-600 dark:text-blue-400'
                          : 'text-red-600 dark:text-red-400'
                      }`}
                    >
                      {formatCAD(net, { fractionDigits: 0 })}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  )
}
