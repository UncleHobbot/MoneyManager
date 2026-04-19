import { useState, useMemo, useCallback } from 'react'
import { Pencil, Trash2, Plus } from 'lucide-react'
import { useAccounts, useUpdateAccount, useDeleteAccount } from '@/hooks/useAccounts'
import {
  Button,
  Input,
  Select,
  Dialog,
  DialogFooter,
  DataTable,
  Badge,
  Spinner,
  AccountIcon,
} from '@/components/ui'
import type { Column } from '@/components/ui'
import type { Account } from '@/types'

const ACCOUNT_TYPES = [
  { label: 'Cash', value: 0 },
  { label: 'Credit Card', value: 1 },
  { label: 'Investment', value: 2 },
  { label: 'Other', value: 3 },
] as const

const emptyAccount: Account = {
  id: 0,
  name: '',
  shownName: '',
  description: null,
  type: 0,
  number: null,
  isHideFromGraph: false,
  alternativeName1: null,
  alternativeName2: null,
  alternativeName3: null,
  alternativeName4: null,
  alternativeName5: null,
  typeIconName: '',
}

export function AccountsPage() {
  const { data: accounts = [], isLoading } = useAccounts()
  const updateAccount = useUpdateAccount()
  const deleteAccount = useDeleteAccount()

  const [editOpen, setEditOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [editForm, setEditForm] = useState<Account>(emptyAccount)
  const [deleteTarget, setDeleteTarget] = useState<Account | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const openAdd = useCallback(() => {
    setEditForm(emptyAccount)
    setEditOpen(true)
  }, [])

  const openEdit = useCallback((account: Account) => {
    setEditForm({ ...account })
    setEditOpen(true)
  }, [])

  const openDelete = useCallback((account: Account) => {
    setDeleteTarget(account)
    setDeleteError(null)
    setDeleteOpen(true)
  }, [])

  const handleSave = useCallback(() => {
    updateAccount.mutate(editForm, {
      onSuccess: () => setEditOpen(false),
    })
  }, [editForm, updateAccount])

  const handleDelete = useCallback(() => {
    if (!deleteTarget) return
    deleteAccount.mutate(deleteTarget.id, {
      onSuccess: () => {
        setDeleteOpen(false)
        setDeleteTarget(null)
      },
      onError: (err) => {
        const status = (err as { response?: { status?: number } }).response?.status
        setDeleteError(
          status === 409
            ? 'Cannot delete this account because it has linked transactions.'
            : 'Failed to delete account.',
        )
      },
    })
  }, [deleteTarget, deleteAccount])

  const patch = useCallback(
    <K extends keyof Account>(key: K, value: Account[K]) =>
      setEditForm((prev) => ({ ...prev, [key]: value })),
    [],
  )

  const columns = useMemo<Column<Account>[]>(
    () => [
      {
        key: 'type',
        header: 'Type',
        className: 'w-16',
        render: (row) => <AccountIcon type={row.type} size={20} />,
      },
      { key: 'shownName', header: 'Shown Name', sortable: true },
      {
        key: 'description',
        header: 'Description',
        render: (row) => row.description ?? '',
      },
      {
        key: 'number',
        header: 'Number',
        render: (row) => row.number ?? '',
      },
      {
        key: 'isHideFromGraph',
        header: 'Hidden',
        className: 'w-24',
        render: (row) =>
          row.isHideFromGraph ? <Badge variant="yellow">Hidden</Badge> : null,
      },
      {
        key: 'actions',
        header: 'Actions',
        className: 'w-28',
        render: (row) => (
          <span className="inline-flex gap-1">
            <Button
              variant="ghost"
              size="sm"
              icon={<Pencil size={14} />}
              onClick={(e) => {
                e.stopPropagation()
                openEdit(row)
              }}
              aria-label="Edit"
            />
            <Button
              variant="ghost"
              size="sm"
              icon={<Trash2 size={14} />}
              onClick={(e) => {
                e.stopPropagation()
                openDelete(row)
              }}
              aria-label="Delete"
              className="text-red-500 hover:text-red-700"
            />
          </span>
        ),
      },
    ],
    [openEdit, openDelete],
  )

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="p-8 space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold dark:text-white">Accounts</h1>
        <Button icon={<Plus size={16} />} onClick={openAdd}>
          Add Account
        </Button>
      </div>

      <DataTable columns={columns} data={accounts} emptyMessage="No accounts yet." />

      {/* Edit / Add Dialog */}
      <Dialog
        open={editOpen}
        onClose={() => setEditOpen(false)}
        title={editForm.id === 0 ? 'Add Account' : 'Edit Account'}
      >
        <div className="space-y-4">
          <Select
            label="Type"
            options={ACCOUNT_TYPES.map((t) => ({ label: t.label, value: t.value }))}
            value={editForm.type}
            onChange={(v) => patch('type', Number(v))}
          />
          <Input
            label="Shown Name"
            value={editForm.shownName}
            onChange={(v) => patch('shownName', v)}
          />
          <Input
            label="Description"
            value={editForm.description ?? ''}
            onChange={(v) => patch('description', v || null)}
          />
          <Input
            label="Account Number"
            value={editForm.number ?? ''}
            onChange={(v) => patch('number', v || null)}
          />
          <Input
            label="Alternative Name 1"
            value={editForm.alternativeName1 ?? ''}
            onChange={(v) => patch('alternativeName1', v || null)}
          />
          <Input
            label="Alternative Name 2"
            value={editForm.alternativeName2 ?? ''}
            onChange={(v) => patch('alternativeName2', v || null)}
          />
          <Input
            label="Alternative Name 3"
            value={editForm.alternativeName3 ?? ''}
            onChange={(v) => patch('alternativeName3', v || null)}
          />
          <Input
            label="Alternative Name 4"
            value={editForm.alternativeName4 ?? ''}
            onChange={(v) => patch('alternativeName4', v || null)}
          />
          <Input
            label="Alternative Name 5"
            value={editForm.alternativeName5 ?? ''}
            onChange={(v) => patch('alternativeName5', v || null)}
          />
          <label className="inline-flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
            <input
              type="checkbox"
              checked={editForm.isHideFromGraph}
              onChange={(e) => patch('isHideFromGraph', e.target.checked)}
              className="rounded border-gray-300 dark:border-gray-600"
            />
            Hide from graphs
          </label>
        </div>
        <DialogFooter>
          <Button variant="secondary" onClick={() => setEditOpen(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave} loading={updateAccount.isPending}>
            Save
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteOpen}
        onClose={() => setDeleteOpen(false)}
        title="Delete Account"
      >
        <p className="text-sm text-gray-700 dark:text-gray-300">
          Are you sure you want to delete{' '}
          <span className="font-semibold">{deleteTarget?.shownName}</span>?
        </p>
        {deleteError && (
          <p className="mt-3 text-sm text-red-600 dark:text-red-400">{deleteError}</p>
        )}
        <DialogFooter>
          <Button variant="secondary" onClick={() => setDeleteOpen(false)}>
            Cancel
          </Button>
          <Button variant="danger" onClick={handleDelete} loading={deleteAccount.isPending}>
            Delete
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  )
}
