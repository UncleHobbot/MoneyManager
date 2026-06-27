import { useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTopMerchants, useChartPeriods } from '@/hooks/useCharts'
import { useTheme } from '@/components/layout/useTheme'
import { Select, Spinner, ChartCard, EChart } from '@/components/ui'
import { formatCAD } from '@/lib/format'
import { transactionsUrl } from '@/lib/transactionsUrl'
import { CHART_PALETTE, chartAxis } from '@/lib/chartTheme'
import type { EChartsOption } from 'echarts'

export default function TopMerchantsPage() {
  const [period, setPeriod] = useState('12')
  const navigate = useNavigate()
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  const { data: periods, isLoading: periodsLoading } = useChartPeriods()
  const { data, isLoading } = useTopMerchants(period)

  const periodOptions = useMemo(
    () => (periods ?? []).map(p => ({ label: p.label, value: p.code })),
    [periods],
  )

  // API returns largest first; reverse so the biggest bar sits at the top.
  const ordered = useMemo(() => [...(data ?? [])].reverse(), [data])

  const option = useMemo<EChartsOption>(() => {
    const axis = chartAxis(isDark)
    return {
      grid: { left: 8, right: 24, top: 8, bottom: 8, containLabel: true },
      tooltip: {
        trigger: 'item',
        formatter: (params) => {
          const { dataIndex } = params as { dataIndex: number }
          const m = ordered[dataIndex]
          return m
            ? `${m.name}<br/><b>${formatCAD(m.amount, { fractionDigits: 0 })}</b> · ${m.count} txn${m.count === 1 ? '' : 's'}`
            : ''
        },
      },
      xAxis: {
        type: 'value',
        axisLabel: { color: axis.label, formatter: (v: number) => formatCAD(v, { fractionDigits: 0 }) },
        splitLine: { lineStyle: { color: axis.split } },
      },
      yAxis: {
        type: 'category',
        data: ordered.map(m => m.name),
        axisLabel: { color: axis.label },
        axisLine: { lineStyle: { color: axis.line } },
      },
      series: [
        {
          type: 'bar',
          data: ordered.map(m => m.amount),
          itemStyle: { color: CHART_PALETTE[0], borderRadius: [0, 3, 3, 0] },
          barMaxWidth: 22,
        },
      ],
    }
  }, [ordered, isDark])

  // Drill into the canonical Transactions surface, filtered by merchant name via
  // description search (CONTEXT.md "Merchant / Payee" / ADR-0005).
  const onEvents = useMemo(
    () => ({
      click: (params: { name: string }) =>
        navigate(transactionsUrl({ period, search: params.name })),
    }),
    [navigate, period],
  )

  if (isLoading || periodsLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner size="lg" />
      </div>
    )
  }

  const height = Math.max(280, ordered.length * 30 + 24)

  return (
    <div className="space-y-6 p-6 lg:p-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">
          Top Merchants
        </h1>
        <div className="w-48">
          <Select label="Period" options={periodOptions} value={period} onChange={setPeriod} />
        </div>
      </div>

      <ChartCard isEmpty={ordered.length === 0} height={height}>
        <EChart option={option} height={height} onEvents={onEvents} />
      </ChartCard>
    </div>
  )
}
