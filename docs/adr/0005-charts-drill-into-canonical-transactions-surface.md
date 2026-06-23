---
status: accepted
---

# Charts drill into the canonical Transactions surface via URL-driven filters; MonthDetailPage retired

Every chart needs a "see the transactions behind this number" affordance. Today
only the Net Income chart has one: clicking a month navigates to a bespoke
`MonthDetailPage` that re-implements summary cards, two donuts, and a transaction
table. As we add trend-over-time, top-merchants, and cash-flow (Sankey) charts —
each of which wants the same drill-down — we decided where a chart click should
lead.

We chose: **a chart element (segment, bar, Sankey edge) navigates to the existing
`TransactionsPage`, pre-filtered via URL query parameters** (period + category +
type + search). `MonthDetailPage` is retired; its month view becomes
`TransactionsPage` filtered to that month.

This requires lifting `TransactionsPage`'s filter state (currently local React
state) into URL query parameters so filtered views are deep-linkable.

## Considered options

- **Route all drills to a URL-filtered `TransactionsPage` (chosen).**
  `TransactionsPage` already owns the mature, server-side filter machinery
  (period, account, category, search, uncategorized) plus a click-to-filter cell
  pattern. Routing drills here reuses that surface instead of duplicating it,
  yields deep-linkable / shareable filtered views, and makes the browser back
  button work. Cost: refactor the page's filters from local state to URL params —
  worthwhile on its own merits.

- **Keep bespoke per-chart detail pages (status quo, `MonthDetailPage`-style).**
  Rejected. Each new chart would grow its own duplicate list + summary UI, the
  exact "parallel surfaces" the recent read-side consolidation work has been
  removing. `MonthDetailPage` already duplicates `TransactionsPage`'s stats
  footer (income/expenses/net/count) and its category-donut logic.

- **Drill into a modal/drawer with an in-context transaction list.** Rejected as
  the foundation (kept as a possible later "quick peek" enhancement). It preserves
  chart context, but still needs a shared list component and re-spells a slice of
  `TransactionsPage`'s behavior. For a single-user app, navigating to the full
  list with a back button is simpler and good enough.

## Consequences

- `TransactionsPage` filter state moves to URL query parameters
  (`?period=&categoryId=&accountId=&search=&uncategorized=&type=`). The page reads
  initial filters from the URL and writes changes back, so any filtered view is a
  shareable link.
- `MonthDetailPage` and its route are deleted; the Net Income month click becomes
  a link to `TransactionsPage` filtered to that month. The `/api/charts/month/{month}`
  endpoint can be retired once no caller remains (separate cleanup).
- All current and future charts adopt one drill convention: build a
  `TransactionsPage` URL from the clicked element's facets. No chart owns a
  transaction list.
- A future "quick peek" drawer, if added, layers on top of this contract (it reads
  the same facets); it does not replace the canonical surface.
