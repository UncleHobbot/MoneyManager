import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { usePersistedState } from '@/hooks/usePersistedState'
import { useNetIncome, useChartPeriods } from '@/hooks/useCharts'
import { useTheme } from '@/components/layout/useTheme'
import { Select, Spinner, Card, ChartCard, EChart } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { transactionsUrl, monthRange } from '@/lib/transactionsUrl'
import { CHART_COLORS } from '@/lib/chartTheme'
import { moneyGrid, categoryAxis, cadValueAxis, cadAxisTooltip } from '@/lib/chartOptions'
import type { BalanceChart } from '@/types'
import type { EChartsOption } from 'echarts'

export default function NetIncomePage() {
  const [period, setPeriod] = usePersistedState('chart-period:net-income', '12')
  const navigate = useNavigate()
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  const { data: periods, isLoading: periodsLoading } = useChartPeriods()
  const { data: chartData, isLoading: dataLoading } = useNetIncome(period)

  const data: BalanceChart[] = useMemo(() => chartData ?? [], [chartData])

  const option = useMemo<EChartsOption>(() => {
    const months = data.map(d => d.month)
    const incomeValues = data.map(d => d.income)
    // Expenses arrive as a signed sum (debits negative), so the expense bars plot
    // downward and net is income + expenses. (Pre-migration code used
    // income - expenses, which double-negated and showed net far above income.)
    const expenseValues = data.map(d => d.expenses)
    const netValues = data.map(d => d.income + d.expenses)
    // Trailing 3-month average of net, to read the trend through monthly noise.
    const rollingNet = netValues.map((_, i) => {
      const window = netValues.slice(Math.max(0, i - 2), i + 1)
      return Math.round(window.reduce((a, b) => a + b, 0) / window.length)
    })
    return {
      grid: moneyGrid({ right: 8, top: 40 }),
      legend: { top: 0 },
      tooltip: cadAxisTooltip({ shadow: true }),
      xAxis: categoryAxis(isDark, months),
      yAxis: cadValueAxis(isDark),
      series: [
        { name: 'Income', type: 'bar', data: incomeValues, itemStyle: { color: CHART_COLORS.income }, barGap: '-100%' },
        { name: 'Expenses', type: 'bar', data: expenseValues, itemStyle: { color: CHART_COLORS.expense } },
        { name: 'Net Income', type: 'line', data: netValues, lineStyle: { width: 3, color: CHART_COLORS.net }, itemStyle: { color: CHART_COLORS.net } },
        { name: 'Net (3-mo avg)', type: 'line', data: rollingNet, symbol: 'none', smooth: true, lineStyle: { width: 2, color: '#F59E0B', type: 'dashed' }, itemStyle: { color: '#F59E0B' } },
      ],
    }
  }, [data, isDark])

  const onEvents = useMemo(
    () => ({
      click: (params: { dataIndex: number }) => {
        const item = data[params.dataIndex]
        if (item) navigate(transactionsUrl(monthRange(item.firstDate)))
      },
    }),
    [data, navigate],
  )

  const periodOptions = useMemo(
    () => (periods ?? []).map(p => ({ label: p.label, value: p.code })),
    [periods],
  )

  if (periodsLoading || dataLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Spinner size="lg" />
      </div>
    )
  }

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
                <th className="pb-2 font-medium text-right">Savings rate</th>
              </tr>
            </thead>
            <tbody>
              {data.map(row => {
                const net = row.income + row.expenses
                const savingsRate = row.income > 0 ? Math.round((net / row.income) * 100) : null
                return (
                  <tr
                    key={row.monthKey}
                    className="border-b border-gray-100 dark:border-gray-700/50 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/40 transition-colors"
                    onClick={() => navigate(transactionsUrl(monthRange(row.firstDate)))}
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
                    <td
                      className={`py-2 text-right tabular-nums ${
                        savingsRate == null
                          ? 'text-gray-400'
                          : savingsRate >= 0
                            ? 'text-gray-700 dark:text-gray-300'
                            : 'text-red-600 dark:text-red-400'
                      }`}
                    >
                      {savingsRate == null ? '—' : `${savingsRate}%`}
                    </td>
                  </tr>
                )
              })}
            </tbody>
            {data.length > 0 && (() => {
              const totalIncome = data.reduce((sum, r) => sum + r.income, 0)
              const totalExpenses = data.reduce((sum, r) => sum + r.expenses, 0)
              const totalNet = totalIncome + totalExpenses
              const totalSavingsRate = totalIncome > 0 ? Math.round((totalNet / totalIncome) * 100) : null
              return (
                <tfoot>
                  <tr className="border-t-2 border-gray-300 dark:border-gray-600 font-semibold">
                    <td className="py-2 text-gray-900 dark:text-gray-100">Total</td>
                    <td className="py-2 text-right text-green-600 dark:text-green-400">
                      {formatCAD(totalIncome, { fractionDigits: 0 })}
                    </td>
                    <td className="py-2 text-right text-red-600 dark:text-red-400">
                      {formatCAD(totalExpenses, { fractionDigits: 0 })}
                    </td>
                    <td className={`py-2 text-right ${totalNet >= 0 ? 'text-blue-600 dark:text-blue-400' : 'text-red-600 dark:text-red-400'}`}>
                      {formatCAD(totalNet, { fractionDigits: 0 })}
                    </td>
                    <td className={`py-2 text-right tabular-nums ${totalSavingsRate == null ? 'text-gray-400' : totalSavingsRate >= 0 ? 'text-gray-700 dark:text-gray-300' : 'text-red-600 dark:text-red-400'}`}>
                      {totalSavingsRate == null ? '—' : `${totalSavingsRate}%`}
                    </td>
                  </tr>
                </tfoot>
              )
            })()}
          </table>
        </div>
      </Card>
    </div>
  )
}
