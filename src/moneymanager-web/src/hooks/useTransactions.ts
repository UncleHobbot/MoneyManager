import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type {
  PaginatedResult,
  TransactionDto,
  TransactionStats,
  UpdateTransactionRequest,
} from '@/types'

export function useTransactions(
  period: string,
  accountId?: number,
  categoryId?: number,
  page = 1,
  pageSize = 50,
) {
  return useQuery<PaginatedResult<TransactionDto>>({
    queryKey: ['transactions', period, accountId, categoryId, page, pageSize],
    queryFn: () =>
      api
        .get('/transactions', {
          params: { period, accountId, categoryId, page, pageSize },
        })
        .then(r => r.data),
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

export function useTransactionStats(period: string) {
  return useQuery<TransactionStats>({
    queryKey: ['transactions', 'stats', period],
    queryFn: () =>
      api.get('/transactions/stats', { params: { period } }).then(r => r.data),
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

export function useDeleteTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => api.delete(`/transactions/${id}`),
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
