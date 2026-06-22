import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type { BudgetDto } from '@/types'

export function useBudgets() {
  return useQuery<BudgetDto[]>({
    queryKey: ['budgets'],
    queryFn: () => api.get('/budgets').then(r => r.data),
  })
}

export function useSetBudget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (body: { categoryId: number; amount: number }) =>
      api.put('/budgets', body).then(r => r.data as BudgetDto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['budgets'] })
      qc.invalidateQueries({ queryKey: ['charts', 'budget-vs-actual'] })
    },
  })
}

export function useDeleteBudget() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (categoryId: number) => api.delete(`/budgets/${categoryId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['budgets'] })
      qc.invalidateQueries({ queryKey: ['charts', 'budget-vs-actual'] })
    },
  })
}
