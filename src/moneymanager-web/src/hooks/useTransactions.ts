import {
  keepPreviousData,
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import api from '@/api/client'
import type {
  CreateTransactionRequest,
  PaginatedResult,
  TransactionDto,
  TransactionStats,
  UpdateTransactionRequest,
} from '@/types'

export type SortDir = 'asc' | 'desc'

export interface TransactionFilters {
  period: string
  accountId?: number
  categoryId?: number
  search?: string
  uncategorized?: boolean
  sortBy?: string
  sortDir?: SortDir
}

export function useTransactions(filters: TransactionFilters, page = 1, pageSize = 50) {
  const {
    period,
    accountId,
    categoryId,
    search,
    uncategorized,
    sortBy = 'date',
    sortDir = 'desc',
  } = filters
  return useQuery<PaginatedResult<TransactionDto>>({
    queryKey: [
      'transactions',
      period,
      accountId,
      categoryId,
      search,
      uncategorized,
      sortBy,
      sortDir,
      page,
      pageSize,
    ],
    queryFn: () =>
      api
        .get('/transactions', {
          params: {
            period,
            accountId,
            categoryId,
            search: search || undefined,
            uncategorized: uncategorized || undefined,
            sortBy,
            sortDir,
            page,
            pageSize,
          },
        })
        .then(r => r.data),
    placeholderData: keepPreviousData,
  })
}

export function useInfiniteTransactions(
  period: string,
  accountId?: number,
  categoryId?: number,
  pageSize = 50,
  enabled = true,
) {
  return useInfiniteQuery<PaginatedResult<TransactionDto>>({
    queryKey: ['transactions', 'infinite', period, accountId, categoryId, pageSize],
    initialPageParam: 1,
    enabled,
    queryFn: ({ pageParam }) =>
      api
        .get('/transactions', {
          params: { period, accountId, categoryId, page: pageParam, pageSize },
        })
        .then(r => r.data),
    getNextPageParam: (lastPage) => {
      const loadedCount = lastPage.page * lastPage.pageSize
      return loadedCount < lastPage.totalCount ? lastPage.page + 1 : undefined
    },
  })
}

export function useTransactionStats(filters: TransactionFilters) {
  const { period, accountId, categoryId, search, uncategorized } = filters
  return useQuery<TransactionStats>({
    queryKey: ['transactions', 'stats', period, accountId, categoryId, search, uncategorized],
    queryFn: () =>
      api
        .get('/transactions/stats', {
          params: {
            period,
            accountId,
            categoryId,
            search: search || undefined,
            uncategorized: uncategorized || undefined,
          },
        })
        .then(r => r.data),
    placeholderData: keepPreviousData,
  })
}

export function useCreateTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateTransactionRequest) =>
      api.post('/transactions', data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transactions'] }),
  })
}

export function useUpdateTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateTransactionRequest }) =>
      api.put(`/transactions/${id}`, data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['transactions'] }),
  })
}

export function useExportTransactions(period: string) {
  return useMutation({
    mutationFn: () =>
      api
        .get('/transactions/export', {
          params: { period },
          responseType: 'blob',
        })
        .then(r => r.data as Blob),
  })
}
