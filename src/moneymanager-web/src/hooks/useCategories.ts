import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type { Category, CategoryTree } from '@/types'

export function useCategories() {
  return useQuery<Category[]>({
    queryKey: ['categories'],
    queryFn: () => api.get('/categories').then(r => r.data),
  })
}

export function useCategoryTree() {
  return useQuery<CategoryTree[]>({
    queryKey: ['categories', 'tree'],
    queryFn: () => api.get('/categories/tree').then(r => r.data),
  })
}

export function useCategoryIcons() {
  return useQuery<string[]>({
    queryKey: ['categories', 'icons'],
    queryFn: () => api.get('/categories/icons').then(r => r.data),
  })
}

export function useUpdateCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (category: Category) =>
      category.id === 0
        ? api.post('/categories', category).then(r => r.data)
        : api.put(`/categories/${category.id}`, category).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['categories'] }),
  })
}

export function useDeleteCategory() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => api.delete(`/categories/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['categories'] }),
  })
}
