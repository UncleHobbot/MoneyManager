import { useState, useCallback, useRef, type DragEvent, type ChangeEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import api from '@/api/client'
import { Button, Select, Card, DataTable, Badge, Dialog, DialogFooter, Spinner } from '@/components/ui'
import type { Column } from '@/components/ui'
import type { ImportResult } from '@/types'
import { Upload, Trash2, Eye } from 'lucide-react'

// ── Types ──────────────────────────────────────────────────────────────────────

interface CsvArchiveFile {
  fileName: string
  date: string
  sizeBytes: number
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

// ── Component ──────────────────────────────────────────────────────────────────

export default function ImportPage() {
  const queryClient = useQueryClient()

  // Upload state
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [bankType, setBankType] = useState('Auto')
  const [createAccounts, setCreateAccounts] = useState(true)
  const [isDragging, setIsDragging] = useState(false)
  const [importResult, setImportResult] = useState<ImportResult | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  // CSV archive dialog state
  const [viewingFile, setViewingFile] = useState<string | null>(null)
  const [csvRows, setCsvRows] = useState<string[][]>([])

  // ── Upload mutation ────────────────────────────────────────────────────────

  const uploadMutation = useMutation({
    mutationFn: async (file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      const { data } = await api.post<ImportResult>(
        `/import/upload?bankType=${bankType}&createAccounts=${createAccounts}`,
        formData,
      )
      return data
    },
    onSuccess: (data) => {
      setImportResult(data)
      setSelectedFile(null)
      queryClient.invalidateQueries({ queryKey: ['csv-archive'] })
      queryClient.invalidateQueries({ queryKey: ['transactions'] })
    },
  })

  // ── CSV archive query ──────────────────────────────────────────────────────

  const archiveQuery = useQuery<CsvArchiveFile[]>({
    queryKey: ['csv-archive'],
    queryFn: () => api.get('/import/csv-archive').then(r => r.data),
  })

  const deleteMutation = useMutation({
    mutationFn: (fileName: string) => api.delete(`/import/csv-archive/${encodeURIComponent(fileName)}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['csv-archive'] }),
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
    const file = e.dataTransfer.files[0]
    if (file?.name.endsWith('.csv')) {
      setSelectedFile(file)
      setImportResult(null)
    }
  }, [])

  const handleFileChange = useCallback((e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null
    setSelectedFile(file)
    setImportResult(null)
    if (e.target) e.target.value = ''
  }, [])

  const handleUpload = useCallback(() => {
    if (selectedFile) uploadMutation.mutate(selectedFile)
  }, [selectedFile, uploadMutation])

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
      <Card title="Upload CSV" subtitle="Import transactions from a bank CSV file">
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
            {selectedFile ? (
              <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
                {selectedFile.name}{' '}
                <span className="text-gray-400">({formatBytes(selectedFile.size)})</span>
              </p>
            ) : (
              <>
                <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Drag & drop a CSV file here, or click to browse
                </p>
                <p className="text-xs text-gray-400">Supports .csv files</p>
              </>
            )}
            <input
              ref={fileInputRef}
              type="file"
              accept=".csv"
              onChange={handleFileChange}
              className="hidden"
            />
          </div>

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
              disabled={!selectedFile}
              loading={uploadMutation.isPending}
              icon={<Upload size={16} />}
            >
              Upload
            </Button>
          </div>

          {/* Upload error */}
          {uploadMutation.isError && (
            <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/30 dark:text-red-400">
              {(uploadMutation.error as Error).message || 'Upload failed'}
            </div>
          )}

          {/* Import result */}
          {importResult && (
            <div className="rounded-lg border border-gray-200 bg-gray-50 p-4 dark:border-gray-700 dark:bg-gray-800/50">
              <h4 className="mb-2 text-sm font-semibold text-gray-900 dark:text-gray-100">
                Import Complete
              </h4>
              <div className="flex flex-wrap gap-3">
                <Badge variant="green">{importResult.importedCount} imported</Badge>
                <Badge variant="yellow">{importResult.skippedCount} skipped</Badge>
                <Badge variant="blue">{importResult.totalCount} total</Badge>
                {importResult.bankType && (
                  <Badge>{importResult.bankType}</Badge>
                )}
              </div>
              {importResult.errors.length > 0 && (
                <div className="mt-3 space-y-1">
                  {importResult.errors.map((err, i) => (
                    <p key={i} className="text-xs text-red-600 dark:text-red-400">
                      {err}
                    </p>
                  ))}
                </div>
              )}
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
