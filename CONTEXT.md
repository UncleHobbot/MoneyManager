# CONTEXT — MoneyManager domain glossary

Single source of truth for the project's ubiquitous language. Add a term here
when it crystallizes in a design conversation, when it names a non-obvious
rule, or when another skill needs the vocabulary. Keep entries short — a
definition, an operational rule, and an owner.

## Listable Transaction

A transaction that is eligible to appear in transaction listings and in
read-side aggregations (charts, stats, exports).

- **Operational rule.** A transaction is listable when its `Account.IsHideFromGraph`
  is `false`. There is no opt-in flag on the read path: hidden-account
  transactions are always excluded.
- **Opposite.** A transaction on a hidden account (e.g. inter-account transfers,
  internal bookkeeping) is non-listable. Non-listable transactions still exist
  in the database and may be referenced by id, but no read-side query surface
  returns them.
- **Owner.** `TransactionQueryService` (see ADR-0002) enforces the invariant in
  its `GetPage` / `GetStats` interfaces. The legacy `DataService.GetTransactionsAsync`
  applies the same rule at the source; both must remain consistent.
- **Gotcha.** `Account.IsHideFromGraph` is the only filter that determines
  listability. Category, period, search, and "uncategorized" are query filters,
  not listability rules — they narrow within the listable set.

## Uncategorized (transaction state)

A transaction whose meaningful category is unknown. Encoded today as
`Category == null || Category.Name.ToLower() == "uncategorized"` (the dedicated
"Uncategorized" category exists as a real `Category` row, not as `null`).

- **Why both branches.** In practice almost no transactions have
  `Category == null`; uncategorized ones point at the "Uncategorized" category
  (this is how the dashboard finds them). The `null` branch is defensive.
- **Known debt.** The string-match against `"uncategorized"` is duplicated
  across backend (filters, charts, import) and frontend (dashboard, dialogs).
  Candidate 3 (ReportingRow with `IsUncategorized` flag) is the planned
  consolidation. Until then, all encodings must use the same operational rule.
- **Distinct concept.** "Needs categorization" used by rule re-application
  (`Category == null || !IsRuleApplied`) is broader — it includes transactions
  whose rules may have been edited since the last application. Do not conflate
  with "Uncategorized".

## Signed amount (AmountExt)

The signed value of a transaction: negative for debits (expenses), positive for
credits (income).

- **Formula.** `IsDebit ? -Amount : Amount`. `Amount` is always stored positive.
- **Canonical source.** `Transaction.AmountExt` (expression-bodied property on
  the entity) is the canonical source for any *materialized* use (DTO
  projection, in-memory aggregation, frontend display).
- **EF translation limit.** EF Core 10 cannot translate `AmountExt` inside
  query expressions (`OrderBy`, `Where`, `Select`). The expression-bodied
  property is treated as unmapped. Sort and filter code that must run
  server-side duplicates the formula inline.
- **Known duplicates.** `DataService.Chart.cs` re-spells the formula inline in
  three places (once with the opposite sign convention). The amount-sort branch
  of `TransactionQueryService.ApplySort` duplicates it a fourth time; this is
  the documented fallback from the Q9 grilling decision after the
  `AmountExt`-based test failed with `InvalidOperationException`. Candidate 3
  (ReportingRow with a persisted signed-amount column) is the planned
  consolidation — it removes all four duplicates by introducing a column EF
  can map.

## ChartPeriod (vocabulary)

The set of relative date windows accepted by chart and stats endpoints.

- **Codes.** `m1` (month), `y1` (year), `12` (last 12 months), `w`/`w1`/`w2`/`w3`
  (weeks), `a` (all), plus multi-month variants (`m1+2`, `m1+3`) and year
  variants (`y2`, `y3`, `y12`).
- **Known debt.** The codes are encoded as bare strings in four places:
  `DataService.GetDates` switch, `ChartEndpoints` period list, AI partial, and
  five frontend pages. Candidate 1 (ChartPeriod value object) is the planned
  consolidation. Until then, treat the codes as opaque strings validated only
  by `GetDates`'s default branch.
- **Decoupling.** Read-side modules (e.g. `TransactionQueryService`) accept
  `StartDate` / `EndDate`, not period codes, so their adoption does not depend
  on Candidate 1 landing first.
