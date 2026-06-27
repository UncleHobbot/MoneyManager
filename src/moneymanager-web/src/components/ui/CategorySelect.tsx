import { useEffect, useRef, useState, type FC } from 'react'
import { ChevronDown } from 'lucide-react'
import { CategoryIcon } from './CategoryIcon'
import type { Category } from '@/types'

interface CategorySelectProps {
  id?: string
  label?: string
  categories: Category[]
  value?: number
  onChange?: (value?: number) => void
  placeholder?: string
}

export const CategorySelect: FC<CategorySelectProps> = ({
  id,
  label,
  categories,
  value,
  onChange,
  placeholder = 'Select category',
}) => {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const containerRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const listRef = useRef<HTMLUListElement>(null)

  const selected = categories.find(c => c.id === value)

  const filtered = search
    ? categories.filter(c => c.name.toLowerCase().includes(search.toLowerCase()))
    : categories

  useEffect(() => {
    if (!open) return
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
        setSearch('')
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [open])

  useEffect(() => {
    if (open) inputRef.current?.focus()
  }, [open])

  function handleSelect(categoryId: number) {
    onChange?.(categoryId)
    setOpen(false)
    setSearch('')
  }

  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'Escape') {
      setOpen(false)
      setSearch('')
    }
  }

  const selectId = id ?? label?.toLowerCase().replace(/\s+/g, '-')

  return (
    <div className="flex flex-col gap-1.5" ref={containerRef}>
      {label && (
        <label htmlFor={selectId} className="text-sm font-medium text-gray-700 dark:text-gray-300">
          {label}
        </label>
      )}
      <button
        id={selectId}
        type="button"
        onClick={() => setOpen(!open)}
        className="flex items-center justify-between rounded-lg border border-gray-200 bg-white px-3 py-2 text-left text-sm transition-colors duration-150 focus:border-transparent focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-gray-700 dark:bg-gray-800"
      >
        {selected ? (
          <span className="flex items-center gap-2 text-gray-900 dark:text-gray-100">
            <CategoryIcon icon={selected.icon ?? selected.pIcon ?? undefined} size={16} />
            <span>{selected.name}</span>
            {selected.parent && (
              <span className="flex items-center gap-1 text-xs text-gray-400 dark:text-gray-500">
                ·
                <CategoryIcon icon={selected.parent.icon ?? undefined} size={12} />
                {selected.parent.name}
              </span>
            )}
          </span>
        ) : (
          <span className="text-gray-400 dark:text-gray-500">{placeholder}</span>
        )}
        <ChevronDown size={16} className="shrink-0 text-gray-400" />
      </button>

      {open && (
        <div className="relative z-50">
          <div className="absolute top-0 left-0 right-0 max-h-64 overflow-auto rounded-lg border border-gray-200 bg-white shadow-lg dark:border-gray-700 dark:bg-gray-800">
            <div className="sticky top-0 border-b border-gray-200 bg-white p-1.5 dark:border-gray-700 dark:bg-gray-800">
              <input
                ref={inputRef}
                type="text"
                value={search}
                onChange={e => setSearch(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Search categories..."
                className="w-full rounded border-0 bg-gray-50 px-2 py-1.5 text-sm text-gray-900 focus:outline-none focus:ring-1 focus:ring-blue-500 dark:bg-gray-700 dark:text-gray-100"
              />
            </div>
            <ul ref={listRef} role="listbox">
              {filtered.map(category => (
                <li
                  key={category.id}
                  role="option"
                  aria-selected={category.id === value}
                  onClick={() => handleSelect(category.id)}
                  className={`flex cursor-pointer items-center gap-2 px-3 py-2 text-sm hover:bg-blue-50 dark:hover:bg-gray-700 ${
                    category.id === value ? 'bg-blue-50 font-medium dark:bg-gray-700' : ''
                  }`}
                >
                  <CategoryIcon icon={category.icon ?? category.pIcon ?? undefined} size={16} className="shrink-0" />
                  <span className="text-gray-900 dark:text-gray-100">{category.name}</span>
                  {category.parent && (
                    <span className="flex items-center gap-1 text-xs text-gray-400 dark:text-gray-500">
                      ·
                      <CategoryIcon icon={category.parent.icon ?? undefined} size={12} className="shrink-0" />
                      {category.parent.name}
                    </span>
                  )}
                </li>
              ))}
              {filtered.length === 0 && (
                <li className="px-3 py-2 text-sm text-gray-400 dark:text-gray-500">No categories found</li>
              )}
            </ul>
          </div>
        </div>
      )}
    </div>
  )
}
