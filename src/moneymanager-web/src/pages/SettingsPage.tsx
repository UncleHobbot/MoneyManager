import { useState, useCallback } from 'react'
import { Pencil, Trash2, Plus, Download, RefreshCw } from 'lucide-react'
import { useAiProviders, useUpdateAiProvider, useDeleteAiProvider } from '@/hooks/useAI'
import { useBackups, useCreateBackup, useRestoreBackup, useCleanupBackups } from '@/hooks/useSystem'
import { Button, Input, Dialog, DialogFooter, DataTable, Spinner, Card, Badge } from '@/components/ui'
import { Select } from '@/components/ui'
import type { Column } from '@/components/ui'
import type { AiProvider, AiProviderRequest, BackupInfo } from '@/types'

const PROVIDER_TYPE_OPTIONS = [
  { label: 'OpenAI', value: 'OpenAI' },
  { label: 'ZAI', value: 'ZAI' },
  { label: 'Custom', value: 'Custom' },
]

const emptyProvider: AiProviderRequest = {
  name: '',
  providerType: 'OpenAI',
  apiKey: '',
  apiUrl: '',
  model: '',
  isDefault: false,
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function SettingsPage() {
  const { data: providers, isLoading: providersLoading } = useAiProviders()
  const updateProvider = useUpdateAiProvider()
  const deleteProvider = useDeleteAiProvider()

  const { data: backups, isLoading: backupsLoading, error: backupsError } = useBackups()
  const createBackup = useCreateBackup()
  const restoreBackup = useRestoreBackup()
  const cleanupBackups = useCleanupBackups()

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | undefined>()
  const [form, setForm] = useState<AiProviderRequest>({ ...emptyProvider })

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [deletingProvider, setDeletingProvider] = useState<AiProvider | null>(null)

  const [restoreTarget, setRestoreTarget] = useState<string | null>(null)
  const [cleanupResult, setCleanupResult] = useState<number | null>(null)

  const openAddDialog = useCallback(() => {
    setEditingId(undefined)
    setForm({ ...emptyProvider })
    setDialogOpen(true)
  }, [])

  const openEditDialog = useCallback((provider: AiProvider) => {
    setEditingId(provider.id)
    setForm({
      name: provider.name,
      providerType: provider.providerType,
      apiKey: '',
      apiUrl: provider.apiUrl,
      model: provider.model,
      isDefault: provider.isDefault,
    })
    setDialogOpen(true)
  }, [])

  const openDeleteDialog = useCallback((provider: AiProvider) => {
    setDeletingProvider(provider)
    setDeleteDialogOpen(true)
  }, [])

  const handleSaveProvider = useCallback(() => {
    updateProvider.mutate(
      { id: editingId, data: form },
      { onSuccess: () => setDialogOpen(false) },
    )
  }, [updateProvider, editingId, form])

  const handleConfirmDelete = useCallback(() => {
    if (!deletingProvider) return
    deleteProvider.mutate(deletingProvider.id, {
      onSuccess: () => {
        setDeleteDialogOpen(false)
        setDeletingProvider(null)
      },
    })
  }, [deleteProvider, deletingProvider])

  const handleRestore = useCallback(() => {
    if (!restoreTarget) return
    restoreBackup.mutate(restoreTarget, {
      onSuccess: () => setRestoreTarget(null),
    })
  }, [restoreBackup, restoreTarget])

  const handleCleanup = useCallback(() => {
    cleanupBackups.mutate(10, {
      onSuccess: (count) => {
        setCleanupResult(count)
        setTimeout(() => setCleanupResult(null), 4000)
      },
    })
  }, [cleanupBackups])

  const providerColumns: Column<AiProvider>[] = [
    { key: 'name', header: 'Name', sortable: true },
    { key: 'providerType', header: 'Type', sortable: true },
    { key: 'model', header: 'Model', sortable: true },
    { key: 'apiUrl', header: 'API URL' },
    {
      key: 'isDefault',
      header: 'Default',
      render: (row) => row.isDefault ? <Badge variant="green">Default</Badge> : null,
    },
    {
      key: 'actions',
      header: 'Actions',
      render: (row) => (
        <div className="flex items-center gap-1">
          <Button variant="ghost" size="sm" icon={<Pencil size={14} />} onClick={() => openEditDialog(row)} />
          <Button variant="ghost" size="sm" icon={<Trash2 size={14} />} onClick={() => openDeleteDialog(row)} />
        </div>
      ),
    },
  ]

  const backupColumns: Column<BackupInfo>[] = [
    { key: 'fileName', header: 'Filename', sortable: true },
    {
      key: 'createdAt',
      header: 'Created',
      sortable: true,
      render: (row) => formatDate(row.createdAt),
    },
    {
      key: 'sizeBytes',
      header: 'Size',
      sortable: true,
      render: (row) => formatSize(row.sizeBytes),
    },
    {
      key: '_actions',
      header: 'Actions',
      render: (row) => (
        <Button
          variant="ghost"
          size="sm"
          icon={<Download size={14} />}
          onClick={(e) => { e.stopPropagation(); setRestoreTarget(row.fileName) }}
        >
          Restore
        </Button>
      ),
    },
  ]

  if (providersLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="space-y-6 p-8">
      <h1 className="text-2xl font-semibold dark:text-white">Settings</h1>

      {/* AI Providers */}
      <Card title="AI Providers" subtitle="Configure AI providers for financial analysis">
        <div className="space-y-4">
          <div className="flex justify-end">
            <Button icon={<Plus size={16} />} onClick={openAddDialog}>
              Add Provider
            </Button>
          </div>

          <DataTable
            columns={providerColumns}
            data={providers ?? []}
            emptyMessage="No AI providers configured"
          />
        </div>
      </Card>

      {/* Backups */}
      <Card title="Backups" subtitle="Create, restore, and manage database backups">
        <div className="space-y-4">
          <div className="flex flex-wrap items-center gap-3">
            <Button
              icon={<Plus size={16} />}
              loading={createBackup.isPending}
              onClick={() => createBackup.mutate()}
            >
              Create Backup
            </Button>
            <Button
              variant="secondary"
              icon={<Trash2 size={16} />}
              loading={cleanupBackups.isPending}
              onClick={handleCleanup}
            >
              Cleanup Old Backups
            </Button>
            {cleanupResult !== null && (
              <span className="text-sm text-green-600 dark:text-green-400">
                {cleanupResult} backup{cleanupResult !== 1 ? 's' : ''} deleted
              </span>
            )}
          </div>

          {backupsError && (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-800 dark:bg-red-900/20 dark:text-red-400">
              Failed to load backups: {backupsError.message}
            </div>
          )}

          {backupsLoading ? (
            <div className="flex items-center justify-center py-12">
              <Spinner />
            </div>
          ) : (
            <DataTable
              columns={backupColumns}
              data={backups ?? []}
              emptyMessage="No backups found. Create one to get started."
            />
          )}
        </div>
      </Card>

      {/* Add / Edit Provider Dialog */}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} title={editingId ? 'Edit Provider' : 'Add Provider'}>
        <div className="space-y-4">
          <Input
            label="Name"
            value={form.name}
            onChange={(v) => setForm((f) => ({ ...f, name: v }))}
            placeholder="My OpenAI Provider"
          />

          <Select
            label="Provider Type"
            options={PROVIDER_TYPE_OPTIONS}
            value={form.providerType}
            onChange={(v) => setForm((f) => ({ ...f, providerType: v }))}
          />

          <Input
            label="API Key"
            type="password"
            value={form.apiKey}
            onChange={(v) => setForm((f) => ({ ...f, apiKey: v }))}
            placeholder={editingId ? '(leave blank to keep current)' : 'sk-...'}
          />

          <Input
            label="API URL"
            value={form.apiUrl}
            onChange={(v) => setForm((f) => ({ ...f, apiUrl: v }))}
            placeholder="https://api.openai.com/v1"
          />

          <Input
            label="Model"
            value={form.model}
            onChange={(v) => setForm((f) => ({ ...f, model: v }))}
            placeholder="gpt-4o"
          />

          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={form.isDefault}
              onChange={(e) => setForm((f) => ({ ...f, isDefault: e.target.checked }))}
              className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700"
            />
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Set as Default</span>
          </label>
        </div>

        <DialogFooter>
          <Button variant="secondary" onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button loading={updateProvider.isPending} onClick={handleSaveProvider}>
            {editingId ? 'Update' : 'Create'}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Delete Provider Confirmation Dialog */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)} title="Delete Provider">
        <p className="text-sm text-gray-600 dark:text-gray-300">
          Are you sure you want to delete <span className="font-semibold">{deletingProvider?.name}</span>? This action cannot be undone.
        </p>
        <DialogFooter>
          <Button variant="secondary" onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button variant="danger" loading={deleteProvider.isPending} onClick={handleConfirmDelete}>
            Delete
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Restore Backup Confirmation Dialog */}
      <Dialog open={restoreTarget !== null} onClose={() => setRestoreTarget(null)} title="Confirm Restore">
        <p className="text-sm text-gray-600 dark:text-gray-300">
          This will overwrite current data. Are you sure you want to restore from{' '}
          <strong className="text-gray-900 dark:text-gray-100">{restoreTarget}</strong>?
        </p>
        <DialogFooter>
          <Button variant="secondary" onClick={() => setRestoreTarget(null)}>Cancel</Button>
          <Button variant="danger" icon={<RefreshCw size={16} />} loading={restoreBackup.isPending} onClick={handleRestore}>
            Restore
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  )
}
