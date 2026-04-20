import { useState, useMemo, useCallback } from 'react'
import {
  ChevronLeft,
  ChevronRight,
  TrendingUp,
  TrendingDown,
  DollarSign,
  Hash,
} from 'lucide-react'
import {
  useTransactions,
  useTransactionStats,
  useUpdateTransaction,
} from '@/hooks/useTransactions'
import { useAccounts } from '@/hooks/useAccounts'
import { useCategories } from '@/hooks/useCategories'
import { useChartPeriods } from '@/hooks/useCharts'
import {
  Button,
  Select,
  DataTable,
  Badge,
  Card,
  Spinner,
  CategoryIcon,
} from '@/components/ui'
import { EditTransactionDialog } from '@/components/EditTransactionDialog'
import type { Column } from '@/components/ui'
import type { TransactionDto } from '@/types'

const PAGE_SIZE_OPTIONS = [25, 50, 100]

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(Math.abs(value))
}

function formatSignedAmount(value: number): string {
  return `${value < 0 ? '-' : ''}${formatCurrency(value)}`
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

export default function TransactionsPage() {
  const [period, setPeriod] = useState('12')
  const [accountFilter, setAccountFilter] = useState<number | undefined>()
  const [categoryFilter, setCategoryFilter] = useState<number | undefined>()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(50)
  const [editRow, setEditRow] = useState<TransactionDto | null>(null)
  const [editDesc, setEditDesc] = useState('')
  const [editCatId, setEditCatId] = useState<number | undefined>()

  const { data: periods } = useChartPeriods()
  const { data: accounts } = useAccounts()
  const { data: categories } = useCategories()
  const { data: stats } = useTransactionStats(period)
  const { data: txPage, isLoading } = useTransactions(
    period,
    accountFilter,
    categoryFilter,
    page,
    pageSize,
  )
  const updateTx = useUpdateTransaction()

  const totalPages = txPage ? Math.max(1, Math.ceil(txPage.totalCount / pageSize)) : 1

  // Reset to page 1 when filters change
  const handlePeriodChange = useCallback((v: string) => {
    setPeriod(v)
    setPage(1)
  }, [])
  const handleAccountChange = useCallback((v: string) => {
    setAccountFilter(v ? Number(v) : undefined)
    setPage(1)
  }, [])
  const handleCategoryChange = useCallback((v: string) => {
    setCategoryFilter(v ? Number(v) : undefined)
    setPage(1)
  }, [])
  const handlePageSizeChange = useCallback((v: string) => {
    setPageSize(Number(v))
    setPage(1)
  }, [])

  const openEdit = useCallback((row: TransactionDto) => {
    setEditRow(row)
    setEditDesc(row.description)
    setEditCatId(row.category?.id)
  }, [])

  const closeEdit = useCallback(() => setEditRow(null), [])

  const saveEdit = useCallback(() => {
    if (!editRow) return
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
  }, [editRow, editDesc, editCatId, updateTx, closeEdit])

  const periodOptions = useMemo(
    () => (periods ?? []).map((p) => ({ label: p.label, value: p.code })),
    [periods],
  )

  const accountOptions = useMemo(
    () => [
      { label: 'All Accounts', value: '' },
      ...(accounts ?? []).map((a) => ({ label: a.shownName, value: a.id })),
    ],
    [accounts],
  )

  const categoryOptions = useMemo(
    () => [
      { label: 'All Categories', value: '' },
      ...(categories ?? []).map((c) => ({ label: c.name, value: c.id })),
    ],
    [categories],
  )

  const categorySelectOptions = useMemo(
    () => (categories ?? []).map((c) => ({ label: c.name, value: c.id })),
    [categories],
  )

  const columns: Column<TransactionDto>[] = useMemo(
    () => [
      {
        key: 'date',
        header: 'Date',
        sortable: true,
        className: 'whitespace-nowrap w-32',
        render: (row) => formatDate(row.date),
      },
      {
        key: 'account',
        header: 'Account',
        className: 'whitespace-nowrap',
        render: (row) => row.account.shownName,
      },
      {
        key: 'amountExt',
        header: 'Amount',
        sortable: true,
        className: 'whitespace-nowrap text-right w-28',
        render: (row) => (
          <span className={row.isDebit ? 'text-red-600 dark:text-red-400' : 'text-green-600 dark:text-green-400'}>
            {row.isDebit ? '-' : '+'}
            {formatCurrency(row.amount)}
          </span>
        ),
      },
      {
        key: 'category',
        header: 'Category',
        className: 'whitespace-nowrap',
        render: (row) =>
          row.category ? (
            <span className="inline-flex items-center gap-1.5">
              <CategoryIcon icon={row.category.icon ?? row.category.pIcon ?? undefined} size={16} />
              {row.category.name}
            </span>
          ) : (
            <span className="text-gray-400 italic">Uncategorized</span>
          ),
      },
      {
        key: 'description',
        header: 'Description',
        sortable: true,
        render: (row) => (
          <span className="line-clamp-1">{row.description}</span>
        ),
      },
      {
        key: 'isRuleApplied',
        header: 'Rule',
        className: 'w-20',
        render: (row) =>
          row.isRuleApplied ? <Badge variant="blue">Rule</Badge> : null,
      },
    ],
    [],
  )

  const rows = txPage?.items ?? []

  return (
    <div className="flex flex-col gap-4 p-6">
      {/* Header & Filters */}
      <div className="flex flex-wrap items-end gap-3">
        <h1 className="text-2xl font-semibold dark:text-white mr-auto">Transactions</h1>
        <Select
          options={periodOptions}
          value={period}
          onChange={handlePeriodChange}
        />
        <Select
          options={accountOptions}
          value={accountFilter ?? ''}
          onChange={handleAccountChange}
        />
        <Select
          options={categoryOptions}
          value={categoryFilter ?? ''}
          onChange={handleCategoryChange}
        />
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" />
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={rows}
          onRowClick={openEdit}
          emptyMessage="No transactions found for the selected filters."
        />
      )}

      {/* Pagination */}
      <div className="flex flex-wrap items-center justify-between gap-3 text-sm text-gray-600 dark:text-gray-400">
        <div className="flex items-center gap-2">
          <span>Rows per page:</span>
          <Select
            options={PAGE_SIZE_OPTIONS.map((s) => ({ label: String(s), value: s }))}
            value={pageSize}
            onChange={handlePageSizeChange}
            className="!py-1"
          />
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            icon={<ChevronLeft size={16} />}
          >
            Prev
          </Button>
          <span>
            Page {page} of {totalPages}
          </span>
          <Button
            variant="ghost"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            icon={<ChevronRight size={16} />}
          >
            Next
          </Button>
        </div>

        <span>{txPage?.totalCount ?? 0} total transactions</span>
      </div>

      {/* Stats footer */}
      {stats && (
        <Card className="mt-2">
          <div className="flex flex-wrap items-center gap-6 text-sm">
            <span className="inline-flex items-center gap-1.5 text-green-600 dark:text-green-400">
              <TrendingUp size={16} />
              Income: {formatCurrency(stats.income)}
            </span>
            <span className="inline-flex items-center gap-1.5 text-red-600 dark:text-red-400">
              <TrendingDown size={16} />
              Expenses: {formatCurrency(stats.expenses)}
            </span>
            <span className="inline-flex items-center gap-1.5 text-gray-900 dark:text-gray-100 font-medium">
              <DollarSign size={16} />
              Net: {formatCurrency(stats.net)}
            </span>
            <span className="inline-flex items-center gap-1.5 text-gray-500 dark:text-gray-400">
              <Hash size={16} />
              Count: {stats.count.toLocaleString()}
            </span>
          </div>
        </Card>
      )}

      {/* Edit Dialog */}
      <EditTransactionDialog
        open={!!editRow}
        transaction={editRow}
        description={editDesc}
        categoryId={editCatId}
        categoryOptions={categorySelectOptions}
        isSaving={updateTx.isPending}
        formatDate={formatDate}
        formatAmount={formatSignedAmount}
        onDescriptionChange={setEditDesc}
        onCategoryChange={setEditCatId}
        onClose={closeEdit}
        onSave={saveEdit}
      />
    </div>
  )
}
