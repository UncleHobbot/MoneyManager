import { useQuery } from '@tanstack/react-query'
import api from '@/api/client'
import { queryKeys } from '@/lib/queryKeys'
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
    queryKey: queryKeys.charts.netIncome(period),
    queryFn: () =>
      api.get('/charts/net-income', { params: { period } }).then(r => r.data),
  })
}

export function useCumulativeSpending() {
  return useQuery<CumulativeSpendingChart[]>({
    queryKey: queryKeys.charts.cumulativeSpending(),
    queryFn: () => api.get('/charts/cumulative-spending').then(r => r.data),
  })
}

export function useSpendingTrend(period: string) {
  return useQuery<SpendingTrendChart>({
    queryKey: queryKeys.charts.spendingTrend(period),
    queryFn: () =>
      api.get('/charts/spending-trend', { params: { period } }).then(r => r.data),
  })
}

export function useTopMerchants(period: string, limit = 15) {
  return useQuery<MerchantSpend[]>({
    queryKey: queryKeys.charts.topMerchants(period, limit),
    queryFn: () =>
      api.get('/charts/top-merchants', { params: { period, limit } }).then(r => r.data),
  })
}

export function useCashFlow(period: string) {
  return useQuery<CashFlowChart>({
    queryKey: queryKeys.charts.cashFlow(period),
    queryFn: () =>
      api.get('/charts/cash-flow', { params: { period } }).then(r => r.data),
  })
}

export function useBudgetVsActual() {
  return useQuery<BudgetVsActual[]>({
    queryKey: queryKeys.charts.budgetVsActual(),
    queryFn: () => api.get('/charts/budget-vs-actual').then(r => r.data),
  })
}

export interface SpendingByCategoryResponse {
  income: CategoryChart[]
  expenses: CategoryChart[]
}

export function useSpendingByCategory(period: string) {
  return useQuery<SpendingByCategoryResponse>({
    queryKey: queryKeys.charts.spendingByCategory(period),
    queryFn: () =>
      api
        .get('/charts/spending-by-category', { params: { period } })
        .then(r => r.data),
  })
}

export function useChartPeriods() {
  return useQuery<ChartPeriod[]>({
    queryKey: queryKeys.charts.periods(),
    queryFn: () => api.get('/charts/periods').then(r => r.data),
  })
}
