/**
 * Composable ECharts option fragments shared by the Cartesian chart pages
 * (ADR-0006). Pages own `series`; these primitives own the repeated grid, axis,
 * and CAD-tooltip grammar so it is written — and themed — in one place. See
 * CONTEXT.md ("Chart option primitives").
 */
import type { EChartsOption, GridComponentOption, TooltipComponentOption } from 'echarts'
import { formatCAD } from '@/lib/format'
import { chartAxis } from '@/lib/chartTheme'

// ECharts' X/Y axis option types are not exported as a shared base, so a single
// builder result would not drop into both `xAxis` and `yAxis`. Intersect the
// single-axis members: the result is assignable to either slot (horizontal
// charts put the value axis on `xAxis` and the category axis on `yAxis`).
type Single<T> = T extends readonly (infer U)[] ? U : T
type AxisOption = Single<NonNullable<EChartsOption['xAxis']>> & Single<NonNullable<EChartsOption['yAxis']>>

/** Money as a whole-dollar axis/tooltip label, e.g. 1234.5 → "$1,235". */
const cad = (value: number) => formatCAD(value, { fractionDigits: 0 })

/** Grid with sensible margins + containLabel; pass overrides for per-chart spacing. */
export function moneyGrid(overrides?: GridComponentOption): GridComponentOption {
  return { left: 8, right: 16, top: 8, bottom: 8, containLabel: true, ...overrides }
}

/**
 * A themed category (labels) axis. `name` adds a centered axis title;
 * `boundaryGap: false` makes a line/area chart start flush against the axis.
 */
export function categoryAxis(
  isDark: boolean,
  data: (string | number)[],
  opts?: { name?: string; boundaryGap?: boolean },
): AxisOption {
  const axis = chartAxis(isDark)
  return {
    type: 'category',
    data,
    ...(opts?.boundaryGap !== undefined ? { boundaryGap: opts.boundaryGap } : {}),
    axisLabel: { color: axis.label },
    axisLine: { lineStyle: { color: axis.line } },
    ...(opts?.name
      ? { name: opts.name, nameLocation: 'middle', nameGap: 28, nameTextStyle: { color: axis.label } }
      : {}),
  } as unknown as AxisOption
}

/** A themed value axis with whole-dollar labels. `name` adds an axis title. */
export function cadValueAxis(isDark: boolean, opts?: { name?: string }): AxisOption {
  const axis = chartAxis(isDark)
  return {
    type: 'value',
    axisLabel: { color: axis.label, formatter: (value: number | string) => cad(Number(value)) },
    splitLine: { lineStyle: { color: axis.split } },
    ...(opts?.name ? { name: opts.name, nameTextStyle: { color: axis.label } } : {}),
  } as unknown as AxisOption
}

/** An axis-trigger tooltip formatting values as whole dollars (null → "—"). */
export function cadAxisTooltip(opts?: { shadow?: boolean }): TooltipComponentOption {
  return {
    trigger: 'axis',
    ...(opts?.shadow ? { axisPointer: { type: 'shadow' } } : {}),
    valueFormatter: (value) => (value == null ? '—' : cad(Number(value))),
  }
}
