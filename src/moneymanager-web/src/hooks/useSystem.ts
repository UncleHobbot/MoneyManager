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
    queryFn: () => api.get('/backups').then(r => r.data),
  })
}

export function useCreateBackup() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.post('/backups').then(r => r.data as BackupInfo),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['backups'] }),
  })
}

export function useRestoreBackup() {
  return useMutation({
    mutationFn: (fileName: string) =>
      api.post('/backups/restore', { fileName }).then(r => r.data),
  })
}

export function useCleanupBackups() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.delete('/backups/cleanup').then(r => r.data as number),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['backups'] }),
  })
}
