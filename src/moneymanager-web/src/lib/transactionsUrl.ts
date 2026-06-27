/**
 * Builds URLs into the canonical Transactions surface (ADR-0005). Every chart
 * drill-in routes here: the page resolves its facets, this module owns the URL
 * grammar — which query params exist, how empty values are dropped, how
 * `uncategorized` is encoded. See CONTEXT.md ("Transactions drill-in").
 */

export interface TransactionsCriteria {
  /** Relative period code (e.g. '12', 'y1'). Omitted when a from/to window is used. */
  period?: string
  /** Inclusive start of an explicit date window, 'YYYY-MM-DD'. Overrides period server-side. */
  from?: string
  /** Exclusive end of an explicit date window, 'YYYY-MM-DD'. */
  to?: string
  /** Filter to a category (and its subtree, applied server-side). */
  categoryId?: number
  /** Free-text description search. */
  search?: string
  /** Restrict to uncategorized transactions. */
  uncategorized?: boolean
}

/**
 * Builds a `/transactions?...` URL from drill-in facets. Empty/undefined values
 * are dropped; `uncategorized` is emitted only when true (as `=1`); `search` is
 * URL-encoded by URLSearchParams. Mirrors TransactionsPage's own updateParams
 * semantics, so a drill link and a user-applied filter produce the same URL.
 */
export function transactionsUrl(criteria: TransactionsCriteria): string {
  const params = new URLSearchParams()
  if (criteria.period) params.set('period', criteria.period)
  if (criteria.from) params.set('from', criteria.from)
  if (criteria.to) params.set('to', criteria.to)
  if (criteria.categoryId !== undefined) params.set('categoryId', String(criteria.categoryId))
  if (criteria.search) params.set('search', criteria.search)
  if (criteria.uncategorized) params.set('uncategorized', '1')

  const qs = params.toString()
  return qs ? `/transactions?${qs}` : '/transactions'
}

/**
 * The calendar-month window for the month containing `dateISO`, as 'YYYY-MM-DD'
 * strings: `from` = first of that month, `to` = first of the next month
 * (exclusive). Feeds a from/to drill-in (e.g. the Net Income month click).
 */
export function monthRange(dateISO: string): { from: string; to: string } {
  const d = new Date(dateISO)
  const pad = (n: number) => String(n).padStart(2, '0')
  const from = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-01`
  const next = new Date(d.getFullYear(), d.getMonth() + 1, 1)
  const to = `${next.getFullYear()}-${pad(next.getMonth() + 1)}-01`
  return { from, to }
}
