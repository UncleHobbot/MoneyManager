import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import { queryKeys } from '@/lib/queryKeys'
import type { Rule, TransactionDto } from '@/types'

export function useRules() {
  return useQuery<Rule[]>({
    queryKey: queryKeys.rules.all,
    queryFn: () => api.get('/rules').then(r => r.data),
  })
}

export function usePossibleRules(transactionId?: number, enabled = true) {
  return useQuery<Rule[]>({
    queryKey: queryKeys.rules.possible(transactionId as number),
    enabled: enabled && transactionId !== undefined,
    queryFn: () => api.get(`/transactions/${transactionId}/possible-rules`).then(r => r.data),
  })
}

export function useUpdateRule() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (rule: Rule) =>
      rule.id === 0
        ? api.post('/rules', rule).then(r => r.data)
        : api.put(`/rules/${rule.id}`, rule).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.rules.all }),
  })
}

export function useDeleteRule() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => api.delete(`/rules/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.rules.all }),
  })
}

export function useApplyAllRules() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.post('/rules/apply-all').then(r => r.data as number),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.transactions.all }),
  })
}

export function useApplyRuleToTransaction() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ transactionId, ruleId }: { transactionId: number; ruleId: number }) =>
      api.post(`/transactions/${transactionId}/apply-rule/${ruleId}`).then(r => r.data as TransactionDto),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.transactions.all }),
  })
}
