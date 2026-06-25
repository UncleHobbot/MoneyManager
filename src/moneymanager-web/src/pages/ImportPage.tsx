import { useState, useCallback, useRef, type DragEvent, type ChangeEvent } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import { queryKeys } from '@/lib/queryKeys'
import { Button, Select, Card, DataTable, Badge, Dialog, DialogFooter, Spinner } from '@/components/ui'
import type { Column } from '@/components/ui'
import type { ImportResult } from '@/types'
import { Upload, Trash2, Eye, X, CheckCircle2, AlertCircle, Loader2 } from 'lucide-react'

// ── Types ──────────────────────────────────────────────────────────────────────

interface CsvArchiveFile {
  fileName: string
  date: string
  sizeBytes: number
}

type FileStatus = 'pending' | 'uploading' | 'done' | 'error'

interface BatchFile {
  id: string
  file: File
  status: FileStatus
  result?: ImportResult
  error?: string
}

const bankTypeOptions = [
  { label: 'Auto-detect', value: 'Auto' },
  { label: 'Mint', value: 'Mint' },
  { label: 'RBC', value: 'RBC' },
  { label: 'CIBC', value: 'CIBC' },
]

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function errorMessage(err: unknown): string {
  if (typeof err === 'object' && err !== null && 'response' in err) {
    const data = (err as { response?: { data?: unknown } }).response?.data
    if (typeof data === 'string' && data.trim()) return data
  }
  return err instanceof Error ? err.message : 'Upload failed'
}

let fileIdCounter = 0
function toBatchFiles(files: File[]): BatchFile[] {
  return files.map((file) => ({ id: `${Date.now()}-${fileIdCounter++}`, file, status: 'pending' as const }))
}

// ── Component ──────────────────────────────────────────────────────────────────

export default function ImportPage() {
  const queryClient = useQueryClient()

  // Upload state
  const [batchFiles, setBatchFiles] = useState<BatchFile[]>([])
  const [bankType, setBankType] = useState('Auto')
  const [createAccounts, setCreateAccounts] = useState(true)
  const [isDragging, setIsDragging] = useState(false)
  const [isUploading, setIsUploading] = useState(false)
  const [backupError, setBackupError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  // CSV archive dialog state
  const [viewingFile, setViewingFile] = useState<string | null>(null)
  const [csvRows, setCsvRows] = useState<string[][]>([])

  // ── CSV archive query ──────────────────────────────────────────────────────

  const archiveQuery = useQuery<CsvArchiveFile[]>({
    queryKey: queryKeys.csvArchive,
    queryFn: () => api.get('/import/csv-archive').then(r => r.data),
  })

  const deleteMutation = useMutation({
    mutationFn: (fileName: string) => api.delete(`/import/csv-archive/${encodeURIComponent(fileName)}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.csvArchive }),
  })

  const viewMutation = useMutation({
    mutationFn: (fileName: string) =>
      api.get<string>(`/import/csv-archive/${encodeURIComponent(fileName)}`, { responseType: 'text' as const })
        .then(r => r.data),
    onSuccess: (text, fileName) => {
      const lines = text.split('\n').filter(l => l.trim())
      const parsed = lines.map(line => {
        const row: string[] = []
        let current = ''
        let inQuotes = false
        for (const ch of line) {
          if (ch === '"') {
            inQuotes = !inQuotes
          } else if (ch === ',' && !inQuotes) {
            row.push(current.trim())
            current = ''
          } else {
            current += ch
          }
        }
        row.push(current.trim())
        return row
      })
      setCsvRows(parsed)
      setViewingFile(fileName)
    },
  })

  // ── File selection ─────────────────────────────────────────────────────────

  const addFiles = useCallback((files: File[]) => {
    const csvs = files.filter(f => f.name.toLowerCase().endsWith('.csv'))
    if (csvs.length === 0) return
    setBackupError(null)
    setBatchFiles(prev => [...prev, ...toBatchFiles(csvs)])
  }, [])

  const removeFile = useCallback((id: string) => {
    setBatchFiles(prev => prev.filter(f => f.id !== id))
  }, [])

  // ── Drag-and-drop handlers ─────────────────────────────────────────────────

  const handleDragOver = useCallback((e: DragEvent) => {
    e.preventDefault()
    setIsDragging(true)
  }, [])

  const handleDragLeave = useCallback((e: DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
  }, [])

  const handleDrop = useCallback((e: DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    addFiles(Array.from(e.dataTransfer.files))
  }, [addFiles])

  const handleFileChange = useCallback((e: ChangeEvent<HTMLInputElement>) => {
    addFiles(Array.from(e.target.files ?? []))
    if (e.target) e.target.value = ''
  }, [addFiles])

  const updateFile = useCallback((id: string, patch: Partial<BatchFile>) => {
    setBatchFiles(prev => prev.map(f => (f.id === id ? { ...f, ...patch } : f)))
  }, [])

  // ── Batch upload ───────────────────────────────────────────────────────────
  // One backup before the batch (aborts if it fails), then files are uploaded
  // sequentially so each SQLite write + dedup runs in isolation (ADR-0008).

  const handleUpload = useCallback(async () => {
    const pending = batchFiles.filter(f => f.status !== 'done')
    if (pending.length === 0) return

    setIsUploading(true)
    setBackupError(null)

    try {
      await api.post('/system/backup')
    } catch (err) {
      setBackupError(`Backup failed, import aborted: ${errorMessage(err)}`)
      setIsUploading(false)
      return
    }

    for (const entry of pending) {
      updateFile(entry.id, { status: 'uploading', error: undefined })
      try {
        const formData = new FormData()
        formData.append('file', entry.file)
        const { data } = await api.post<ImportResult>(
          `/import/upload?bankType=${bankType}&createAccounts=${createAccounts}`,
          formData,
        )
        updateFile(entry.id, { status: 'done', result: data })
      } catch (err) {
        updateFile(entry.id, { status: 'error', error: errorMessage(err) })
      }
    }

    setIsUploading(false)
    queryClient.invalidateQueries({ queryKey: queryKeys.csvArchive })
    queryClient.invalidateQueries({ queryKey: queryKeys.transactions.all })
  }, [batchFiles, bankType, createAccounts, updateFile, queryClient])

  // ── Derived totals ─────────────────────────────────────────────────────────

  const doneFiles = batchFiles.filter(f => f.status === 'done')
  const totalImported = doneFiles.reduce((sum, f) => sum + (f.result?.importedCount ?? 0), 0)
  const totalSkipped = doneFiles.reduce((sum, f) => sum + (f.result?.skippedCount ?? 0), 0)
  const hasPending = batchFiles.some(f => f.status !== 'done')

  // ── Archive table columns ──────────────────────────────────────────────────

  const archiveColumns: Column<CsvArchiveFile>[] = [
    { key: 'fileName', header: 'Filename', sortable: true },
    {
      key: 'date',
      header: 'Date',
      sortable: true,
      render: (row) => new Date(row.date).toLocaleDateString(),
    },
    {
      key: 'sizeBytes',
      header: 'Size',
      sortable: true,
      render: (row) => formatBytes(row.sizeBytes),
    },
    {
      key: '_actions',
      header: '',
      className: 'w-24',
      render: (row) => (
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            icon={<Eye size={14} />}
            loading={viewMutation.isPending && viewMutation.variables === row.fileName}
            onClick={(e) => { e.stopPropagation(); viewMutation.mutate(row.fileName) }}
          />
          <Button
            variant="ghost"
            size="sm"
            icon={<Trash2 size={14} />}
            loading={deleteMutation.isPending && deleteMutation.variables === row.fileName}
            onClick={(e) => { e.stopPropagation(); deleteMutation.mutate(row.fileName) }}
          />
        </div>
      ),
    },
  ]

  // ── CSV viewer columns ─────────────────────────────────────────────────────

  const csvHeaders = csvRows[0] ?? []
  const csvData = csvRows.slice(1)
  const csvColumns: Column<string[]>[] = csvHeaders.map((header, i) => ({
    key: String(i),
    header: header || `Col ${i + 1}`,
    render: (row: string[]) => row[i] ?? '',
  }))

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-6 p-8">
      <h1 className="text-2xl font-semibold dark:text-white">Import Transactions</h1>

      {/* Upload Section */}
      <Card title="Upload CSV" subtitle="Import transactions from one or more bank CSV files">
        <div className="space-y-4">
          {/* Drop zone */}
          <div
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
            className={`flex cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed p-8 transition-colors duration-150 ${
              isDragging
                ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
                : 'border-gray-300 hover:border-gray-400 dark:border-gray-600 dark:hover:border-gray-500'
            }`}
          >
            <Upload size={32} className="text-gray-400" />
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Drag & drop CSV files here, or click to browse
            </p>
            <p className="text-xs text-gray-400">Supports multiple .csv files</p>
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv"
              multiple
              onChange={handleFileChange}
              className="hidden"
            />
          </div>

          {/* Selected files list */}
          {batchFiles.length > 0 && (
            <ul className="divide-y divide-gray-200 rounded-lg border border-gray-200 dark:divide-gray-700 dark:border-gray-700">
              {batchFiles.map((entry) => (
                <li key={entry.id} className="flex items-center gap-3 px-3 py-2 text-sm">
                  <StatusIcon status={entry.status} />
                  <span className="flex-1 truncate text-gray-700 dark:text-gray-300">
                    {entry.file.name}{' '}
                    <span className="text-gray-400">({formatBytes(entry.file.size)})</span>
                  </span>
                  {entry.status === 'done' && entry.result && (
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      {entry.result.importedCount} imported, {entry.result.skippedCount} skipped
                    </span>
                  )}
                  {entry.status === 'error' && (
                    <span className="text-xs text-red-600 dark:text-red-400">{entry.error}</span>
                  )}
                  {entry.status === 'pending' && !isUploading && (
                    <button
                      onClick={() => removeFile(entry.id)}
                      className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200"
                      aria-label="Remove file"
                    >
                      <X size={16} />
                    </button>
                  )}
                </li>
              ))}
            </ul>
          )}

          {/* Options row */}
          <div className="flex flex-wrap items-end gap-4">
            <Select
              label="Bank Type"
              options={bankTypeOptions}
              value={bankType}
              onChange={setBankType}
            />

            <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 pb-1">
              <input
                type="checkbox"
                checked={createAccounts}
                onChange={(e) => setCreateAccounts(e.target.checked)}
                className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              Create accounts during import
            </label>

            <Button
              onClick={handleUpload}
              disabled={!hasPending}
              loading={isUploading}
              icon={<Upload size={16} />}
            >
              Upload{batchFiles.length > 1 ? ` ${batchFiles.length} files` : ''}
            </Button>
          </div>

          {/* Backup error (aborts the batch) */}
          {backupError && (
            <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
              {backupError}
            </div>
          )}

          {/* Aggregate summary */}
          {doneFiles.length > 0 && (
            <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-700 dark:bg-gray-800/50">
              <h4 className="mb-2 text-sm font-semibold text-gray-900 dark:text-gray-100">
                Import Complete — {doneFiles.length} file{doneFiles.length !== 1 ? 's' : ''}
              </h4>
              <div className="flex flex-wrap gap-3">
                <Badge variant="green">{totalImported} imported</Badge>
                <Badge variant="yellow">{totalSkipped} skipped</Badge>
              </div>
            </div>
          )}
        </div>
      </Card>

      {/* CSV Archive Section */}
      <Card title="CSV Archive" subtitle="Previously imported CSV files">
        {archiveQuery.isLoading ? (
          <div className="flex justify-center py-8">
            <Spinner />
          </div>
        ) : archiveQuery.isError ? (
          <p className="text-sm text-red-500">Failed to load archive</p>
        ) : (
          <DataTable
            columns={archiveColumns}
            data={archiveQuery.data ?? []}
            emptyMessage="No archived CSV files"
          />
        )}
      </Card>

      {/* CSV Viewer Dialog */}
      <Dialog
        open={viewingFile !== null}
        onClose={() => setViewingFile(null)}
        title={viewingFile ?? 'CSV Viewer'}
      >
        <div className="max-h-96 overflow-auto">
          {csvData.length > 0 ? (
            <DataTable columns={csvColumns} data={csvData} />
          ) : (
            <p className="text-sm text-gray-400">No data</p>
          )}
        </div>
        <DialogFooter>
          <Button variant="secondary" onClick={() => setViewingFile(null)}>
            Close
          </Button>
        </DialogFooter>
      </Dialog>
    </div>
  )
}

// ── Helpers ────────────────────────────────────────────────────────────────────

function StatusIcon({ status }: { status: FileStatus }) {
  switch (status) {
    case 'uploading':
      return <Loader2 size={16} className="animate-spin text-blue-500" />
    case 'done':
      return <CheckCircle2 size={16} className="text-green-500" />
    case 'error':
      return <AlertCircle size={16} className="text-red-500" />
    default:
      return <Upload size={16} className="text-gray-400" />
  }
}
