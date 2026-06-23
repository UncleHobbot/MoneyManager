---
status: accepted
---

# Standardize charting on Apache ECharts; migrate off ApexCharts

The chart roadmap is growing beyond what ApexCharts covers. The immediate trigger
is a cash-flow **Sankey** diagram, which ApexCharts cannot render; the broader
roadmap also includes a stacked-area spending trend, and likely a calendar
heatmap and a category-hierarchy (sunburst) view. We decided whether to keep
ApexCharts and bolt on a second library for the gaps, or standardize on a single
engine that covers everything.

We chose to **standardize on Apache ECharts** (`echarts` core + a thin in-repo
React wrapper) as the single charting engine and migrate the existing ApexCharts
charts to it.

> Implementation note: `echarts-for-react` was the obvious wrapper, but its
> `/lib/core` entry ships CommonJS and fails under Vite's ESM dev server
> (`ReferenceError: exports is not defined`). We render through a ~40-line in-repo
> `EChart` wrapper over `echarts/core` instead — fewer deps, no CJS interop, and it
> keeps the modular-import control this ADR calls for.

## Considered options

- **Keep ApexCharts; add `@nivo/sankey` only for the Sankey (rejected).** Lowest
  immediate disruption, but it commits us to **two chart engines with two theming
  systems** indefinitely — and each additional exotic chart (calendar heatmap,
  sunburst, streamgraph) either pushes us toward a third library or a later
  migration anyway. It also fights the just-made decision to consolidate chart
  styling behind one `chartTheme` helper.

- **`react-google-charts` (Google Charts Sankey) (rejected).** Loads an external
  Google script at runtime. Unacceptable for a self-hosted, offline-capable Docker
  app (breaks offline, adds an external request).

- **Standardize on ECharts (chosen).** One Apache-2.0 engine covers every chart on
  the roadmap: line, bar, area/stacked-area, pie/donut, **sankey**, **sunburst**,
  **calendar heatmap**, **themeRiver**, graph. It is modular (import `echarts/core`
  plus only the needed chart/component modules), so the bundle stays comparable to
  ApexCharts despite the larger full package. Its event model (`click` on any
  element, including Sankey edges) suits the ADR-0005 drill-down contract. The
  one-time cost is rewriting the ~6 existing ApexCharts option objects — largely
  absorbed because the `chartTheme` consolidation and the light-mode theming fix
  were already going to touch every one of those sites.

## Consequences

- `apexcharts` / `react-apexcharts` are removed once migration completes; `echarts`
  is added and rendered via the in-repo `EChart` wrapper (no `echarts-for-react`).
  Imports are modular to control bundle size.
- The `chartTheme(isDark)` helper and `<ChartCard>` shell (decided separately)
  **target the ECharts option schema**, not `ApexOptions`. That earlier decision is
  retargeted, not reversed: one styling source of truth, now echarts-shaped.
- The ~6 existing charts (Net Income, Cumulative Spending, Spending-by-Category
  donuts, the three Dashboard sparkline minis, Month-detail donuts) are migrated to
  ECharts. The light-mode theming bug (hard-coded `theme: 'dark'` in the donut and
  cumulative charts) is fixed as part of the migration.
- New charts (spending trend, top merchants, cash-flow Sankey) are authored in
  ECharts from the start — no second engine, no nivo.
- Drill-down uses ECharts click events to build the canonical Transactions URL
  (ADR-0005).
- If a future need arises that ECharts cannot serve, prefer a contained,
  React-native, offline library and document the addition rather than silently
  reintroducing a second general-purpose engine.
