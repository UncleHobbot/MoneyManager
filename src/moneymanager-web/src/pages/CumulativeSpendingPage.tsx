import { useCumulativeSpending } from '@/hooks/useCharts'
import { Spinner, Card } from '@/components/ui'
import Chart from 'react-apexcharts'
import type { ApexOptions } from 'apexcharts'

export default function CumulativeSpendingPage() {
  const { data, isLoading, error } = useCumulativeSpending()

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

  const points = (data ?? []).filter(
    (d) => Number.isFinite(d.lastMonthExpenses) || Number.isFinite(d.thisMonthExpenses),
  )

  const categories = points.map((d) => d.dayNumber)

  const lastMonthSeries = points.map((d) =>
    Number.isFinite(d.lastMonthExpenses) ? d.lastMonthExpenses : null,
  )
  const thisMonthSeries = points.map((d) =>
    Number.isFinite(d.thisMonthExpenses) ? d.thisMonthExpenses : null,
  )

  const series: ApexOptions['series'] = [
    { name: 'Last Month', data: lastMonthSeries },
    { name: 'This Month', data: thisMonthSeries },
  ]

  const options: ApexOptions = {
    chart: {
      type: 'area',
      background: 'transparent',
      toolbar: { show: false },
    },
    theme: { mode: 'dark' },
    colors: ['#9CA3AF', '#34D399'],
    fill: {
      type: 'gradient',
      gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.05, stops: [0, 100] },
    },
    stroke: { curve: 'smooth', width: 2 },
    dataLabels: { enabled: false },
    xaxis: {
      categories,
      title: { text: 'Day of Month', style: { color: '#9CA3AF' } },
      labels: { style: { colors: '#9CA3AF' } },
    },
    yaxis: {
      title: { text: 'Cumulative $ Spent', style: { color: '#9CA3AF' } },
      labels: {
        style: { colors: '#9CA3AF' },
        formatter: (val: number) => `$${val.toLocaleString()}`,
      },
    },
    tooltip: {
      theme: 'dark',
      y: { formatter: (val: number) => `$${val.toLocaleString()}` },
    },
    grid: { borderColor: '#374151' },
    legend: { labels: { colors: '#D1D5DB' } },
  }

  return (
    <div className="space-y-6 p-8">
      <h1 className="text-2xl font-semibold dark:text-white">
        Cumulative Spending: This Month vs Last Month
      </h1>
      <Card>
        <Chart options={options} series={series} type="area" height={420} />
      </Card>
    </div>
  )
}
