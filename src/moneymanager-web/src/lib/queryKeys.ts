import type { TransactionFilters } from '@/hooks/useTransactions'

/**
 * Single source of truth for TanStack Query keys.
 *
 * Each domain exposes `all` (the root key used for broad invalidation) plus
 * builders for the specific queries. The arrays produced here are intentionally
 * identical to the keys that were previously inlined across hooks and pages, so
 * adopting this factory is behaviour-neutral (partial-match invalidation by the
 * root key keeps matching every sub-key).
 */
export const queryKeys = {
  transactions: {
    all: ['transactions'] as const,
    list: (filters: TransactionFilters, page: number, pageSize: number) =>
      [
        'transactions',
        filters.period,
        filters.accountId,
        filters.categoryId,
        filters.search,
        filters.uncategorized,
        filters.sortBy ?? 'date',
        filters.sortDir ?? 'desc',
        filters.from,
        filters.to,
        page,
        pageSize,
      ] as const,
    infinite: (
      period: string,
      accountId: number | undefined,
      categoryId: number | undefined,
      pageSize: number,
    ) => ['transactions', 'infinite', period, accountId, categoryId, pageSize] as const,
    stats: (filters: TransactionFilters) =>
      [
        'transactions',
        'stats',
        filters.period,
        filters.accountId,
        filters.categoryId,
        filters.search,
        filters.uncategorized,
        filters.from,
        filters.to,
      ] as const,
  },

  charts: {
    all: ['charts'] as const,
    netIncome: (period: string) => ['charts', 'net-income', period] as const,
    cumulativeSpending: () => ['charts', 'cumulative-spending'] as const,
    spendingTrend: (period: string) => ['charts', 'spending-trend', period] as const,
    topMerchants: (period: string, limit: number) => ['charts', 'top-merchants', period, limit] as const,
    cashFlow: (period: string) => ['charts', 'cash-flow', period] as const,
    budgetVsActual: () => ['charts', 'budget-vs-actual'] as const,
    spendingByCategory: (period: string) => ['charts', 'spending-by-category', period] as const,
    periods: () => ['charts', 'periods'] as const,
  },

  categories: {
    all: ['categories'] as const,
    tree: () => ['categories', 'tree'] as const,
    icons: () => ['categories', 'icons'] as const,
  },

  accounts: {
    all: ['accounts'] as const,
  },

  rules: {
    all: ['rules'] as const,
    possible: (transactionId: number) => ['rules', 'possible', transactionId] as const,
  },

  budgets: {
    all: ['budgets'] as const,
  },

  ai: {
    providers: () => ['ai', 'providers'] as const,
    analysisTypes: () => ['ai', 'analysis-types'] as const,
  },

  backups: ['backups'] as const,
  csvArchive: ['csv-archive'] as const,
} as const
