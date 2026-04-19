import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import type { BackupInfo, SettingsModel } from '@/types'

export function useSettings() {
  return useQuery<SettingsModel>({
    queryKey: ['settings'],
    queryFn: () => api.get('/settings').then(r => r.data),
  })
}

export function useUpdateSettings() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (settings: SettingsModel) =>
      api.put('/settings', settings).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['settings'] }),
  })
}

export function useBackups() {
  return useQuery<BackupInfo[]>({
    queryKey: ['backups'],
    queryFn: () => api.get('/system/backups').then(r => r.data),
  })
}

export function useCreateBackup() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.post('/system/backup').then(r => r.data as BackupInfo),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['backups'] }),
  })
}

export function useRestoreBackup() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (fileName: string) =>
      api.post(`/system/backups/${encodeURIComponent(fileName)}/restore`).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['backups'] }),
  })
}

export function useCleanupBackups() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (keep: number = 10) =>
      api.delete(`/system/backups/cleanup`, { params: { keep } }).then(r => r.data as number),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['backups'] }),
  })
}
