/**
 * Modular ECharts core instance.
 *
 * ADR-0006 standardizes charting on Apache ECharts and calls for modular imports
 * to keep the bundle in check. This module registers only the chart types and
 * components the app actually uses and re-exports the configured core instance.
 * Every chart wrapper (`EChart`) renders through this instance, so adding a new
 * chart type means registering it here once.
 */
import * as echarts from 'echarts/core'
import { BarChart, LineChart, PieChart, SankeyChart } from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
  DatasetComponent,
} from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'

echarts.use([
  BarChart,
  LineChart,
  PieChart,
  SankeyChart,
  TitleComponent,
  TooltipComponent,
  GridComponent,
  LegendComponent,
  DatasetComponent,
  CanvasRenderer,
])

export { echarts }
