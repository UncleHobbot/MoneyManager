import { useState, useMemo, useCallback } from 'react'
import { useRules, useUpdateRule, useDeleteRule, useApplyAllRules } from '@/hooks/useRules'
import { useCategories } from '@/hooks/useCategories'
import { Button, Input, Select, Dialog, DialogFooter, DataTable, Badge, Spinner, CategoryIcon } from '@/components/ui'
import type { Column } from '@/components/ui'
import type { Rule, Category } from '@/types'
import { Plus, Play, Pencil, Trash2 } from 'lucide-react'

const COMPARE_TYPES = [
  { value: 0, label: 'Contains' },
  { value: 1, label: 'Starts With' },
  { value: 2, label: 'Ends With' },
  { value: 3, label: 'Equals' },
] as const

const COMPARE_BADGE_VARIANT: Record<number, 'blue' | 'green' | 'yellow' | 'red'> = {
  0: 'blue',
  1: 'green',
  2: 'yellow',
  3: 'red',
}

const emptyRule: Rule = {
  id: 0,
  originalDescription: '',
  newDescription: '',
  compareType: 0,
  compareTypeString: 'Contains',
  category: { id: 0, parent: null, name: '', icon: null, isNew: false, pIcon: null },
}

export default function RulesPage() {
  const { data: rules = [], isLoading } = useRules()
  const { data: categories = [] } = useCategories()
  const updateRule = useUpdateRule()
  const deleteRule = useDeleteRule()
  const applyAll = useApplyAllRules()

  const [editOpen, setEditOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [editingRule, setEditingRule] = useState<Rule>(emptyRule)
  const [deletingRule, setDeletingRule] = useState<Rule | null>(null)
  const [applyResult, setApplyResult] = useState<number | null>(null)

  const categoryOptions = useMemo(
    () => categories.map((c: Category) => ({ label: c.name, value: c.id })),
    [categories],
  )

  const compareTypeOptions = useMemo(
    () => COMPARE_TYPES.map((t) => ({ label: t.label, value: t.value })),
    [],
  )

  const openAdd = useCallback(() => {
    setEditingRule({ ...emptyRule })
    setEditOpen(true)
  }, [])

  const openEdit = useCallback((rule: Rule) => {
    setEditingRule({ ...rule })
    setEditOpen(true)
  }, [])

  const openDelete = useCallback((rule: Rule) => {
    setDeletingRule(rule)
    setDeleteOpen(true)
  }, [])

  const handleSave = useCallback(() => {
    const selectedCategory = categories.find((c: Category) => c.id === editingRule.category.id)
    const compareLabel = COMPARE_TYPES.find((t) => t.value === editingRule.compareType)?.label ?? ''
    const ruleToSave: Rule = {
      ...editingRule,
      compareTypeString: compareLabel,
      category: selectedCategory ?? editingRule.category,
    }
    updateRule.mutate(ruleToSave, {
      onSuccess: () => setEditOpen(false),
    })
  }, [editingRule, categories, updateRule])

  const handleDelete = useCallback(() => {
    if (!deletingRule) return
    deleteRule.mutate(deletingRule.id, {
      onSuccess: () => {
        setDeleteOpen(false)
        setDeletingRule(null)
      },
    })
  }, [deletingRule, deleteRule])

  const handleApplyAll = useCallback(() => {
    setApplyResult(null)
    applyAll.mutate(undefined, {
      onSuccess: (count: number) => setApplyResult(count),
    })
  }, [applyAll])

  const columns: Column<Rule>[] = useMemo(
    () => [
      {
        key: 'compareType',
        header: 'Compare Type',
        sortable: true,
        className: 'w-36',
        render: (rule: Rule) => (
          <Badge variant={COMPARE_BADGE_VARIANT[rule.compareType] ?? 'default'}>
            {rule.compareTypeString}
          </Badge>
        ),
      },
      {
        key: 'originalDescription',
        header: 'Original Description',
        sortable: true,
      },
      {
        key: 'newDescription',
        header: 'New Description',
        sortable: true,
      },
      {
        key: 'category',
        header: 'Category',
        sortable: false,
        render: (rule: Rule) =>
          rule.category ? (
            <span className="inline-flex items-center gap-1.5">
              <CategoryIcon icon={rule.category.icon ?? rule.category.pIcon ?? undefined} size={16} />
              <span>{rule.category.name}</span>
            </span>
          ) : (
            <span className="text-gray-400">—</span>
          ),
      },
      {
        key: 'actions',
        header: 'Actions',
        className: 'w-28',
        render: (rule: Rule) => (
          <span className="inline-flex items-center gap-1">
            <Button
              variant="ghost"
              size="sm"
              icon={<Pencil size={14} />}
              onClick={(e) => {
                e.stopPropagation()
                openEdit(rule)
              }}
              aria-label="Edit rule"
            />
            <Button
              variant="ghost"
              size="sm"
              icon={<Trash2 size={14} />}
              onClick={(e) => {
                e.stopPropagation()
                openDelete(rule)
              }}
              aria-label="Delete rule"
              className="text-red-500 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300"
            />
          </span>
        ),
      },
    ],
    [openEdit, openDelete],
  )

  const canSave =
    editingRule.originalDescription.trim() !== '' && editingRule.category.id !== 0

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-16">
        <Spinner />
      </div>
    )
  }

  return (
    <div className="space-y-6 p-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold dark:text-white">Rules</h1>
          <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
            Auto-categorization rules for imported transactions
          </p>
        </div>
        <div className="flex items-center gap-3">
          {applyResult !== null && (
            <span className="text-sm text-green-600 dark:text-green-400">
              {applyResult} transaction{applyResult !== 1 ? 's' : ''} updated
            </span>
          )}
          <Button
            variant="secondary"
            icon={<Play size={16} />}
            onClick={handleApplyAll}
            loading={applyAll.isPending}
          >
            Apply All Rules
          </Button>
          <Button icon={<Plus size={16} />} onClick={openAdd}>
            Add Rule
          </Button>
        </div>
      </div>

      {/* Table */}
      <DataTable columns={columns} data={rules} emptyMessage="No rules defined yet" />

      {/* Edit / Add Dialog */}
      <Dialog
        open={editOpen}
        onClose={() => setEditOpen(false)}
        title={editingRule.id === 0 ? 'Add Rule' : 'Edit Rule'}
      >
        <div className="space-y-4">
          <Select
            label="Compare Type"
            options={compareTypeOptions}
            value={editingRule.compareType}
            onChange={(v) =>
              setEditingRule((prev) => ({ ...prev, compareType: Number(v) }))
            }
          />
          <Input
            label="Original Description"
            value={editingRule.originalDescription}
            onChange={(v) =>
              setEditingRule((prev) => ({ ...prev, originalDescription: v }))
            }
            placeholder="Text to match in original description"
          />
          <Input
            label="New Description"
            value={editingRule.newDescription}
            onChange={(v) =>
              setEditingRule((prev) => ({ ...prev, newDescription: v }))
            }
            placeholder="Replacement description (optional)"
          />
          <Select
            label="Category"
            options={categoryOptions}
            value={editingRule.category.id || ''}
            onChange={(v) =>
              setEditingRule((prev) => ({
                ...prev,
                category: { ...prev.category, id: Number(v) },
              }))
            }
            placeholder="Select a category"
          />
        </div>
        <DialogFooter>
          <Button variant="secondary" onClick={() => setEditOpen(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave} loading={updateRule.isPending} disabled={!canSave}>
            {editingRule.id === 0 ? 'Create' : 'Save'}
          </Button>
        </DialogFooter>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)} title="Delete Rule">
        <p className="text-sm text-gray-600 dark:text-gray-300">
          Are you sure you want to delete the rule matching{' '}
          <strong>&quot;{deletingRule?.originalDescription}&quot;</strong>? This action cannot be
          undone.
        </p>
        <DialogFooter>
          <Button variant="secondary" onClick={() => setDeleteOpen(false)}>
            Cancel
          </Button>
          <Button variant="danger" onClick={handleDelete} loading={deleteRule.isPending}>
            Delete
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  )
}
