import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type {
  AiProvider,
  AiProviderRequest,
  AnalysisRequest,
  AnalysisResult,
  AnalysisType,
} from '@/types'

export function useAiProviders() {
  return useQuery<AiProvider[]>({
    queryKey: ['ai', 'providers'],
    queryFn: () => api.get('/ai/providers').then(r => r.data),
  })
}

export function useUpdateAiProvider() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id?: number; data: AiProviderRequest }) =>
      id
        ? api.put(`/ai/providers/${id}`, data).then(r => r.data)
        : api.post('/ai/providers', data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['ai', 'providers'] }),
  })
}

export function useDeleteAiProvider() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => api.delete(`/ai/providers/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['ai', 'providers'] }),
  })
}

export function useRunAnalysis() {
  return useMutation<AnalysisResult, Error, AnalysisRequest>({
    mutationFn: (request) =>
      api.post('/ai/analyze', request).then(r => r.data),
  })
}

export function useAnalysisTypes() {
  return useQuery<AnalysisType[]>({
    queryKey: ['ai', 'analysis-types'],
    queryFn: () => api.get('/ai/analysis-types').then(r => r.data),
  })
}
