import { useState } from 'react'
import { useBackups, useCreateBackup, useRestoreBackup, useCleanupBackups } from '@/hooks/useSystem'
import { Button, Dialog, DialogFooter, DataTable, Spinner, Card } from '@/components/ui'
import type { Column } from '@/components/ui'
import type { BackupInfo } from '@/types'
import { Download, Trash2, Plus, RefreshCw } from 'lucide-react'

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function formatDate(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function BackupPage() {
  const { data: backups, isLoading, error } = useBackups()
  const createBackup = useCreateBackup()
  const restoreBackup = useRestoreBackup()
  const cleanupBackups = useCleanupBackups()

  const [restoreTarget, setRestoreTarget] = useState<string | null>(null)
  const [cleanupResult, setCleanupResult] = useState<number | null>(null)

  const handleCreate = () => {
    createBackup.mutate()
  }

  const handleRestore = () => {
    if (!restoreTarget) return
    restoreBackup.mutate(restoreTarget, {
      onSuccess: () => setRestoreTarget(null),
    })
  }

  const handleCleanup = () => {
    cleanupBackups.mutate(10, {
      onSuccess: (count) => {
        setCleanupResult(count)
        setTimeout(() => setCleanupResult(null), 4000)
      },
    })
  }

  const columns: Column<BackupInfo>[] = [
    {
      key: 'fileName',
      header: 'Filename',
      sortable: true,
    },
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
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            icon={<Download size={14} />}
            onClick={(e) => {
              e.stopPropagation()
              setRestoreTarget(row.fileName)
            }}
          >
            Restore
          </Button>
        </div>
      ),
    },
  ]

  return (
    <div className="space-y-6">
      <Card title="Backup Management" subtitle="Create, restore, and manage database backups">
        {/* Toolbar */}
        <div className="flex flex-wrap items-center gap-3 mb-4">
          <Button
            icon={<Plus size={16} />}
            loading={createBackup.isPending}
            onClick={handleCreate}
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

        {/* Error */}
        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-800 dark:bg-red-900/20 dark:text-red-400 mb-4">
            Failed to load backups: {error.message}
          </div>
        )}

        {/* Loading */}
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <Spinner />
          </div>
        ) : (
          <DataTable
            columns={columns}
            data={backups ?? []}
            emptyMessage="No backups found. Create one to get started."
          />
        )}
      </Card>

      {/* Restore confirmation dialog */}
      <Dialog
        open={restoreTarget !== null}
        onClose={() => setRestoreTarget(null)}
        title="Confirm Restore"
      >
        <p className="text-sm text-gray-600 dark:text-gray-300">
          This will overwrite current data. Are you sure you want to restore from{' '}
          <strong className="text-gray-900 dark:text-gray-100">{restoreTarget}</strong>?
        </p>
        <DialogFooter>
          <Button variant="secondary" onClick={() => setRestoreTarget(null)}>
            Cancel
          </Button>
          <Button
            variant="danger"
            icon={<RefreshCw size={16} />}
            loading={restoreBackup.isPending}
            onClick={handleRestore}
          >
            Restore
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  )
}
