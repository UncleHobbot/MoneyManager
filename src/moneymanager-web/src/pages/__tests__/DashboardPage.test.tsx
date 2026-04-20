import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import DashboardPage from '@/pages/DashboardPage'
import { useCategories } from '@/hooks/useCategories'
import { useInfiniteTransactions, useUpdateTransaction } from '@/hooks/useTransactions'
import type { Account, Category, Transaction, TransactionDto } from '@/types'

vi.mock('react-apexcharts', () => ({ default: () => null }))
vi.mock('@/hooks/useCategories', () => ({ useCategories: vi.fn() }))
vi.mock('@/hooks/useTransactions', () => ({
  useInfiniteTransactions: vi.fn(),
  useUpdateTransaction: vi.fn(),
}))
vi.mock('@/hooks/useCharts', () => ({
  useNetIncome: () => ({ data: [], isLoading: false }),
  useCumulativeSpending: () => ({ data: [], isLoading: false }),
  useSpendingByCategory: () => ({ data: { expenses: [] }, isLoading: false }),
}))
vi.mock('@/hooks/useSystem', () => ({
  useCreateBackup: () => ({
    mutate: vi.fn(),
    isPending: false,
    isSuccess: false,
    isError: false,
  }),
}))

const uncategorizedCategory: Category = {
  id: 42,
  parent: null,
  name: 'Uncategorized',
  icon: 'Misc',
  isNew: false,
  pIcon: null,
}

const account: Account = {
  id: 7,
  name: 'Chequing',
  shownName: 'Main Chequing',
  description: null,
  type: 0,
  number: null,
  isHideFromGraph: false,
  alternativeName1: null,
  alternativeName2: null,
  alternativeName3: null,
  alternativeName4: null,
  alternativeName5: null,
  typeIconName: 'Cash',
}

const transaction: Transaction = {
  id: 11,
  account,
  date: '2026-03-15T00:00:00Z',
  description: 'Forgot to categorize me',
  originalDescription: 'Forgot to categorize me',
  amount: 12.34,
  amountExt: -12.34,
  isDebit: true,
  category: uncategorizedCategory,
  isRuleApplied: false,
}

const transactionDto: TransactionDto = {
  ...transaction,
  transaction,
}

const recentTransaction: TransactionDto = {
  ...transactionDto,
  id: 12,
  description: 'Coffee shop',
  amount: 5.5,
  amountExt: -5.5,
  category: {
    id: 9,
    parent: null,
    name: 'Food',
    icon: 'Food',
    isNew: false,
    pIcon: null,
  },
}

function renderPage() {
  return render(
    <MemoryRouter>
      <DashboardPage />
    </MemoryRouter>,
  )
}

describe('DashboardPage', () => {
  it('shows all uncategorized transactions in a scrollable card and opens edit dialog', async () => {
    const mockedUseCategories = vi.mocked(useCategories)
    const mockedUseInfiniteTransactions = vi.mocked(useInfiniteTransactions)
    const mockedUseUpdateTransaction = vi.mocked(useUpdateTransaction)
    const user = userEvent.setup()
    const mutate = vi.fn()

    mockedUseCategories.mockReturnValue({
      data: [uncategorizedCategory],
      isLoading: false,
    } as never)
    mockedUseUpdateTransaction.mockReturnValue({
      mutate,
      isPending: false,
    } as never)

    mockedUseInfiniteTransactions.mockImplementation((period, _accountId, categoryId, pageSize, enabled) => {
      if (period === '12' && categoryId === uncategorizedCategory.id && pageSize === 50 && enabled === true) {
        return {
          data: { pages: [{ items: [transactionDto], totalCount: 1, page: 1, pageSize: 50 }] },
          isLoading: false,
          hasNextPage: false,
          isFetchingNextPage: false,
          fetchNextPage: vi.fn(),
        } as never
      }

      if (period === 'w2' && categoryId === undefined && pageSize === 50) {
        return {
          data: { pages: [{ items: [recentTransaction], totalCount: 1, page: 1, pageSize: 50 }] },
          isLoading: false,
          hasNextPage: false,
          isFetchingNextPage: false,
          fetchNextPage: vi.fn(),
        } as never
      }

      return {
        data: { pages: [{ items: [], totalCount: 0, page: 1, pageSize }] },
        isLoading: false,
        hasNextPage: false,
        isFetchingNextPage: false,
        fetchNextPage: vi.fn(),
      } as never
    })

    renderPage()

    expect(mockedUseInfiniteTransactions).toHaveBeenCalledWith(
      '12',
      undefined,
      uncategorizedCategory.id,
      50,
      true,
    )
    expect(screen.getByRole('button', { name: /view more uncategorized/i })).toBeInTheDocument()
    expect(screen.getByLabelText('Uncategorized transactions list')).toHaveClass('overflow-y-auto')
    const uncategorizedRegion = screen.getByRole('region', { name: /uncategorized transactions list/i })
    expect(uncategorizedRegion).toHaveAttribute('tabindex', '0')
    expect(screen.getByText('Forgot to categorize me')).toBeInTheDocument()
    expect(within(uncategorizedRegion).getByText('Main Chequing')).toBeInTheDocument()
    expect(within(uncategorizedRegion).getByText(/^Mar \d{1,2}$/)).toBeInTheDocument()
    expect(within(uncategorizedRegion).queryByText(/2026/)).not.toBeInTheDocument()
    const editButton = screen.getByRole('button', {
      name: /edit forgot to categorize me, .*2026, -?\$12\.34/i,
    })
    expect(editButton).toBeInTheDocument()
    expect(within(uncategorizedRegion).getByText('-$12.34')).toBeInTheDocument()

    await user.click(editButton)

    expect(screen.getByRole('heading', { name: /edit transaction/i })).toBeInTheDocument()
    const dateInput = screen.getByLabelText('Date') as HTMLInputElement
    expect(dateInput.value).toMatch(/2026/)
    expect(dateInput).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Account')).toHaveValue('Main Chequing')
    expect(screen.getByLabelText('Account')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Amount')).toHaveValue('-$12.34')
    expect(screen.getByLabelText('Amount')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Description')).toHaveValue('Forgot to categorize me')
    expect(screen.getByLabelText('Original Description')).toHaveValue('Forgot to categorize me')
    expect(screen.getByLabelText('Original Description')).toHaveAttribute('readonly')
    expect(screen.getByLabelText('Rule was applied')).not.toBeChecked()
    expect(screen.getByLabelText('Rule was applied')).toBeDisabled()
  })

  it('shows all recent transactions in a scrollable card with account and category metadata', () => {
    const mockedUseCategories = vi.mocked(useCategories)
    const mockedUseInfiniteTransactions = vi.mocked(useInfiniteTransactions)
    const mockedUseUpdateTransaction = vi.mocked(useUpdateTransaction)

    mockedUseCategories.mockReturnValue({
      data: [uncategorizedCategory, recentTransaction.category!],
      isLoading: false,
    } as never)
    mockedUseUpdateTransaction.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as never)

    mockedUseInfiniteTransactions.mockImplementation((period, _accountId, _categoryId, pageSize) => {
      if (period === '12') {
        return {
          data: { pages: [{ items: [transactionDto], totalCount: 1, page: 1, pageSize: 50 }] },
          isLoading: false,
          hasNextPage: false,
          isFetchingNextPage: false,
          fetchNextPage: vi.fn(),
        } as never
      }

      if (period === 'w2' && _categoryId === undefined && pageSize === 50) {
        return {
          data: { pages: [{ items: [recentTransaction], totalCount: 1, page: 1, pageSize: 50 }] },
          isLoading: false,
          hasNextPage: false,
          isFetchingNextPage: false,
          fetchNextPage: vi.fn(),
        } as never
      }

      return {
        data: { pages: [{ items: [], totalCount: 0, page: 1, pageSize }] },
        isLoading: false,
        hasNextPage: false,
        isFetchingNextPage: false,
        fetchNextPage: vi.fn(),
      } as never
    })

    renderPage()

    expect(mockedUseInfiniteTransactions).toHaveBeenCalledWith('w2', undefined, undefined, 50)
    expect(screen.getByRole('button', { name: /view more recent transactions/i })).toBeInTheDocument()
    const recentRegion = screen.getByRole('region', { name: /recent transactions list/i })
    expect(recentRegion).toHaveClass('overflow-y-auto')
    expect(recentRegion).toHaveAttribute('tabindex', '0')
    expect(screen.getByText('Coffee shop')).toBeInTheDocument()
    expect(within(recentRegion).getByText('Main Chequing')).toBeInTheDocument()
    expect(within(recentRegion).getByText(/^Mar \d{1,2}$/)).toBeInTheDocument()
    expect(within(recentRegion).getByText('Food')).toBeInTheDocument()
    expect(within(recentRegion).getByText('-$5.50')).toBeInTheDocument()
  })

  it('announces incremental loading inside dashboard transaction lists', () => {
    const mockedUseCategories = vi.mocked(useCategories)
    const mockedUseInfiniteTransactions = vi.mocked(useInfiniteTransactions)
    const mockedUseUpdateTransaction = vi.mocked(useUpdateTransaction)

    mockedUseCategories.mockReturnValue({
      data: [uncategorizedCategory, recentTransaction.category!],
      isLoading: false,
    } as never)
    mockedUseUpdateTransaction.mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as never)

    mockedUseInfiniteTransactions.mockImplementation((period, _accountId, categoryId, pageSize, enabled) => {
      if (period === '12' && categoryId === uncategorizedCategory.id && pageSize === 50 && enabled === true) {
        return {
          data: { pages: [{ items: [transactionDto], totalCount: 1, page: 1, pageSize: 50 }] },
          isLoading: false,
          hasNextPage: true,
          isFetchingNextPage: true,
          fetchNextPage: vi.fn(),
        } as never
      }

      if (period === 'w2' && categoryId === undefined && pageSize === 50) {
        return {
          data: { pages: [{ items: [recentTransaction], totalCount: 1, page: 1, pageSize: 50 }] },
          isLoading: false,
          hasNextPage: true,
          isFetchingNextPage: true,
          fetchNextPage: vi.fn(),
        } as never
      }

      return {
        data: { pages: [{ items: [], totalCount: 0, page: 1, pageSize }] },
        isLoading: false,
        hasNextPage: false,
        isFetchingNextPage: false,
        fetchNextPage: vi.fn(),
      } as never
    })

    renderPage()

    expect(screen.getAllByRole('status', { name: '' })).toHaveLength(2)
    expect(screen.getAllByText('Loading more transactions...')).toHaveLength(2)
  })
})
