import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Chart from 'react-apexcharts'
import { useNetIncome, useChartPeriods } from '@/hooks/useCharts'
import { useTheme } from '@/components/layout/ThemeProvider'
import { Select, Spinner, Card } from '@/components/ui'
import type { BalanceChart } from '@/types'
import type { ApexOptions } from 'apexcharts'

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-CA', {
    style: 'currency',
    currency: 'CAD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value)
}

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
  const expenseValues = data.map(d => d.expenses)
  const netValues = data.map(d => d.income - d.expenses)

  const options: ApexOptions = {
    chart: {
      type: 'bar',
      background: 'transparent',
      toolbar: { show: false },
      events: {
        dataPointSelection: (_e, _chart, config) => {
          const idx = config?.dataPointIndex as number
          const item = data[idx]
          if (item) navigate(`/charts/month/${item.monthKey}`)
        },
      },
    },
    theme: { mode: isDark ? 'dark' : 'light' },
    plotOptions: {
      bar: { columnWidth: '60%' },
    },
    stroke: {
      width: [0, 0, 3],
    },
    colors: ['#22c55e', '#ef4444', '#3b82f6'],
    xaxis: {
      categories: months,
      labels: {
        style: { colors: isDark ? '#d1d5db' : '#374151' },
      },
    },
    yaxis: {
      labels: {
        formatter: (val: number) => formatCurrency(val),
        style: { colors: isDark ? '#d1d5db' : '#374151' },
      },
    },
    tooltip: {
      theme: isDark ? 'dark' : 'light',
      y: { formatter: (val: number) => formatCurrency(val) },
    },
    legend: {
      labels: { colors: isDark ? '#d1d5db' : '#374151' },
    },
    dataLabels: { enabled: false },
    grid: {
      borderColor: isDark ? '#374151' : '#e5e7eb',
    },
  }

  const series = [
    { name: 'Income', type: 'bar' as const, data: incomeValues },
    { name: 'Expenses', type: 'bar' as const, data: expenseValues },
    { name: 'Net Income', type: 'line' as const, data: netValues },
  ]

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

      <Card>
        <Chart
          options={options}
          series={series}
          type="line"
          height={400}
        />
      </Card>

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
                const net = row.income - row.expenses
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
                      {formatCurrency(row.income)}
                    </td>
                    <td className="py-2 text-right text-red-600 dark:text-red-400">
                      {formatCurrency(row.expenses)}
                    </td>
                    <td
                      className={`py-2 text-right font-medium ${
                        net >= 0
                          ? 'text-blue-600 dark:text-blue-400'
                          : 'text-red-600 dark:text-red-400'
                      }`}
                    >
                      {formatCurrency(net)}
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
