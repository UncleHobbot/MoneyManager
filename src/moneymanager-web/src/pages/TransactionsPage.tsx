import { useState, useMemo, useCallback, type ReactNode } from 'react'
import {
  ChevronLeft,
  ChevronRight,
  TrendingUp,
  TrendingDown,
  DollarSign,
  Hash,
  Search,
  Download,
  Pencil,
  Plus,
  X,
} from 'lucide-react'
import {
  useTransactions,
  useTransactionStats,
  useCreateTransaction,
  useUpdateTransaction,
  useExportTransactions,
  type SortDir,
  type TransactionFilters,
} from '@/hooks/useTransactions'
import { useAccounts } from '@/hooks/useAccounts'
import { useCategories } from '@/hooks/useCategories'
import { useChartPeriods } from '@/hooks/useCharts'
import { useDebouncedValue } from '@/hooks/useDebouncedValue'
import {
  Button,
  Input,
  Select,
  DataTable,
  Badge,
  Card,
  Spinner,
  CategoryIcon,
  Money,
} from '@/components/ui'
import { EditTransactionDialog } from '@/components/EditTransactionDialog'
import { AddTransactionDialog } from '@/components/AddTransactionDialog'
import { formatCAD } from '@/lib/format'
import type { Column } from '@/components/ui'
import type { CreateTransactionRequest, TransactionDto } from '@/types'

const PAGE_SIZE_OPTIONS = [25, 50, 100]

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

/** A cell value that filters the grid when clicked. */
function FilterCell({
  onClick,
  title,
  children,
}: {
  onClick: () => void
  title: string
  children: ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      className="inline-flex items-center gap-1.5 rounded text-left hover:text-blue-600 hover:underline focus:outline-none focus:ring-2 focus:ring-blue-500 dark:hover:text-blue-400"
    >
      {children}
    </button>
  )
}

/** A column header with an optional ✕ button to clear that column's filter. */
function ClearableHeader({
  label,
  active,
  onClear,
}: {
  label: string
  active: boolean
  onClear: () => void
}) {
  return (
    <span className="inline-flex items-center gap-1">
      {label}
      {active && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation()
            onClear()
          }}
          title={`Clear ${label} filter`}
          aria-label={`Clear ${label} filter`}
          className="rounded p-0.5 text-blue-600 hover:bg-blue-100 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:text-blue-400 dark:hover:bg-blue-900/40"
        >
          <X size={12} />
        </button>
      )}
    </span>
  )
}

export default function TransactionsPage() {
  const [period, setPeriod] = useState('12')
  const [accountFilter, setAccountFilter] = useState<number | undefined>()
  const [categoryFilter, setCategoryFilter] = useState<number | undefined>()
  const [searchInput, setSearchInput] = useState('')
  const [uncategorized, setUncategorized] = useState(false)
  const [sortBy, setSortBy] = useState('date')
  const [sortDir, setSortDir] = useState<SortDir>('desc')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(50)
  const [editRow, setEditRow] = useState<TransactionDto | null>(null)
  const [editDesc, setEditDesc] = useState('')
  const [editCatId, setEditCatId] = useState<number | undefined>()
  const [editError, setEditError] = useState<string | null>(null)
  const [addOpen, setAddOpen] = useState(false)
  const [addError, setAddError] = useState<string | null>(null)

  const search = useDebouncedValue(searchInput.trim(), 300)

  const filters: TransactionFilters = useMemo(
    () => ({
      period,
      accountId: accountFilter,
      categoryId: categoryFilter,
      search,
      uncategorized,
      sortBy,
      sortDir,
    }),
    [period, accountFilter, categoryFilter, search, uncategorized, sortBy, sortDir],
  )

  const { data: periods } = useChartPeriods()
  const { data: accounts } = useAccounts()
  const { data: categories } = useCategories()
  const { data: stats } = useTransactionStats(filters)
  const { data: txPage, isLoading, isFetching } = useTransactions(filters, page, pageSize)
  const updateTx = useUpdateTransaction()
  const createTx = useCreateTransaction()
  const exportTx = useExportTransactions(period)

  const totalPages = txPage ? Math.max(1, Math.ceil(txPage.totalCount / pageSize)) : 1

  const hasActiveFilters =
    accountFilter !== undefined ||
    categoryFilter !== undefined ||
    searchInput.trim() !== '' ||
    uncategorized

  // Any change that narrows or reorders results returns to the first page.
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
  const handleSearchChange = useCallback((v: string) => {
    setSearchInput(v)
    setPage(1)
  }, [])
  const handleUncategorizedChange = useCallback((checked: boolean) => {
    setUncategorized(checked)
    setPage(1)
  }, [])
  const handlePageSizeChange = useCallback((v: string) => {
    setPageSize(Number(v))
    setPage(1)
  }, [])

  const clearFilters = useCallback(() => {
    setAccountFilter(undefined)
    setCategoryFilter(undefined)
    setSearchInput('')
    setUncategorized(false)
    setPage(1)
  }, [])

  const handleSortChange = useCallback((key: string, dir: SortDir) => {
    setSortBy(key)
    setSortDir(dir)
    setPage(1)
  }, [])

  // Click-a-cell-to-filter handlers (kept in sync with the top filter bar).
  const filterByAccount = useCallback((id: number) => {
    setAccountFilter(id)
    setPage(1)
  }, [])
  const clearAccountFilter = useCallback(() => {
    setAccountFilter(undefined)
    setPage(1)
  }, [])
  const filterByCategory = useCallback((id: number) => {
    setCategoryFilter(id)
    setUncategorized(false)
    setPage(1)
  }, [])
  const filterUncategorized = useCallback(() => {
    setUncategorized(true)
    setCategoryFilter(undefined)
    setPage(1)
  }, [])
  const clearCategoryFilter = useCallback(() => {
    setCategoryFilter(undefined)
    setUncategorized(false)
    setPage(1)
  }, [])
  const filterByDescription = useCallback((text: string) => {
    setSearchInput(text)
    setPage(1)
  }, [])
  const clearSearchFilter = useCallback(() => {
    setSearchInput('')
    setPage(1)
  }, [])

  const openEdit = useCallback((row: TransactionDto) => {
    setEditRow(row)
    setEditDesc(row.description)
    setEditCatId(row.category?.id)
    setEditError(null)
  }, [])

  const closeEdit = useCallback(() => setEditRow(null), [])

  const saveEdit = useCallback(() => {
    if (!editRow) return
    setEditError(null)
    updateTx.mutate(
      {
        id: editRow.id,
        data: {
          description: editDesc,
          categoryId: editCatId,
        },
      },
      {
        onSuccess: () => closeEdit(),
        onError: () => setEditError('Failed to save changes. Please try again.'),
      },
    )
  }, [editRow, editDesc, editCatId, updateTx, closeEdit])

  const openAdd = useCallback(() => {
    setAddError(null)
    setAddOpen(true)
  }, [])

  const closeAdd = useCallback(() => setAddOpen(false), [])

  const createTransaction = useCallback(
    (data: CreateTransactionRequest) => {
      setAddError(null)
      createTx.mutate(data, {
        onSuccess: () => setAddOpen(false),
        onError: () => setAddError('Failed to add transaction. Please try again.'),
      })
    },
    [createTx],
  )

  const handleExport = useCallback(() => {
    exportTx.mutate(undefined, {
      onSuccess: (blob) => {
        const url = URL.createObjectURL(blob)
        const link = document.createElement('a')
        link.href = url
        link.download = `transactions-${period}.csv`
        link.click()
        URL.revokeObjectURL(url)
      },
    })
  }, [exportTx, period])

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
        header: (
          <ClearableHeader
            label="Account"
            active={accountFilter !== undefined}
            onClear={clearAccountFilter}
          />
        ),
        className: 'whitespace-nowrap',
        render: (row) => (
          <FilterCell
            onClick={() => filterByAccount(row.account.id)}
            title={`Filter by account: ${row.account.shownName}`}
          >
            {row.account.shownName}
          </FilterCell>
        ),
      },
      {
        key: 'amount',
        header: 'Amount',
        sortable: true,
        className: 'whitespace-nowrap text-right w-28',
        render: (row) => (
          <Money amount={row.amountExt} signed color />
        ),
      },
      {
        key: 'category',
        header: (
          <ClearableHeader
            label="Category"
            active={categoryFilter !== undefined || uncategorized}
            onClear={clearCategoryFilter}
          />
        ),
        className: 'whitespace-nowrap',
        render: (row) =>
          row.category ? (
            <FilterCell
              onClick={() => filterByCategory(row.category!.id)}
              title={`Filter by category: ${row.category.name}`}
            >
              <CategoryIcon icon={row.category.icon ?? row.category.pIcon ?? undefined} size={16} />
              {row.category.name}
            </FilterCell>
          ) : (
            <FilterCell onClick={filterUncategorized} title="Filter by uncategorized">
              <span className="italic text-gray-400">Uncategorized</span>
            </FilterCell>
          ),
      },
      {
        key: 'description',
        header: (
          <ClearableHeader
            label="Description"
            active={searchInput.trim() !== ''}
            onClear={clearSearchFilter}
          />
        ),
        sortable: true,
        render: (row) => (
          <FilterCell
            onClick={() => filterByDescription(row.description)}
            title={`Filter descriptions containing: ${row.description}`}
          >
            <span className="line-clamp-1">{row.description}</span>
          </FilterCell>
        ),
      },
      {
        key: 'isRuleApplied',
        header: 'Rule',
        className: 'w-20',
        render: (row) =>
          row.isRuleApplied ? <Badge variant="blue">Rule</Badge> : null,
      },
      {
        key: 'actions',
        header: '',
        className: 'w-20 text-right',
        render: (row) => (
          <Button
            variant="ghost"
            size="sm"
            icon={<Pencil size={14} />}
            aria-label={`Edit ${row.description}`}
            onClick={() => openEdit(row)}
          >
            Edit
          </Button>
        ),
      },
    ],
    [
      accountFilter,
      categoryFilter,
      uncategorized,
      searchInput,
      filterByAccount,
      clearAccountFilter,
      filterByCategory,
      filterUncategorized,
      clearCategoryFilter,
      filterByDescription,
      clearSearchFilter,
      openEdit,
    ],
  )

  const rows = txPage?.items ?? []

  return (
    <div className="flex flex-col gap-4 p-6">
      {/* Header & Filters */}
      <div className="flex flex-wrap items-end gap-3">
        <h1 className="text-2xl font-semibold dark:text-white mr-auto">Transactions</h1>
        <div className="relative">
          <Search
            size={16}
            className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
          />
          <Input
            id="transaction-search"
            type="search"
            placeholder="Search description..."
            value={searchInput}
            onChange={handleSearchChange}
            className="!pl-9 w-56"
          />
        </div>
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
        <Button
          size="sm"
          onClick={openAdd}
          icon={<Plus size={16} />}
        >
          Add
        </Button>
        <Button
          variant="secondary"
          size="sm"
          onClick={handleExport}
          loading={exportTx.isPending}
          icon={<Download size={16} />}
        >
          Export
        </Button>
      </div>

      {/* Secondary filter row */}
      <div className="flex flex-wrap items-center gap-3 text-sm">
        <label className="inline-flex cursor-pointer items-center gap-2 text-gray-700 dark:text-gray-300">
          <input
            type="checkbox"
            checked={uncategorized}
            onChange={(e) => handleUncategorizedChange(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700"
          />
          Uncategorized only
        </label>
        {isFetching && !isLoading && (
          <span className="inline-flex items-center gap-1.5 text-gray-400">
            <Spinner size="sm" /> Updating…
          </span>
        )}
        {hasActiveFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={clearFilters}
            icon={<X size={14} />}
            className="ml-auto"
          >
            Clear filters
          </Button>
        )}
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
          rowKey={(row) => row.id}
          sortKey={sortBy}
          sortDir={sortDir}
          onSortChange={handleSortChange}
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
              Income: {formatCAD(stats.income)}
            </span>
            <span className="inline-flex items-center gap-1.5 text-red-600 dark:text-red-400">
              <TrendingDown size={16} />
              Expenses: {formatCAD(stats.expenses)}
            </span>
            <span className="inline-flex items-center gap-1.5 text-gray-900 dark:text-gray-100 font-medium">
              <DollarSign size={16} />
              Net: {formatCAD(stats.net)}
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
        categories={categories ?? []}
        isSaving={updateTx.isPending}
        errorMessage={editError}
        formatDate={formatDate}
        onDescriptionChange={setEditDesc}
        onCategoryChange={setEditCatId}
        onClose={closeEdit}
        onSave={saveEdit}
      />

      {/* Add Dialog */}
      <AddTransactionDialog
        open={addOpen}
        accounts={accounts ?? []}
        categories={categories ?? []}
        isSaving={createTx.isPending}
        errorMessage={addError}
        onClose={closeAdd}
        onCreate={createTransaction}
      />
    </div>
  )
}
