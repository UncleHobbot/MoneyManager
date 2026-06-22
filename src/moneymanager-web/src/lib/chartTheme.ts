/**
 * Single source of truth for chart styling (ADR-0006).
 *
 * `chartTheme(isDark)` returns the base ECharts options every chart merges in via
 * the `EChart` wrapper: palette, transparent background, themed text, tooltip, and
 * legend colors. Charts that have axes pull axis colors from `chartAxis(isDark)`.
 * This replaces the per-page re-spelled ApexCharts options and the hard-coded
 * `theme: 'dark'` that broke light mode in the donut and cumulative charts.
 */
import type { EChartsOption } from 'echarts'

/** Categorical palette shared by every multi-series / multi-slice chart. */
export const CHART_PALETTE = [
  '#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6',
  '#EC4899', '#14B8A6', '#F97316', '#6366F1', '#84CC16',
  '#06B6D4', '#D946EF', '#F43F5E', '#22D3EE', '#A3E635',
  '#FB923C', '#818CF8', '#2DD4BF', '#FBBF24', '#C084FC',
]

/** Semantic colors used directly by income/expense/net series. */
export const CHART_COLORS = {
  income: '#22C55E',
  expense: '#EF4444',
  net: '#3B82F6',
}

/** Axis / grid line colors for charts that render Cartesian axes. */
export function chartAxis(isDark: boolean) {
  return {
    label: isDark ? '#9CA3AF' : '#6B7280',
    line: isDark ? '#4B5563' : '#D1D5DB',
    split: isDark ? '#374151' : '#E5E7EB',
  }
}

/** Base ECharts options merged into every chart by the `EChart` wrapper. */
export function chartTheme(isDark: boolean): EChartsOption {
  const text = isDark ? '#D1D5DB' : '#374151'
  const axis = chartAxis(isDark)

  return {
    color: [...CHART_PALETTE],
    backgroundColor: 'transparent',
    // Skip intro/transition animation: snappier for a finance dashboard and keeps
    // the canvas idle (continuous animation blocks screenshots / wastes paint).
    animation: false,
    textStyle: { color: text },
    tooltip: {
      backgroundColor: isDark ? '#1F2937' : '#FFFFFF',
      borderColor: axis.split,
      textStyle: { color: text },
    },
    legend: {
      textStyle: { color: text },
    },
  }
}
