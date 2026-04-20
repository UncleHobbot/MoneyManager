import { type ReactNode, type UIEvent, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Chart from 'react-apexcharts'
import type { ApexOptions } from 'apexcharts'
import { useInfiniteTransactions, useUpdateTransaction } from '@/hooks/useTransactions'
import { useNetIncome, useCumulativeSpending, useSpendingByCategory } from '@/hooks/useCharts'
import { useCategories } from '@/hooks/useCategories'
import { useCreateBackup } from '@/hooks/useSystem'
import { Card, Spinner, Button, Badge, CategoryIcon, Dialog, DialogFooter, Input, Select } from '@/components/ui'
import type { TransactionDto } from '@/types'
import {
  Upload,
  HardDrive,
  AlertCircle,
  Clock,
  TrendingUp,
  BarChart3,
  PieChart,
  ChevronRight,
  Pencil,
} from 'lucide-react'

const DASHBOARD_LIST_PAGE_SIZE = 50

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-CA', { style: 'currency', currency: 'CAD' }).format(value)
}

function formatDate(dateStr: string, includeYear = false): string {
  return new Date(dateStr).toLocaleDateString('en-CA', {
    month: 'short',
    day: 'numeric',
    ...(includeYear ? { year: 'numeric' } : {}),
  })
}

function loadMoreOnScroll(
  event: UIEvent<HTMLDivElement>,
  hasNextPage: boolean,
  isFetchingNextPage: boolean,
  fetchNextPage: () => Promise<unknown>,
) {
  const target = event.currentTarget
  const isNearBottom = target.scrollHeight - target.scrollTop - target.clientHeight < 120

  if (isNearBottom && hasNextPage && !isFetchingNextPage) {
    void fetchNextPage()
  }
}

function CardHeader({
  icon,
  title,
  to,
}: {
  icon: ReactNode
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
  const updateTx = useUpdateTransaction()
  const [editRow, setEditRow] = useState<TransactionDto | null>(null)
  const [editDesc, setEditDesc] = useState('')
  const [editCatId, setEditCatId] = useState<number | undefined>()
  const uncategorizedCategory = categories?.find(
    category => category.name.toLowerCase() === 'uncategorized',
  )
  const categoryOptions = useMemo(
    () => (categories ?? []).map(category => ({ label: category.name, value: category.id })),
    [categories],
  )
  const {
    data,
    isLoading: isLoadingTransactions,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
  } = useInfiniteTransactions(
    '12',
    undefined,
    uncategorizedCategory?.id ?? -1,
    DASHBOARD_LIST_PAGE_SIZE,
    !isLoadingCategories && !!uncategorizedCategory,
  )
  const items = data?.pages.flatMap(page => page.items) ?? []
  const isLoading = isLoadingCategories || isLoadingTransactions

  function openEdit(row: TransactionDto) {
    setEditRow(row)
    setEditDesc(row.description)
    setEditCatId(row.category?.id)
  }

  function closeEdit() {
    setEditRow(null)
  }

  function saveEdit() {
    if (!editRow) {
      return
    }

    updateTx.mutate(
      {
        id: editRow.id,
        data: {
          description: editDesc,
          categoryId: editCatId,
        },
      },
      { onSuccess: () => closeEdit() },
    )
  }

  return (
    <Card className="h-[400px] flex flex-col">
      <CardHeader icon={<AlertCircle size={16} />} title="Uncategorized" to="/transactions" />
      {isLoading ? (
        <div className="flex flex-1 items-center justify-center py-6"><Spinner size="sm" /></div>
      ) : !items.length ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
          All transactions categorized!
        </p>
      ) : (
        <>
          <div
            role="region"
            aria-label="Uncategorized transactions list"
            tabIndex={0}
            onScroll={(event) =>
              loadMoreOnScroll(event, Boolean(hasNextPage), isFetchingNextPage, fetchNextPage)
            }
            className="min-h-0 flex-1 overflow-y-auto pr-1 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <ul className="divide-y divide-gray-100 dark:divide-gray-700">
              {items.map(t => (
                <li key={t.id} className="py-3 first:pt-0 last:pb-0">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-sm font-medium text-gray-800 dark:text-gray-200">
                        {t.description}
                      </div>
                      <div className="mt-1 flex flex-wrap items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
                        <span className="truncate">{t.account.shownName}</span>
                        <span>&middot;</span>
                        <span className="shrink-0">{formatDate(t.date, true)}</span>
                        <span>&middot;</span>
                        <span
                          className={`shrink-0 ${t.amountExt >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}
                        >
                          {formatCurrency(t.amountExt)}
                        </span>
                      </div>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      icon={<Pencil size={14} />}
                      className="shrink-0 px-2"
                      aria-label={`Edit ${t.description}, ${formatDate(t.date, true)}, ${formatCurrency(t.amountExt)}`}
                      onClick={() => openEdit(t)}
                    >
                      Edit
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
            {isFetchingNextPage && (
              <div className="py-3 text-center text-xs text-gray-500 dark:text-gray-400">
                Loading more transactions...
              </div>
            )}
          </div>

          <Dialog open={!!editRow} onClose={closeEdit} title="Edit Transaction">
            {editRow && (
              <>
                <div className="mb-3 text-xs text-gray-500 dark:text-gray-400">
                  {formatDate(editRow.date, true)} &middot; {editRow.account.shownName} &middot;{' '}
                  <span className={editRow.isDebit ? 'text-red-500' : 'text-green-500'}>
                    {formatCurrency(editRow.amountExt)}
                  </span>
                </div>

                <div className="flex flex-col gap-4">
                  <Input
                    label="Description"
                    value={editDesc}
                    onChange={setEditDesc}
                    autoFocus
                  />
                  <Select
                    label="Category"
                    options={categoryOptions}
                    value={editCatId ?? ''}
                    onChange={(value) => setEditCatId(value ? Number(value) : undefined)}
                    placeholder="Select category"
                  />
                </div>

                <DialogFooter>
                  <Button variant="secondary" onClick={closeEdit}>
                    Cancel
                  </Button>
                  <Button onClick={saveEdit} loading={updateTx.isPending}>
                    Save
                  </Button>
                </DialogFooter>
              </>
            )}
          </Dialog>
        </>
      )}
    </Card>
  )
}

function RecentTransactionsCard() {
  const { data, isLoading, hasNextPage, isFetchingNextPage, fetchNextPage } = useInfiniteTransactions(
    'w2',
    undefined,
    undefined,
    DASHBOARD_LIST_PAGE_SIZE,
  )
  const items = data?.pages.flatMap(page => page.items) ?? []

  return (
    <Card className="h-[400px] flex flex-col">
      <CardHeader icon={<Clock size={16} />} title="Recent Transactions" to="/transactions" />
      {isLoading ? (
        <div className="flex flex-1 items-center justify-center py-6"><Spinner size="sm" /></div>
      ) : !items.length ? (
        <p className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">
          No recent transactions
        </p>
      ) : (
        <div
          role="region"
          aria-label="Recent transactions list"
          tabIndex={0}
          onScroll={(event) =>
            loadMoreOnScroll(event, Boolean(hasNextPage), isFetchingNextPage, fetchNextPage)
          }
          className="min-h-0 flex-1 overflow-y-auto pr-1 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          <ul className="divide-y divide-gray-100 dark:divide-gray-700">
            {items.map(t => (
              <li key={t.id} className="py-3 first:pt-0 last:pb-0">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0 flex-1">
                    <div className="flex items-start justify-between gap-3">
                      <span className="truncate text-sm text-gray-800 dark:text-gray-200">
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
                    <div className="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs text-gray-500 dark:text-gray-400">
                      <span>{formatDate(t.date)}</span>
                      <span>&middot;</span>
                      <span className="truncate">{t.account.shownName}</span>
                      <span>&middot;</span>
                      {t.category ? (
                        <span className="inline-flex items-center gap-1">
                          <span aria-hidden="true">
                            <CategoryIcon
                              icon={t.category.icon ?? t.category.pIcon ?? undefined}
                              size={14}
                              className="shrink-0"
                            />
                          </span>
                          {t.category.name}
                        </span>
                      ) : (
                        <span className="italic">Uncategorized</span>
                      )}
                    </div>
                  </div>
                </div>
              </li>
            ))}
          </ul>
          {isFetchingNextPage && (
            <div className="py-3 text-center text-xs text-gray-500 dark:text-gray-400">
              Loading more transactions...
            </div>
          )}
        </div>
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
