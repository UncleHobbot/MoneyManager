import { useState, useMemo } from 'react'
import { useAiProviders, useRunAnalysis, useAnalysisTypes } from '@/hooks/useAI'
import { useChartPeriods } from '@/hooks/useCharts'
import { Button, Select, Spinner, Card, Badge } from '@/components/ui'
import type { AnalysisType, AnalysisResult } from '@/types'

/** Groups used for the sidebar sections, in display order. */
const SECTION_ORDER = [
  'Spending Analysis',
  'Debt & Savings',
  'Planning & Forecasting',
  'Behavior & Optimization',
  'Canadian-Specific',
] as const

/**
 * Converts simple markdown-like formatting to HTML.
 * Handles bold, italic, headings, lists, tables, and line breaks.
 */
function markdownToHtml(text: string): string {
  let html = text
    // Escape HTML entities
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')

  // Tables: detect lines with pipes
  const lines = html.split('\n')
  const result: string[] = []
  let inTable = false

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]
    const isTableRow = line.trim().startsWith('|') && line.trim().endsWith('|')
    const isSeparator = /^\|[\s\-:|]+\|$/.test(line.trim())

    if (isTableRow) {
      if (!inTable) {
        result.push('<table class="w-full border-collapse my-3 text-sm">')
        inTable = true
      }
      if (isSeparator) continue
      const cells = line.split('|').filter((_, idx, arr) => idx > 0 && idx < arr.length - 1)
      const tag = !inTable || result.filter(r => r.includes('<tr')).length === 0 ? 'th' : 'td'
      const cellClass = tag === 'th'
        ? 'border border-gray-300 dark:border-gray-600 px-3 py-1.5 bg-gray-50 dark:bg-gray-700 font-semibold text-left'
        : 'border border-gray-300 dark:border-gray-600 px-3 py-1.5'
      result.push(`<tr>${cells.map(c => `<${tag} class="${cellClass}">${c.trim()}</${tag}>`).join('')}</tr>`)
    } else {
      if (inTable) {
        result.push('</table>')
        inTable = false
      }
      result.push(line)
    }
  }
  if (inTable) result.push('</table>')

  html = result.join('\n')

  // Headings
  html = html.replace(/^### (.+)$/gm, '<h4 class="text-base font-semibold mt-4 mb-2">$1</h4>')
  html = html.replace(/^## (.+)$/gm, '<h3 class="text-lg font-semibold mt-5 mb-2">$1</h3>')
  html = html.replace(/^# (.+)$/gm, '<h2 class="text-xl font-bold mt-6 mb-3">$1</h2>')

  // Bold and italic
  html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
  html = html.replace(/\*(.+?)\*/g, '<em>$1</em>')

  // Unordered lists
  html = html.replace(/^[\-\*] (.+)$/gm, '<li class="ml-4 list-disc">$1</li>')
  // Ordered lists
  html = html.replace(/^\d+\. (.+)$/gm, '<li class="ml-4 list-decimal">$1</li>')

  // Wrap consecutive <li> in <ul>/<ol>
  html = html.replace(/((?:<li class="ml-4 list-disc">.+<\/li>\n?)+)/g, '<ul class="my-2">$1</ul>')
  html = html.replace(/((?:<li class="ml-4 list-decimal">.+<\/li>\n?)+)/g, '<ol class="my-2">$1</ol>')

  // Paragraphs: double newline
  html = html.replace(/\n\n+/g, '</p><p class="my-2">')
  // Single newlines to <br>
  html = html.replace(/\n/g, '<br/>')

  return `<p class="my-2">${html}</p>`
}

/** Groups analysis types by their group field. */
function groupAnalysisTypes(types: AnalysisType[]): Map<string, AnalysisType[]> {
  const map = new Map<string, AnalysisType[]>()
  for (const section of SECTION_ORDER) {
    map.set(section, [])
  }
  for (const t of types) {
    const group = map.get(t.group)
    if (group) {
      group.push(t)
    } else {
      // Fallback: place unknown groups at the end
      map.set(t.group, [t])
    }
  }
  return map
}

export default function AIAnalysisPage() {
  const [period, setPeriod] = useState('12')
  const [selectedType, setSelectedType] = useState<string | null>(null)
  const [providerId, setProviderId] = useState<number | undefined>(undefined)
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set())
  const [result, setResult] = useState<AnalysisResult | null>(null)

  const { data: periods, isLoading: periodsLoading } = useChartPeriods()
  const { data: providers, isLoading: providersLoading } = useAiProviders()
  const { data: analysisTypes, isLoading: typesLoading } = useAnalysisTypes()
  const runAnalysis = useRunAnalysis()

  const grouped = useMemo(
    () => groupAnalysisTypes(analysisTypes ?? []),
    [analysisTypes],
  )

  const selectedTypeInfo = useMemo(
    () => analysisTypes?.find(t => t.type === selectedType) ?? null,
    [analysisTypes, selectedType],
  )

  const defaultProvider = providers?.find(p => p.isDefault)

  const effectiveProviderId = providerId ?? defaultProvider?.id

  function toggleSection(section: string) {
    setCollapsedSections(prev => {
      const next = new Set(prev)
      if (next.has(section)) next.delete(section)
      else next.add(section)
      return next
    })
  }

  function handleRun() {
    if (!selectedType) return
    runAnalysis.mutate(
      { period, analysisType: selectedType, providerId: effectiveProviderId },
      { onSuccess: setResult, onError: () => setResult(null) },
    )
  }

  const isLoading = periodsLoading || providersLoading || typesLoading

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="flex h-full gap-6 p-6">
      {/* Left sidebar */}
      <aside className="w-72 shrink-0 space-y-1 overflow-y-auto">
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-3">
          Analysis Types
        </h2>

        {[...grouped.entries()].map(([section, types]) => {
          if (types.length === 0) return null
          const isCollapsed = collapsedSections.has(section)
          return (
            <div key={section} className="mb-1">
              <button
                type="button"
                onClick={() => toggleSection(section)}
                className="flex w-full items-center justify-between rounded-md px-3 py-2 text-sm font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
              >
                <span>{section}</span>
                <svg
                  className={`h-4 w-4 transition-transform ${isCollapsed ? '' : 'rotate-90'}`}
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth={2}
                >
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
                </svg>
              </button>

              {!isCollapsed && (
                <div className="ml-2 mt-0.5 space-y-0.5">
                  {types.map(t => (
                    <button
                      key={t.type}
                      type="button"
                      onClick={() => setSelectedType(t.type)}
                      title={t.description}
                      className={`w-full text-left rounded-md px-3 py-1.5 text-sm transition-colors ${
                        selectedType === t.type
                          ? 'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300 font-medium'
                          : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800'
                      }`}
                    >
                      {t.name}
                    </button>
                  ))}
                </div>
              )}
            </div>
          )
        })}
      </aside>

      {/* Main content */}
      <div className="flex-1 min-w-0 space-y-4">
        {/* Controls bar */}
        <div className="flex flex-wrap items-end gap-4">
          <Select
            label="Period"
            value={period}
            onChange={setPeriod}
            options={(periods ?? []).map(p => ({ label: p.label, value: p.code }))}
          />

          {providers && providers.length > 1 && (
            <Select
              label="AI Provider"
              value={effectiveProviderId?.toString() ?? ''}
              onChange={v => setProviderId(Number(v))}
              options={providers.map(p => ({ label: `${p.name} (${p.model})`, value: p.id }))}
            />
          )}

          <Button
            onClick={handleRun}
            disabled={!selectedType || runAnalysis.isPending}
            loading={runAnalysis.isPending}
          >
            Run Analysis
          </Button>

          {selectedTypeInfo && (
            <p className="text-sm text-gray-500 dark:text-gray-400 self-center">
              {selectedTypeInfo.description}
            </p>
          )}
        </div>

        {/* Error */}
        {runAnalysis.isError && (
          <Card className="border-red-300 dark:border-red-700">
            <div className="flex items-center gap-2 text-red-600 dark:text-red-400">
              <svg className="h-5 w-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span className="text-sm font-medium">
                {runAnalysis.error?.message ?? 'Analysis failed. Please try again.'}
              </span>
            </div>
          </Card>
        )}

        {/* Result with error from API */}
        {result && !result.isSuccess && result.errorMessage && (
          <Card className="border-red-300 dark:border-red-700">
            <div className="flex items-center gap-2 text-red-600 dark:text-red-400">
              <svg className="h-5 w-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span className="text-sm font-medium">{result.errorMessage}</span>
            </div>
          </Card>
        )}

        {/* Loading state */}
        {runAnalysis.isPending && (
          <div className="flex flex-col items-center justify-center py-16 gap-3 text-gray-500 dark:text-gray-400">
            <Spinner size="lg" />
            <p className="text-sm">Running analysis…</p>
          </div>
        )}

        {/* Result content */}
        {result && result.isSuccess && !runAnalysis.isPending && (
          <Card>
            {/* Token / model info */}
            <div className="flex flex-wrap items-center gap-2 mb-4">
              <Badge variant="blue">{result.model}</Badge>
              {result.tokensUsed > 0 && (
                <Badge>{result.tokensUsed.toLocaleString()} tokens</Badge>
              )}
            </div>

            {/* Analysis content */}
            <div
              className="prose prose-sm dark:prose-invert max-w-none text-gray-800 dark:text-gray-200 leading-relaxed"
              dangerouslySetInnerHTML={{ __html: markdownToHtml(result.content) }}
            />
          </Card>
        )}

        {/* Empty state */}
        {!result && !runAnalysis.isPending && !runAnalysis.isError && (
          <div className="flex flex-col items-center justify-center py-24 text-gray-400 dark:text-gray-500">
            <svg className="h-12 w-12 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19 14.5M14.25 3.104c.251.023.501.05.75.082M19 14.5l-2.47 2.47a3.375 3.375 0 01-2.387.989H9.857a3.375 3.375 0 01-2.387-.989L5 14.5m14 0V17a3 3 0 01-3 3H8a3 3 0 01-3-3v-2.5" />
            </svg>
            <p className="text-sm font-medium">Select an analysis type and click Run Analysis</p>
          </div>
        )}
      </div>
    </div>
  )
}
