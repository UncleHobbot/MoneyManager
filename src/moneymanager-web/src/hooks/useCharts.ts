import { useQuery } from '@tanstack/react-query'
import api from '@/api/client'
import type {
  BalanceChart,
  BudgetVsActual,
  CashFlowChart,
  CategoryChart,
  ChartPeriod,
  CumulativeSpendingChart,
  MerchantSpend,
  SpendingTrendChart,
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

export function useSpendingTrend(period: string) {
  return useQuery<SpendingTrendChart>({
    queryKey: ['charts', 'spending-trend', period],
    queryFn: () =>
      api.get('/charts/spending-trend', { params: { period } }).then(r => r.data),
  })
}

export function useTopMerchants(period: string, limit = 15) {
  return useQuery<MerchantSpend[]>({
    queryKey: ['charts', 'top-merchants', period, limit],
    queryFn: () =>
      api.get('/charts/top-merchants', { params: { period, limit } }).then(r => r.data),
  })
}

export function useCashFlow(period: string) {
  return useQuery<CashFlowChart>({
    queryKey: ['charts', 'cash-flow', period],
    queryFn: () =>
      api.get('/charts/cash-flow', { params: { period } }).then(r => r.data),
  })
}

export function useBudgetVsActual() {
  return useQuery<BudgetVsActual[]>({
    queryKey: ['charts', 'budget-vs-actual'],
    queryFn: () => api.get('/charts/budget-vs-actual').then(r => r.data),
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

export function useChartPeriods() {
  return useQuery<ChartPeriod[]>({
    queryKey: ['charts', 'periods'],
    queryFn: () => api.get('/charts/periods').then(r => r.data),
  })
}
