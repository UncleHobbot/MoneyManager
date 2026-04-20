import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import DashboardPage from '@/pages/DashboardPage'
import { useCategories } from '@/hooks/useCategories'
import { useTransactions } from '@/hooks/useTransactions'
import type { Account, Category, Transaction, TransactionDto } from '@/types'

vi.mock('react-apexcharts', () => ({ default: () => null }))
vi.mock('@/hooks/useCategories', () => ({ useCategories: vi.fn() }))
vi.mock('@/hooks/useTransactions', () => ({ useTransactions: vi.fn() }))
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

function renderPage() {
  return render(
    <MemoryRouter>
      <DashboardPage />
    </MemoryRouter>,
  )
}

describe('DashboardPage', () => {
  it('loads uncategorized transactions using the legacy 12-month Uncategorized category filter', () => {
    const mockedUseCategories = vi.mocked(useCategories)
    const mockedUseTransactions = vi.mocked(useTransactions)

    mockedUseCategories.mockReturnValue({
      data: [uncategorizedCategory],
      isLoading: false,
    } as never)

    mockedUseTransactions.mockImplementation((period, _accountId, categoryId, page, pageSize) => {
      if (period === '12' && categoryId === uncategorizedCategory.id && page === 1 && pageSize === 5) {
        return {
          data: { items: [transactionDto], totalCount: 1, page: 1, pageSize: 5 },
          isLoading: false,
        } as never
      }

      return {
        data: { items: [], totalCount: 0, page: 1, pageSize },
        isLoading: false,
      } as never
    })

    renderPage()

    expect(mockedUseTransactions).toHaveBeenCalledWith('12', undefined, uncategorizedCategory.id, 1, 5)
    expect(screen.getByText('Forgot to categorize me')).toBeInTheDocument()
  })
})
