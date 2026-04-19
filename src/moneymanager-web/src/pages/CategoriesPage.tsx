import { useState, useCallback } from 'react'
import {
  useCategoryTree,
  useCategoryIcons,
  useUpdateCategory,
  useDeleteCategory,
} from '@/hooks/useCategories'
import { Button, Input, Select, Dialog, DialogFooter, Spinner, CategoryIcon } from '@/components/ui'
import type { CategoryTree, Category } from '@/types'

/** Blank category used when creating a new entry. */
function blankCategory(parentId: number | null): Category {
  return { id: 0, name: '', icon: null, isNew: true, pIcon: null, parent: null, _parentId: parentId } as Category & { _parentId: number | null }
}

/** Flatten the tree into a list of {id, name, depth} for the parent selector. */
function flattenTree(nodes: CategoryTree[], depth = 0): { id: number; name: string; depth: number }[] {
  const result: { id: number; name: string; depth: number }[] = []
  for (const n of nodes) {
    result.push({ id: n.id, name: n.name, depth })
    if (n.children?.length) result.push(...flattenTree(n.children, depth + 1))
  }
  return result
}

// ── Tree node ────────────────────────────────────────────────────────

interface TreeNodeProps {
  node: CategoryTree
  depth: number
  selectedId: number | null
  expanded: Set<number>
  onToggle: (id: number) => void
  onSelect: (node: CategoryTree) => void
}

function TreeNode({ node, depth, selectedId, expanded, onToggle, onSelect }: TreeNodeProps) {
  const hasChildren = node.children?.length > 0
  const isExpanded = expanded.has(node.id)
  const isSelected = selectedId === node.id

  return (
    <>
      <button
        type="button"
        onClick={() => onSelect(node)}
        className={`w-full flex items-center gap-2 px-3 py-1.5 text-sm rounded-md transition-colors duration-100 ${
          isSelected
            ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-200'
            : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
        }`}
        style={{ paddingLeft: `${depth * 20 + 12}px` }}
      >
        {/* expand / collapse toggle */}
        {hasChildren ? (
          <span
            role="button"
            tabIndex={-1}
            onClick={(e) => { e.stopPropagation(); onToggle(node.id) }}
            onKeyDown={(e) => { if (e.key === 'Enter') { e.stopPropagation(); onToggle(node.id) } }}
            className="w-4 text-gray-400 dark:text-gray-500 select-none"
          >
            {isExpanded ? '▾' : '▸'}
          </span>
        ) : (
          <span className="w-4" />
        )}
        <CategoryIcon icon={node.icon ?? undefined} size={16} />
        <span className="truncate">{node.name}</span>
      </button>

      {hasChildren && isExpanded &&
        node.children.map((child) => (
          <TreeNode
            key={child.id}
            node={child}
            depth={depth + 1}
            selectedId={selectedId}
            expanded={expanded}
            onToggle={onToggle}
            onSelect={onSelect}
          />
        ))}
    </>
  )
}

// ── Main page ────────────────────────────────────────────────────────

export default function CategoriesPage() {
  const { data: tree, isLoading: treeLoading } = useCategoryTree()
  const { data: icons } = useCategoryIcons()
  const updateCategory = useUpdateCategory()
  const deleteCategory = useDeleteCategory()

  const [selectedNode, setSelectedNode] = useState<CategoryTree | null>(null)
  const [expanded, setExpanded] = useState<Set<number>>(() => new Set())
  const [editing, setEditing] = useState<(Category & { _parentId: number | null }) | null>(null)
  const [confirmDelete, setConfirmDelete] = useState(false)

  // ── helpers ──

  const toggleExpand = useCallback((id: number) => {
    setExpanded((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }, [])

  const handleSelect = useCallback((node: CategoryTree) => {
    setSelectedNode(node)
    setEditing({
      id: node.id,
      name: node.name,
      icon: node.icon,
      isNew: false,
      pIcon: null,
      parent: null,
      _parentId: node.parent?.id ?? null,
    } as Category & { _parentId: number | null })
    setConfirmDelete(false)
  }, [])

  const handleAddRoot = useCallback(() => {
    setSelectedNode(null)
    setEditing(blankCategory(null) as Category & { _parentId: number | null })
    setConfirmDelete(false)
  }, [])

  const handleAddChild = useCallback(() => {
    if (!selectedNode) return
    setEditing(blankCategory(selectedNode.id) as Category & { _parentId: number | null })
    setConfirmDelete(false)
  }, [selectedNode])

  const handleSave = useCallback(async () => {
    if (!editing || !editing.name.trim()) return

    const payload: Category = {
      id: editing.id,
      name: editing.name.trim(),
      icon: editing.icon,
      isNew: editing.id === 0,
      pIcon: null,
      parent: editing._parentId != null ? ({ id: editing._parentId } as Category) : null,
    }

    await updateCategory.mutateAsync(payload)
    setEditing(null)
    setSelectedNode(null)
  }, [editing, updateCategory])

  const handleDelete = useCallback(async () => {
    if (!editing || editing.id === 0) return
    await deleteCategory.mutateAsync(editing.id)
    setEditing(null)
    setSelectedNode(null)
    setConfirmDelete(false)
  }, [editing, deleteCategory])

  // ── flat list for parent selector (exclude self + descendants) ──

  const parentOptions = (() => {
    if (!tree) return []
    const flat = flattenTree(tree)
    if (editing && editing.id !== 0) {
      // Build set of ids to exclude (self + all descendants)
      const excludeIds = new Set<number>()
      const collectDescendants = (nodes: CategoryTree[], collecting: boolean): void => {
        for (const n of nodes) {
          if (n.id === editing.id || collecting) {
            excludeIds.add(n.id)
            if (n.children?.length) collectDescendants(n.children, true)
          } else if (n.children?.length) {
            collectDescendants(n.children, false)
          }
        }
      }
      collectDescendants(tree, false)
      return flat.filter((f) => !excludeIds.has(f.id))
    }
    return flat
  })()

  // ── render ──

  if (treeLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
        <h1 className="text-xl font-semibold text-gray-900 dark:text-white">Categories</h1>
        <div className="flex gap-2">
          <Button size="sm" variant="secondary" onClick={handleAddRoot}>
            + New Category
          </Button>
          {selectedNode && (
            <Button size="sm" variant="secondary" onClick={handleAddChild}>
              + Child of "{selectedNode.name}"
            </Button>
          )}
        </div>
      </div>

      {/* Body — two panels */}
      <div className="flex flex-1 min-h-0">
        {/* Left: tree */}
        <div className="w-80 shrink-0 border-r border-gray-200 dark:border-gray-700 overflow-y-auto py-2">
          {tree?.length ? (
            tree.map((node) => (
              <TreeNode
                key={node.id}
                node={node}
                depth={0}
                selectedId={selectedNode?.id ?? null}
                expanded={expanded}
                onToggle={toggleExpand}
                onSelect={handleSelect}
              />
            ))
          ) : (
            <p className="px-4 py-6 text-sm text-gray-500 dark:text-gray-400">
              No categories yet. Click "+ New Category" to get started.
            </p>
          )}
        </div>

        {/* Right: edit panel */}
        <div className="flex-1 overflow-y-auto p-6">
          {editing ? (
            <div className="max-w-md space-y-5">
              <h2 className="text-lg font-medium text-gray-900 dark:text-white">
                {editing.id === 0 ? 'New Category' : `Edit: ${editing.name}`}
              </h2>

              <Input
                label="Name"
                value={editing.name}
                onChange={(v) => setEditing({ ...editing, name: v })}
                placeholder="Category name"
              />

              {/* Icon selector with previews */}
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-gray-700 dark:text-gray-300">Icon</label>
                <div className="flex items-center gap-3">
                  <CategoryIcon icon={editing.icon ?? undefined} size={24} className="text-gray-600 dark:text-gray-300" />
                  <select
                    value={editing.icon ?? ''}
                    onChange={(e) => setEditing({ ...editing, icon: e.target.value || null })}
                    className="flex-1 rounded-lg border border-gray-200 dark:border-gray-700 px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  >
                    <option value="">None</option>
                    {icons?.map((icon) => (
                      <option key={icon} value={icon}>
                        {icon}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Parent selector */}
              <Select
                label="Parent Category"
                value={editing._parentId?.toString() ?? ''}
                onChange={(v) => setEditing({ ...editing, _parentId: v ? Number(v) : null })}
                placeholder="None (top-level)"
                options={parentOptions.map((p) => ({
                  value: p.id,
                  label: `${'  '.repeat(p.depth)}${p.name}`,
                }))}
              />

              {/* Action buttons */}
              <div className="flex items-center gap-3 pt-2">
                <Button onClick={handleSave} loading={updateCategory.isPending}>
                  {editing.id === 0 ? 'Create' : 'Save'}
                </Button>
                <Button variant="secondary" onClick={() => { setEditing(null); setSelectedNode(null) }}>
                  Cancel
                </Button>
                {editing.id !== 0 && (
                  <Button variant="danger" onClick={() => setConfirmDelete(true)}>
                    Delete
                  </Button>
                )}
              </div>
            </div>
          ) : (
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-12">
              Select a category from the tree, or create a new one.
            </p>
          )}
        </div>
      </div>

      {/* Delete confirmation dialog */}
      <Dialog open={confirmDelete} onClose={() => setConfirmDelete(false)} title="Delete Category">
        <p className="text-sm text-gray-700 dark:text-gray-300">
          Are you sure you want to delete <strong>{editing?.name}</strong>? This cannot be undone.
        </p>
        <DialogFooter>
          <Button variant="secondary" onClick={() => setConfirmDelete(false)}>
            Cancel
          </Button>
          <Button variant="danger" onClick={handleDelete} loading={deleteCategory.isPending}>
            Delete
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  )
}
