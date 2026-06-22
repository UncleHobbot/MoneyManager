import { useEffect, useRef, type CSSProperties } from 'react'
import type { EChartsOption } from 'echarts'
import { echarts } from '@/lib/echarts'
import { chartTheme } from '@/lib/chartTheme'
import { useTheme } from '@/components/layout/useTheme'

/** A click/hover handler bound to an ECharts event (e.g. `click`). */
export type EChartEventHandler = (params: EChartClickParams) => void

/** The subset of ECharts event params chart drill-downs rely on. */
export interface EChartClickParams {
  name: string
  value: unknown
  dataIndex: number
  seriesName?: string
  data?: unknown
}

interface EChartProps {
  option: EChartsOption
  height?: number | string
  className?: string
  /** ECharts event name → handler, e.g. `{ click: (p) => ... }`. */
  onEvents?: Record<string, EChartEventHandler>
}

/**
 * Thin React wrapper over the modular ECharts core instance (ADR-0006). Owns the
 * chart lifecycle (init / setOption / resize / dispose) directly against
 * `echarts/core` — no `echarts-for-react`, which ships CommonJS and breaks under
 * Vite's ESM dev server. Merges the shared `chartTheme` under the caller's option
 * so every chart is themed and light/dark-correct; the caller's option always wins.
 */
export function EChart({ option, height = 320, className, onEvents }: EChartProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<ReturnType<typeof echarts.init> | null>(null)
  const { theme } = useTheme()
  const isDark = theme === 'dark'

  // Init once; keep the instance sized to its container.
  useEffect(() => {
    if (!containerRef.current) return
    const chart = echarts.init(containerRef.current)
    chartRef.current = chart
    // Guard against a queued resize firing after a StrictMode dispose.
    const observer = new ResizeObserver(() => {
      if (!chart.isDisposed()) chart.resize()
    })
    observer.observe(containerRef.current)
    return () => {
      observer.disconnect()
      chart.dispose()
      chartRef.current = null
    }
  }, [])

  // Re-render whenever the option or the theme changes.
  useEffect(() => {
    const chart = chartRef.current
    if (!chart || chart.isDisposed()) return
    const base = chartTheme(isDark)
    chart.setOption(
      {
        ...base,
        ...option,
        textStyle: { ...base.textStyle, ...option.textStyle },
        tooltip: { ...base.tooltip, ...option.tooltip },
        // Only render a legend when the caller asks for one — a legend in the base
        // theme would otherwise force ECharts to show it on every chart, including
        // the donuts that supply their own custom legend list.
        legend: option.legend
          ? { ...(base.legend as object), ...(option.legend as object) }
          : undefined,
        color: option.color ?? base.color,
      },
      true,
    )
  }, [option, isDark])

  // (Re)bind events.
  useEffect(() => {
    const chart = chartRef.current
    if (!chart || !onEvents) return
    const handlers = Object.entries(onEvents)
    for (const [event, handler] of handlers) {
      chart.on(event, handler as (params: unknown) => void)
    }
    return () => {
      for (const [event, handler] of handlers) {
        chart.off(event, handler as (params: unknown) => void)
      }
    }
  }, [onEvents])

  const style: CSSProperties = { height, width: '100%' }
  return <div ref={containerRef} className={className} style={style} />
}
