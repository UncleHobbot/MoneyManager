import { useState, useEffect, useCallback } from 'react'
import { Pencil, Trash2, Plus, Save } from 'lucide-react'
import { useSettings, useUpdateSettings } from '@/hooks/useSystem'
import { useAiProviders, useUpdateAiProvider, useDeleteAiProvider } from '@/hooks/useAI'
import { Button, Input, Dialog, DialogFooter, DataTable, Spinner, Card, Badge } from '@/components/ui'
import { Select } from '@/components/ui'
import type { Column } from '@/components/ui'
import type { AiProvider, AiProviderRequest } from '@/types'

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

export default function SettingsPage() {
  const { data: settings, isLoading: settingsLoading } = useSettings()
  const updateSettings = useUpdateSettings()
  const { data: providers, isLoading: providersLoading } = useAiProviders()
  const updateProvider = useUpdateAiProvider()
  const deleteProvider = useDeleteAiProvider()

  const [isDarkMode, setIsDarkMode] = useState(false)
  const [backupPath, setBackupPath] = useState('')

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingId, setEditingId] = useState<number | undefined>()
  const [form, setForm] = useState<AiProviderRequest>({ ...emptyProvider })

  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [deletingProvider, setDeletingProvider] = useState<AiProvider | null>(null)

  useEffect(() => {
    if (settings) {
      setIsDarkMode(settings.isDarkMode)
      setBackupPath(settings.backupPath ?? '')
    }
  }, [settings])

  const handleSaveSettings = useCallback(() => {
    updateSettings.mutate({ isDarkMode, backupPath: backupPath || null })
  }, [updateSettings, isDarkMode, backupPath])

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

  const columns: Column<AiProvider>[] = [
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

  if (settingsLoading || providersLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="space-y-6 p-8">
      <h1 className="text-2xl font-semibold dark:text-white">Settings</h1>

      {/* App Settings */}
      <Card title="App Settings">
        <div className="space-y-4 max-w-md">
          <label className="flex items-center gap-3 cursor-pointer">
            <input
              type="checkbox"
              checked={isDarkMode}
              onChange={(e) => setIsDarkMode(e.target.checked)}
              className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-700"
            />
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Dark Mode</span>
          </label>

          <Input
            label="Backup Path"
            value={backupPath}
            onChange={setBackupPath}
            placeholder="e.g. C:\Backups\MoneyManager"
          />

          <Button
            icon={<Save size={16} />}
            loading={updateSettings.isPending}
            onClick={handleSaveSettings}
          >
            Save Settings
          </Button>
        </div>
      </Card>

      {/* AI Providers */}
      <Card title="AI Providers" subtitle="Configure AI providers for financial analysis">
        <div className="space-y-4">
          <div className="flex justify-end">
            <Button icon={<Plus size={16} />} onClick={openAddDialog}>
              Add Provider
            </Button>
          </div>

          <DataTable
            columns={columns}
            data={providers ?? []}
            emptyMessage="No AI providers configured"
          />
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

      {/* Delete Confirmation Dialog */}
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
    </div>
  )
}
