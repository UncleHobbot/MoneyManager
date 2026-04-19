import { useMemo } from 'react'
import { useParams } from 'react-router-dom'
import Chart from 'react-apexcharts'
import { useMonthDetail } from '@/hooks/useCharts'
import { Spinner, Card, DataTable, CategoryIcon } from '@/components/ui'
import type { Column } from '@/components/ui'
import type { TransactionDto } from '@/types'

/** Parse "yyMM" → formatted month string like "January 2025". */
function formatMonthTitle(yyMM: string): string {
  const yy = parseInt(yyMM.slice(0, 2), 10)
  const mm = parseInt(yyMM.slice(2, 4), 10)
  const date = new Date(2000 + yy, mm - 1)
  return date.toLocaleString('default', { month: 'long', year: 'numeric' })
}

interface CategoryGroup {
  name: string
  icon: string | null
  total: number
}

function groupByParentCategory(
  transactions: TransactionDto[],
  filterFn: (t: TransactionDto) => boolean,
): CategoryGroup[] {
  const map = new Map<string, CategoryGroup>()
  for (const t of transactions) {
    if (!filterFn(t)) continue
    const cat = t.category
    const parentName = cat?.parent?.name ?? cat?.name ?? 'Uncategorized'
    const parentIcon = cat?.parent?.icon ?? cat?.icon ?? null
    const existing = map.get(parentName)
    if (existing) {
      existing.total += Math.abs(t.amountExt)
    } else {
      map.set(parentName, { name: parentName, icon: parentIcon, total: Math.abs(t.amountExt) })
    }
  }
  return Array.from(map.values()).sort((a, b) => b.total - a.total)
}

const columns: Column<TransactionDto>[] = [
  {
    key: 'date',
    header: 'Date',
    sortable: true,
    render: (row) => new Date(row.date).toLocaleDateString(),
  },
  {
    key: 'account',
    header: 'Account',
    sortable: true,
    render: (row) => row.account?.shownName ?? row.account?.name ?? '',
  },
  {
    key: 'amountExt',
    header: 'Amount',
    sortable: true,
    className: 'text-right',
    render: (row) => (
      <span className={row.amountExt >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}>
        {row.amountExt >= 0 ? '+' : ''}
        {row.amountExt.toLocaleString('en-CA', { style: 'currency', currency: 'CAD' })}
      </span>
    ),
  },
  {
    key: 'category',
    header: 'Category',
    render: (row) =>
      row.category ? (
        <span className="inline-flex items-center gap-1.5">
          <CategoryIcon icon={row.category.pIcon ?? row.category.icon ?? undefined} size={16} />
          {row.category.name}
        </span>
      ) : (
        <span className="text-gray-400">—</span>
      ),
  },
  {
    key: 'description',
    header: 'Description',
    sortable: true,
  },
]

function DonutChart({ title, groups }: { title: string; groups: CategoryGroup[] }) {
  if (groups.length === 0) return null

  const options: ApexCharts.ApexOptions = {
    chart: { type: 'donut' },
    labels: groups.map((g) => g.name),
    legend: { position: 'bottom', fontSize: '13px' },
    tooltip: {
      y: {
        formatter: (val: number) =>
          val.toLocaleString('en-CA', { style: 'currency', currency: 'CAD' }),
      },
    },
    plotOptions: {
      pie: {
        donut: {
          labels: {
            show: true,
            total: {
              show: true,
              label: 'Total',
              formatter: (w) => {
                const sum = (w.globals.seriesTotals as number[]).reduce((a, b) => a + b, 0)
                return sum.toLocaleString('en-CA', { style: 'currency', currency: 'CAD' })
              },
            },
          },
        },
      },
    },
  }

  return (
    <Card title={title}>
      <Chart options={options} series={groups.map((g) => g.total)} type="donut" height={320} />
    </Card>
  )
}

export default function MonthDetailPage() {
  const { month = '' } = useParams<{ month: string }>()
  const { data: transactions, isLoading, error } = useMonthDetail(month)

  const title = useMemo(() => (month ? formatMonthTitle(month) : 'Monthly Detail'), [month])

  const { income, expenses, incomeGroups, expenseGroups } = useMemo(() => {
    if (!transactions) return { income: 0, expenses: 0, incomeGroups: [], expenseGroups: [] }
    let inc = 0
    let exp = 0
    for (const t of transactions) {
      if (t.amountExt >= 0) inc += t.amountExt
      else exp += Math.abs(t.amountExt)
    }
    return {
      income: inc,
      expenses: exp,
      incomeGroups: groupByParentCategory(transactions, (t) => t.amountExt >= 0),
      expenseGroups: groupByParentCategory(transactions, (t) => t.amountExt < 0),
    }
  }, [transactions])

  const net = income - expenses

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Spinner size="lg" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="p-8 text-red-600 dark:text-red-400">
        Failed to load month detail.
      </div>
    )
  }

  const fmt = (v: number) => v.toLocaleString('en-CA', { style: 'currency', currency: 'CAD' })

  return (
    <div className="space-y-6 p-6">
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">{title}</h1>

      {/* Summary stats */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Card>
          <p className="text-sm text-gray-500 dark:text-gray-400">Income</p>
          <p className="text-xl font-semibold text-green-600 dark:text-green-400">{fmt(income)}</p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500 dark:text-gray-400">Expenses</p>
          <p className="text-xl font-semibold text-red-600 dark:text-red-400">{fmt(expenses)}</p>
        </Card>
        <Card>
          <p className="text-sm text-gray-500 dark:text-gray-400">Net</p>
          <p className={`text-xl font-semibold ${net >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
            {fmt(net)}
          </p>
        </Card>
      </div>

      {/* Donut charts */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <DonutChart title="Income by Category" groups={incomeGroups} />
        <DonutChart title="Expenses by Category" groups={expenseGroups} />
      </div>

      {/* Transaction list */}
      <Card title="Transactions">
        <DataTable columns={columns} data={transactions ?? []} emptyMessage="No transactions for this month." />
      </Card>
    </div>
  )
}
