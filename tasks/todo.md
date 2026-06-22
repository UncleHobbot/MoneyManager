# Charts: improvement + expansion plan

Outcome of the `/grill-with-docs` design session on charts (2026-06-22). Decisions
are captured in `CONTEXT.md` (Merchant/Payee, Budget) and ADR-0005/0006/0007. This
file is the sequenced build plan. (Supersedes the completed TransactionQueryService
plan — see git history + ADR-0002/0003.)

## Goal

Make charts more informative (comparison + drill-down everywhere), prettier
(single themed engine), and broaden the catalog toward a real finance assistant —
without inventing data we don't have.

## Decisions (reference)

- **Primary job:** retrospective-first, one scoped forward-looking bet (Budget).
- **New charts, in priority order:** spending trend over time → top merchants →
  cash-flow Sankey. (Calendar heatmap deferred.)
- **Drill-down:** every chart element → canonical `TransactionsPage`, URL-filtered.
  `MonthDetailPage` retired. — ADR-0005
- **Engine:** standardize on Apache ECharts; migrate off ApexCharts. — ADR-0006
- **Styling:** one `chartTheme(isDark)` factory + `<ChartCard>` shell (ECharts-shaped).
- **Merchant:** no entity; group by `Transaction.Description`; Rules own normalization. — CONTEXT
- **Budget:** `Budget(Category=parent, Amount)`, opt-in recurring monthly; per-month
  history (Y) deferred but designed-for. — ADR-0007, CONTEXT
- **Trend form:** stacked area, absolute $, parent rollup, top-7 + "Other", click-isolate.
- **Sankey form:** income categories → "Total Income" hub → expense categories +
  "Savings"; deficit shown as an extra source node; parent rollup, top-N + "Other".
- **Navigation:** flat sidebar "Charts" group + a Dashboard mini-card per chart.

## Phase 0 — Foundations (prerequisites)

- [x] Add `echarts` (modular imports) via in-repo `EChart` wrapper over
  `echarts/core` — `echarts-for-react` rejected (CommonJS, breaks Vite ESM). See
  ADR-0006 note.
- [x] Build `chartTheme(isDark)` factory + shared palette (`CHART_PALETTE`,
  `CHART_COLORS`, `chartAxis`) + `<ChartCard>` (card + empty + loading). Unit-tested
  (`chartTheme.test.ts`). Verified light & dark in browser.
- [ ] Migrate the 6 existing ApexCharts sites to ECharts:
  - [x] Net Income (also fixed a latent net-income sign bug — see below)
  - [x] Cumulative Spending (light-mode bug fixed)
  - [x] Spending-by-Category donuts (light-mode bug fixed; dropped ECharts built-in
    legend in favor of the existing custom legend list)
  - [x] 3 Dashboard minis
  - [x] Month-detail donuts — page retired (replaced by Transactions date-range drill)
- [x] Remove `apexcharts` / `react-apexcharts` (done; main bundle 827KB → 310KB).
- [x] Fix the hard-coded `theme: 'dark'` light-mode bug (done as each donut/cumulative
  chart migrated; Net Income already theme-correct via `chartTheme`).

EChart wrapper notes: `animation: false` in `chartTheme` (snappy + keeps the canvas
idle); `isDisposed()` guards on resize/setOption. Residual "[ECharts] instance has
been disposed" warnings are a dev-only React StrictMode double-mount artifact — they
do not occur in the production build.
- [x] Lift `TransactionsPage` filters into URL query params (deep-linkable);
  retire `MonthDetailPage`; Net Income month click → `/transactions?from&to`;
  Spending slice → `/transactions?categoryId` (subtree). Backend: category-subtree
  filter + `from`/`to` on GetAll/GetStats. Verified in browser (chip, deep-link,
  totals reconcile with slice).

**Phase 0 complete.** Backend 205 tests, web 88 tests green.

**Bug fixed in passing (Net Income migration):** the chart and breakdown table
computed `net = income - expenses`, but `expenses` is a signed sum (debits
negative), so net was double-negated and rendered far above income (e.g. +$13,582
for a month that was actually -$1,688). Corrected to `income + expenses`; verified
against the API's own `balance` field. Pre-existing in the ApexCharts version.

## Phase 1 — New retrospective charts (ECharts)

- [x] **Spending Trend over time** — stacked area, parent rollup, top-7 + "Other",
  monthly buckets, expenses only, transfers excluded, `ChartPeriod` selector
  (default 12mo), legend toggle to isolate, click a segment → drill
  (`/transactions?from&to&categoryId`). Backend `GET /api/charts/spending-trend`
  (+2 tests); sidebar + header nav. (Screenshot pending — preview window was
  collapsed during the run; page mounts clean, no console errors.)
- [x] **Top Merchants** — group by `Description` (top 15); horizontal bars; click bar
  → `/transactions?search=<Description>`. Backend `GET /api/charts/top-merchants`
  (+2 tests); added `Description` to `ReportingRow` (CONTEXT updated). Note: the bar
  groups by exact `Description`; the drill uses `search` (LIKE %…%), so the drilled
  list is a superset of the bar (documented CONTEXT behavior — merchant drill is
  fuzzy, unlike the exact category-subtree drill). Verified: endpoint data correct,
  page mounts clean, search drill filters.
- [x] **Cash-flow Sankey** — ECharts sankey; income→"Total Income" hub→expenses
  (top-8 + "Other") + "Savings"; deficit shown as a source node feeding the hub;
  node drill (category subtree / uncategorized). Backend `GET /api/charts/cash-flow`
  (+1 test asserting the hub balances). Verified in browser (deficit case renders
  correctly, no Savings node).

**Phase 1 complete** (trend, top merchants, cash-flow). Backend 210 tests, web 88 tests green.

## Phase 2 — Forward bet: Budget

- [x] `Budget` entity + `DbSet<Budget>` + `BudgetService` + `/api/budgets` (GET/PUT/
  DELETE). Program.cs creates the table for existing dev DBs. (216 backend tests.)
- [x] Budget management UI — `/budgets` page: per top-level category, set/clear an
  opt-in monthly amount (Income/Transfer/Uncategorized excluded). Verified in browser
  (set → persists + chart updates + Clear button; clear → removed).
- [x] **Budget vs Actual** chart (this month) — grouped horizontal bars (Actual
  colored green/red by over/under, Budget gray), via `ChartBudgetVsActualAsync`
  reusing `ReportingRow` at the parent-rollup level.
- [ ] Budget **pace** overlay on Cumulative Spending (expected-by-today vs actual).

## Phase 3 — Existing-chart enhancements ("more informative") — PROPOSED, confirm

- [ ] Net Income: savings-rate % + rolling-average line; period-over-period.
- [ ] Spending-by-Category: per-category Δ vs previous period; drill on slice.
- [ ] Cumulative: generalize beyond this-vs-last-month; add budget pace line (needs Phase 2).

## Notes / housekeeping spotted (out of scope, not yet decided)

- `BalanceChart` DTO is misnamed (holds income/expenses, not balances). The
  `Balance` table is never written — net-worth-over-time remains blocked until a
  balance-ingestion or running-balance mechanism is decided (separate session).
