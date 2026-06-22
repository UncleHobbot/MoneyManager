# PRD: Charts — improvement & expansion

Status: ready-for-agent

Source: `/grill-with-docs` design session, 2026-06-22. Decisions captured in
`CONTEXT.md` (Merchant/Payee, Budget) and ADR-0005, ADR-0006, ADR-0007. Sequenced
build plan in `tasks/todo.md`.

## Problem Statement

As a user of MoneyManager, the charts tell me *what one period looked like* but not
*how I'm trending or whether that's normal for me*. The three charts (Net Income,
Spending by Category, Cumulative Spending) each show a single slice with little
comparison, only the Net Income chart lets me drill into the underlying
transactions, and two of the three render with the wrong colors in light mode. I
can't see how a category is trending over time, where specifically my money goes
(which merchants), how income flows into spending, or whether I'm within a budget.
The result is charts that are pretty-ish but not *informative*, and a catalog that
falls short of a real personal-finance assistant.

## Solution

Make every chart answer "compared to what?" and "what's behind this number?", unify
the look, and broaden the catalog — without inventing data the app doesn't have.

From the user's perspective:

- **Drill anywhere.** Clicking any chart element (a bar, a slice, a trend segment, a
  Sankey edge) opens the full, filtered transaction list for exactly that
  category/period/type — and that filtered view is a shareable link.
- **See trends, not just snapshots.** A new spending-trend chart shows how each
  spending category moves month over month.
- **See where money actually goes.** A new top-merchants chart ranks counterparties.
- **See the whole flow.** A new cash-flow Sankey shows income pooling and flowing out
  to spending and savings.
- **Plan, a little.** Opt-in monthly budgets per category, plus a budget-vs-actual
  chart and a pace indicator on cumulative spending.
- **Consistent, correct visuals.** All charts share one theme and render correctly in
  both light and dark mode.

## User Stories

1. As a user, I want to click a slice/bar/segment on any chart, so that I can see the
   exact transactions behind that number.
2. As a user, I want the drilled-in transaction view to be a normal filtered
   Transactions page, so that I keep all the sorting, paging, and search I already know.
3. As a user, I want a filtered chart-drill view to be a shareable/bookmarkable URL,
   so that I can return to or share a specific slice of my data.
4. As a user, I want the browser back button to return me from a drill-in to where I
   was, so that navigation feels natural.
5. As a user, I want charts to render with correct colors in light mode, so that the
   app doesn't look broken when I'm not in dark mode.
6. As a user, I want all charts to share a consistent visual style, so that the app
   feels coherent.
7. As a user, I want a spending-trend-over-time chart, so that I can see which
   categories are growing or shrinking.
8. As a user, I want the trend chart to roll subcategories up into their parent
   category, so that the picture isn't fragmented.
9. As a user, I want the trend chart to show my top categories plus an "Other"
   bucket, so that it stays readable instead of becoming a hairball.
10. As a user, I want to click a category in the trend chart to isolate it, so that I
    can focus on one category's trajectory.
11. As a user, I want the trend chart to show absolute dollar amounts, so that I can
    reason about real money, not just proportions.
12. As a user, I want to pick the time window for the trend chart, so that I can look
    at the last 12 months, this year, etc.
13. As a user, I want a top-merchants chart, so that I can see which counterparties I
    pay the most.
14. As a user, I want merchant names to reflect my categorization rules' cleaned
    descriptions, so that "AMZN*1234" and "AMAZON.CA" collapse where I've set a rule.
15. As a user, I want un-normalized merchants to show as their raw bank string, so
    that I'm nudged to add a rule to clean them up.
16. As a user, I want to click a merchant to see all transactions for it, so that I
    can review what I bought there.
17. As a user, I want a cash-flow Sankey diagram, so that I can see how my income
    pools and flows out to spending and savings.
18. As a user, I want the Sankey to show a "Savings/Surplus" node when income exceeds
    spending, so that I can see what I kept.
19. As a user, I want the Sankey to clearly indicate a deficit when I spent more than
    I earned, so that I'm not misled by a balanced-looking diagram.
20. As a user, I want to click a Sankey node or flow to drill into those transactions,
    so that I can investigate a flow.
21. As a user, I want to set an opt-in monthly budget for a category, so that I can
    track spending against a plan.
22. As a user, I want to set budgets only on the categories I care about, so that I'm
    not forced to budget everything.
23. As a user, I want a budget-vs-actual chart for the current month, so that I can
    see at a glance which categories are over or under.
24. As a user, I want over-budget categories visually distinguished from under-budget
    ones, so that problems stand out.
25. As a user, I want a pace indicator on cumulative spending, so that I can see
    whether I'm ahead of or behind where I should be partway through the month.
26. As a user, I want the Net Income chart to show my savings rate, so that I can
    judge how much of my income I keep.
27. As a user, I want a rolling-average line on Net Income, so that noisy months don't
    obscure the trend.
28. As a user, I want spending-by-category to show the change versus the previous
    period per category, so that I can see what shifted.
29. As a user, I want to reach each chart from the sidebar, so that I can navigate
    directly.
30. As a user, I want a dashboard mini-card for each chart, so that I get an overview
    and a jump-off point.
31. As a user, I want hidden-account transactions excluded from every chart, so that
    internal transfers don't distort the picture.
32. As a user, I want transfers excluded from spending charts, so that moving my own
    money isn't counted as spending.
33. As a user, I want empty and loading states on every chart, so that I'm never
    staring at a blank or broken panel.
34. As a user, I want charts to keep using Canadian-dollar formatting, so that amounts
    read naturally.

## Implementation Decisions

### Charting engine (ADR-0006)

- Standardize on **Apache ECharts** (`echarts` + `echarts-for-react`) as the single
  charting engine; migrate the existing ApexCharts charts to it and remove
  `apexcharts`/`react-apexcharts`. Use modular ECharts imports to keep the bundle in
  check.
- Introduce one styling source of truth: a `chartTheme(isDark)` option factory + a
  shared palette module + a `<ChartCard>` shell (card + empty + loading), all shaped
  for the ECharts option schema. This replaces per-page re-spelled options and fixes
  the hard-coded dark-mode bug in the donut and cumulative charts.

### Read-side data (reuse the established seam)

- All chart aggregations consume `TransactionQueryService.GetReportingRowsAsync(filters)`
  — the existing read seam that already enforces the **Listable Transaction**
  invariant (hidden accounts excluded) and carries `SignedAmount`, `EffectiveCategory`
  (parent rollup), `IsIncome`, `IsTransfer`. No new aggregation seam per chart.
- Period codes are translated to `StartDate`/`EndDate` at the endpoint boundary via
  the existing `ChartPeriod` value object.

### New charts

- **Spending trend over time:** monthly buckets over a selected `ChartPeriod`,
  expenses only, transfers excluded, grouped by `EffectiveCategory`, top-7 categories
  + an "Other" bucket. Rendered as an absolute-dollar stacked area; clicking a series
  isolates it; clicking a segment drills.
- **Top merchants:** group listable expense transactions by `Transaction.Description`
  (the **Merchant/Payee** convention — no Merchant entity; Rules own normalization).
  Ranked horizontal bars; clicking a bar drills via description search.
- **Cash-flow Sankey:** income `EffectiveCategory` nodes → a single "Total Income" hub
  → expense `EffectiveCategory` nodes + a "Savings" node, over a selected period,
  parent rollup, top-N + "Other" per side. A deficit (expenses > income) is shown as
  an extra source node feeding the hub. Nodes/edges drill.

### Budget (ADR-0007)

- New `Budget` entity: one opt-in row per **parent category** with a single recurring
  monthly `Amount`. New `DbSet<Budget>` (no EF migration files exist; schema derives
  from `DataContext`) and CRUD endpoints under a new `api/budgets` route group.
- **Budget vs Actual** chart: for the current month, compare each budgeted category's
  `Amount` against summed expense `ReportingRow`s for that `EffectiveCategory`;
  over/under is colored. Categories without a budget are absent.
- **Pace overlay** on cumulative spending: expected-spend-by-today (linear from the
  month's total budget) vs actual.
- Per-month budget history and leaf-level budgets are deferred but designed-for: v1
  must not bake in assumptions that block later effective-dated rows.

### Drill-down contract (ADR-0005)

- Every chart element navigates to the canonical **TransactionsPage**, pre-filtered
  via URL query parameters (`period`, `categoryId`, `accountId`, `search`,
  `uncategorized`, `type`). TransactionsPage's filter state moves from local React
  state into URL params so views are deep-linkable and the back button works.
- `MonthDetailPage` is retired; the Net Income month click becomes a TransactionsPage
  link filtered to that month. The `api/charts/month/{month}` endpoint is retired once
  no caller remains.

### Navigation

- Add each new chart to the existing sidebar "Charts" group and add a Dashboard
  mini-card per chart. No separate Reports hub (the Dashboard already serves as the
  overview).

## Testing Decisions

A good test asserts **external behavior at a seam**, not implementation details: it
seeds inputs, exercises the public surface, and checks outputs/observable effects. It
does not reach into private composition, and it does not assert on ECharts canvas
internals.

### Backend — endpoint-handler seam (existing)

- New chart data and Budget CRUD are `internal static` endpoint handlers; test them by
  calling the handler directly against in-memory SQLite seeded via `DbContextHelper`,
  reusing `GetReportingRowsAsync` underneath.
- Prior art: `TransactionQueryServiceTests`, `DataServiceChartTests`, the existing
  `ChartEndpoints`/`TransactionEndpoints` handler tests.
- Representative assertions: trend totals reconcile with spending-by-category for the
  same period; merchant sums match the filtered transaction list; Sankey hub inflow
  equals outflow; budget-vs-actual "actual" matches the same `EffectiveCategory`
  rollup other charts use; hidden-account and transfer exclusion hold; deficit case
  produces the deficit node.
- Budget persistence is the one new backend seam: create/update/delete/list budgets;
  opt-in (absent budget ⇒ category absent from budget views).

### Frontend — pure builders + component behavior (existing seams)

- Extract each chart's data-shaping into **pure series-builder functions** (API DTO →
  ECharts series/option inputs) and unit-test them: top-7 + "Other" rollup, Sankey
  deficit node, pace calculation, CAD formatting. Prior art: `lib/format.test.ts`.
- Test **component behavior** with Testing Library + MSW: period change refetches,
  clicking an element navigates to the expected `/transactions?...` URL, empty and
  loading states render. ECharts is mocked — assert on the series/props passed in, not
  pixels. Prior art: `AccountsPage.test.tsx`, `DashboardPage.test.tsx`.
- **URL-filter contract:** one behavior test that TransactionsPage initializes its
  filters from URL query parameters (the ADR-0005 contract that charts produce and the
  page consumes).

## Out of Scope

- **Net-worth / account-balance-over-time charts.** The `Balance` table is never
  written and `BalanceChart` is a misnamed income/expenses DTO; this needs a separate
  decision about balance ingestion or running-balance derivation.
- **Per-month budget history and leaf-level budgets** (Budget "Y" — deferred,
  designed-for in ADR-0007).
- **Calendar heatmap, sunburst/hierarchy drill, streamgraph** — ECharts can render
  them, but they are not in this scope.
- **A separate Reports/Insights hub page** — revisit only if the catalog outgrows the
  sidebar group.
- **A Merchant/Payee entity** — intentionally not introduced; revisit if merchant-level
  features outgrow Rules.
- **Frontend codegen for `ChartPeriod` codes / a TS union type** — backend already
  exposes the period list; narrowing the type is separate.

## Further Notes

- Build sequencing (see `tasks/todo.md`): Phase 0 foundations (ECharts migration +
  `chartTheme`/`<ChartCard>` + URL-filter refactor + retire `MonthDetailPage`) must
  land before new charts, so nothing is authored on the old engine. Then Phase 1 (new
  retrospective charts), Phase 2 (Budget), Phase 3 (existing-chart enhancements:
  savings rate, period-over-period delta, pace line).
- Phase 3 existing-chart enhancements (stories 26–28) were proposed but not grilled in
  depth; they directly serve the "more informative" goal and can be trimmed without
  affecting earlier phases.
- Respect the documented "known debt": the "Uncategorized" string-match and the inline
  signed-amount formula are intentional until the ReportingRow consolidation finishes;
  do not re-litigate them here.
