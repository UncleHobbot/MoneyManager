import { useNavigate } from 'react-router-dom'
import Chart from 'react-apexcharts'
import type { ApexOptions } from 'apexcharts'
import { useTransactions } from '@/hooks/useTransactions'
import { useNetIncome, useCumulativeSpending, useSpendingByCategory } from '@/hooks/useCharts'
import { useCategories } from '@/hooks/useCategories'
import { useCreateBackup } from '@/hooks/useSystem'
import { Card, Spinner, Button, Badge } from '@/components/ui'
import {
  Upload,
  HardDrive,
  AlertCircle,
  Clock,
  TrendingUp,
  BarChart3,
  PieChart,
  ChevronRight,
} from 'lucide-react'

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-CA', { style: 'currency', currency: 'CAD' }).format(value)
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-CA', { month: 'short', day: 'numeric' })
}

function CardHeader({
  icon,
  title,
  to,
}: {
  icon: React.ReactNode
  title: string
  to: string
}) {
  const navigate = useNavigate()
  return (
    <div className="flex items-center justify-between mb-3">
      <div className="flex items-center gap-2 text-gray-900 dark:text-gray-100">
        {icon}
        <h3 className="text-sm font-semibold">{title}</h3>
      </div>
      <button
        onClick={() => navigate(to)}
        className="flex items-center gap-0.5 text-xs text-blue-600 dark:text-blue-400 hover:underline"
      >
        View More <ChevronRight size={14} />
      </button>
    </div>
  )
}

function ImportCard() {
  const navigate = useNavigate()
  const backup = useCreateBackup()

  return (
    <Card className="h-full">
      <CardHeader icon={<Upload size={16} />} title="Import & Backup" to="/transactions" />
      <div className="flex flex-col gap-3">
        <Button
          variant="primary"
          size="sm"
          icon={<Upload size={14} />}
          onClick={() => navigate('/transactions')}
          className="w-full"
        >
          Import Transactions
        </Button>
        <Button
          variant="secondary"
          size="sm"
          icon={<HardDrive size={14} />}
          loading={backup.isPending}
          onClick={() => backup.mutate()}
          className="w-full"
        >
          Backup Database
        </Button>
        {backup.isSuccess && (
          <Badge variant="green">Backup created successfully</Badge>
        )}
        {backup.isError && (
          <Badge variant="red">Backup failed</Badge>
        )}
      </div>
    </Card>
  )
}

function UncategorizedCard() {
  const { data: categories, isLoading: isLoadingCategories } = useCategories()
  const uncategorizedCategory = categories?.find(
    category => category.name.toLowerCase() === 'uncategorized',
  )
  const { data, isLoading: isLoadingTransactions } = useTransactions(
    '12',
    undefined,
    uncategorizedCategory?.id ?? -1,
    1,
    5,
  )
  const isLoading = isLoadingCategories || isLoadingTransactions

  return (
    <Card className="h-full">
      <CardHeader icon={<AlertCircle size={16} />} title="Uncategorized" to="/transactions" />
      {isLoading ? (
        <div className="flex justify-center py-6"><Spinner size="sm" /></div>
      ) : !data?.items.length ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
          All transactions categorized!
        </p>
      ) : (
        <ul className="divide-y divide-gray-100 dark:divide-gray-700">
          {data.items.slice(0, 5).map(t => (
            <li key={t.id} className="py-2 first:pt-0 last:pb-0">
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs text-gray-400 dark:text-gray-500 shrink-0">
                  {formatDate(t.date)}
                </span>
                <span className="text-sm text-gray-800 dark:text-gray-200 truncate flex-1 text-right">
                  {t.description}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}

function RecentTransactionsCard() {
  const { data, isLoading } = useTransactions('w2', undefined, undefined, 1, 6)

  return (
    <Card className="h-full">
      <CardHeader icon={<Clock size={16} />} title="Recent Transactions" to="/transactions" />
      {isLoading ? (
        <div className="flex justify-center py-6"><Spinner size="sm" /></div>
      ) : !data?.items.length ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
          No recent transactions
        </p>
      ) : (
        <ul className="divide-y divide-gray-100 dark:divide-gray-700">
          {data.items.slice(0, 6).map(t => (
            <li key={t.id} className="py-2 first:pt-0 last:pb-0">
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs text-gray-400 dark:text-gray-500 shrink-0">
                  {formatDate(t.date)}
                </span>
                <span className="text-sm text-gray-800 dark:text-gray-200 truncate flex-1">
                  {t.description}
                </span>
                <span
                  className={`text-sm font-medium shrink-0 ${
                    t.amountExt >= 0
                      ? 'text-green-600 dark:text-green-400'
                      : 'text-red-600 dark:text-red-400'
                  }`}
                >
                  {formatCurrency(t.amountExt)}
                </span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </Card>
  )
}

function CumulativeSpendingCard() {
  const { data, isLoading } = useCumulativeSpending()

  const options: ApexOptions = {
    chart: { type: 'area', sparkline: { enabled: true }, toolbar: { show: false } },
    stroke: { curve: 'smooth', width: 2 },
    fill: { type: 'gradient', gradient: { opacityFrom: 0.4, opacityTo: 0.05 } },
    colors: ['#3b82f6', '#f97316'],
    tooltip: {
      theme: 'dark',
      y: { formatter: (v: number) => formatCurrency(v) },
    },
    xaxis: { labels: { show: false } },
    yaxis: { labels: { show: false } },
  }

  const series = data
    ? [
        { name: 'This Month', data: data.map(d => d.thisMonthExpenses) },
        { name: 'Last Month', data: data.map(d => d.lastMonthExpenses) },
      ]
    : []

  return (
    <Card className="h-full">
      <CardHeader
        icon={<TrendingUp size={16} />}
        title="Cumulative Spending"
        to="/charts/cumulative"
      />
      {isLoading ? (
        <div className="flex justify-center py-6"><Spinner size="sm" /></div>
      ) : (
        <Chart options={options} series={series} type="area" height={200} />
      )}
    </Card>
  )
}

function NetIncomeCard() {
  const { data, isLoading } = useNetIncome('12')

  const options: ApexOptions = {
    chart: { type: 'bar', sparkline: { enabled: true }, toolbar: { show: false } },
    plotOptions: { bar: { columnWidth: '60%' } },
    colors: ['#22c55e', '#ef4444'],
    tooltip: {
      theme: 'dark',
      y: { formatter: (v: number) => formatCurrency(v) },
    },
    xaxis: { labels: { show: false } },
    yaxis: { labels: { show: false } },
  }

  const series = data
    ? [
        { name: 'Income', data: data.map(d => d.income) },
        { name: 'Expenses', data: data.map(d => d.expenses) },
      ]
    : []

  return (
    <Card className="h-full">
      <CardHeader icon={<BarChart3 size={16} />} title="Net Income (12 mo)" to="/charts/income" />
      {isLoading ? (
        <div className="flex justify-center py-6"><Spinner size="sm" /></div>
      ) : (
        <Chart options={options} series={series} type="bar" height={200} />
      )}
    </Card>
  )
}

function SpendingByCategoryCard() {
  const { data, isLoading } = useSpendingByCategory('12')

  const labels = data?.expenses?.map(d => d.name) ?? []
  const series = data?.expenses?.map(d => d.amount) ?? []

  const options: ApexOptions = {
    chart: { type: 'donut', sparkline: { enabled: true } },
    labels,
    legend: { show: false },
    dataLabels: { enabled: false },
    tooltip: {
      theme: 'dark',
      y: { formatter: (v: number) => formatCurrency(v) },
    },
    plotOptions: {
      pie: { donut: { size: '65%' } },
    },
  }

  return (
    <Card className="h-full">
      <CardHeader
        icon={<PieChart size={16} />}
        title="Spending by Category"
        to="/charts/spending"
      />
      {isLoading ? (
        <div className="flex justify-center py-6"><Spinner size="sm" /></div>
      ) : !series.length ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">No data</p>
      ) : (
        <Chart options={options} series={series} type="donut" height={200} />
      )}
    </Card>
  )
}

export default function DashboardPage() {
  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">Dashboard</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
        <ImportCard />
        <UncategorizedCard />
        <RecentTransactionsCard />
        <CumulativeSpendingCard />
        <NetIncomeCard />
        <SpendingByCategoryCard />
      </div>
    </div>
  )
}
