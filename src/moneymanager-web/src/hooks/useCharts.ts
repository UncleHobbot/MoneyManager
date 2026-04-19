import { useQuery } from '@tanstack/react-query'
import api from '@/api/client'
import type {
  BalanceChart,
  CategoryChart,
  ChartPeriod,
  CumulativeSpendingChart,
  TransactionDto,
} from '@/types'

export function useNetIncome(period: string) {
  return useQuery<BalanceChart[]>({
    queryKey: ['charts', 'net-income', period],
    queryFn: () =>
      api.get('/charts/net-income', { params: { period } }).then(r => r.data),
  })
}

export function useCumulativeSpending() {
  return useQuery<CumulativeSpendingChart[]>({
    queryKey: ['charts', 'cumulative-spending'],
    queryFn: () => api.get('/charts/cumulative-spending').then(r => r.data),
  })
}

export interface SpendingByCategoryResponse {
  income: CategoryChart[]
  expenses: CategoryChart[]
}

export function useSpendingByCategory(period: string) {
  return useQuery<SpendingByCategoryResponse>({
    queryKey: ['charts', 'spending-by-category', period],
    queryFn: () =>
      api
        .get('/charts/spending-by-category', { params: { period } })
        .then(r => r.data),
  })
}

export function useMonthDetail(month: string) {
  return useQuery<TransactionDto[]>({
    queryKey: ['charts', 'month-detail', month],
    queryFn: () =>
      api.get('/charts/month-detail', { params: { month } }).then(r => r.data),
    enabled: !!month,
  })
}

export function useChartPeriods() {
  return useQuery<ChartPeriod[]>({
    queryKey: ['charts', 'periods'],
    queryFn: () => api.get('/charts/periods').then(r => r.data),
  })
}
