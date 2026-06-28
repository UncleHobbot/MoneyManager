import { useMemo } from 'react'
import { usePersistedState } from '@/hooks/usePersistedState'
import { useNavigate } from 'react-router-dom'
import { useCashFlow, useChartPeriods } from '@/hooks/useCharts'
import { useTheme } from '@/components/layout/useTheme'
import { Select, Spinner, ChartCard, EChart } from '@/components/ui'
import type { EChartClickParams } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { transactionsUrl } from '@/lib/transactionsUrl'
import { CHART_PALETTE } from '@/lib/chartTheme'
import type { EChartsOption } from 'echarts'

const KIND_COLORS: Record<string, string> = {
  income: '#22C55E',
  hub: '#3B82F6',
  savings: '#10B981',
  deficit: '#EF4444',
  uncategorized: '#9CA3AF',
  other: '#9CA3AF',
}

export default function CashFlowPage() {
  const [period, setPeriod] = usePersistedState('chart-period:cash-flow', '12')
  const navigate = useNavigate()
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  const { data: periods, isLoading: periodsLoading } = useChartPeriods()
  const { data, isLoading } = useCashFlow(period)

  const periodOptions = useMemo(
    () => (periods ?? []).map(p => ({ label: p.label, value: p.code })),
    [periods],
  )

  const nodes = useMemo(() => data?.nodes ?? [], [data])
  const links = useMemo(() => data?.links ?? [], [data])

  const option = useMemo<EChartsOption>(() => {
    let expenseColor = 0
    const seriesData = nodes.map(n => ({
      name: n.name,
      itemStyle: {
        color: KIND_COLORS[n.kind] ?? CHART_PALETTE[expenseColor++ % CHART_PALETTE.length],
      },
    }))

    return {
      tooltip: {
        trigger: 'item',
        formatter: (p) => {
          const d = p as { dataType?: string; name?: string; value?: number; data?: { source?: string; target?: string } }
          if (d.dataType === 'edge') {
            return `${d.data?.source} → ${d.data?.target}<br/><b>${formatCAD(Number(d.value), { fractionDigits: 0 })}</b>`
          }
          return `<b>${d.name ?? ''}</b>`
        },
      },
      series: [
        {
          type: 'sankey',
          data: seriesData,
          links: links.map(l => ({ source: l.source, target: l.target, value: l.value })),
          emphasis: { focus: 'adjacency' },
          nodeWidth: 16,
          nodeGap: 12,
          label: { color: isDark ? '#E5E7EB' : '#374151', fontSize: 12 },
          lineStyle: { color: 'gradient', opacity: 0.4 },
        },
      ],
    }
  }, [nodes, links, isDark])

  // Drill a clicked node into the Transactions surface (ADR-0005): category nodes
  // by category subtree, the uncategorized node by the uncategorized filter. Hub /
  // savings / deficit / "Other" have no single drill target.
  const onEvents = useMemo(
    () => ({
      click: (params: EChartClickParams) => {
        if (params.dataType !== 'node') return
        const node = nodes.find(n => n.name === params.name)
        if (!node) return
        if (node.categoryId != null) {
          navigate(transactionsUrl({ period, categoryId: node.categoryId }))
        } else if (node.kind === 'uncategorized') {
          navigate(transactionsUrl({ period, uncategorized: true }))
        }
      },
    }),
    [nodes, period, navigate],
  )

  if (isLoading || periodsLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner size="lg" />
      </div>
    )
  }

  return (
    <div className="space-y-6 p-6 lg:p-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">
          Cash Flow
        </h1>
        <div className="w-48">
          <Select label="Period" options={periodOptions} value={period} onChange={setPeriod} />
        </div>
      </div>

      <ChartCard isEmpty={nodes.length === 0} height={520}>
        <EChart option={option} height={520} onEvents={onEvents} />
      </ChartCard>
    </div>
  )
}
