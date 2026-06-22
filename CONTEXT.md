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

The set of relative date windows accepted by chart, stats, and AI endpoints.
Implemented as a value object (`Model/Query/ChartPeriod.cs`) that owns the
period vocabulary and the per-period date math; the legacy 60-line switch in
`DataService.GetDates` is deleted.

- **Shape.** `ChartPeriod(Code, Label, Func<DateTime, (DateTime Start, DateTime End)> GetDateRange)`.
  Static `All` list (14 periods); `Find(code)` lookup; `Default` fallback
  for unknown codes (equivalent to "y1" / "This Year", preserves the
  pre-migration silent-fallback behavior).
- **Single source of truth.** The `/api/charts/periods` endpoint enumerates
  `ChartPeriod.All` directly. Endpoints (`GetAll`, `GetStats`,
  `GetSpendingByCategory`) and services (`ChartNetIncomeAsync`,
  `AIGetTransactionsAsync`, `AIGetTransactionsCSVAsync`) all resolve codes
  through `ChartPeriod.Find(code) ?? ChartPeriod.Default`.
- **Codes.** `12` (last 12 months), `y1`/`y2`/`y3`/`y12` (year variants),
  `m1`/`m2`/`m1+2`/`m1+3` (month variants), `w`/`w1`/`w2`/`w3` (week
  variants, `w1` is alias for `w`), `a` (all time).
- **Decoupling.** Read-side modules (`TransactionQueryService`,
  `ReportingRow`) accept `StartDate`/`EndDate`, not period codes. The
  translation happens at the endpoint/service boundary via `ChartPeriod`.
- **Known debt.** `ChartPeriod.Default` silently maps unknown codes to
  `y1` to preserve pre-migration behavior. Tightening to throw requires
  auditing frontend usage and is a separate candidate.
- **Frontend not touched.** This candidate is backend-only. Frontend
  pages still send bare string codes; TS codegen for a narrow union type
  is a separate candidate.

## Reporting Row

A read-side projection of a listable transaction, optimized for aggregation.
Produced by `TransactionQueryService.GetReportingRowsAsync`. Carries the
per-row facts that chart and stats methods need without re-spelling the sign
convention, the parent-rollup rule, or the "Income"/"Transfer" name matches.

- **Shape.** `ReportingRow(Date, SignedAmount, EffectiveCategory?, IsIncome, IsTransfer)`.
  `EffectiveCategory` is `Category?.Parent ?? Category` (rolled up); `null`
  when the transaction has no category.
- **Flags are valid regardless of EffectiveCategory.** A row with
  `EffectiveCategory: null` still has `IsIncome == false` and
  `IsTransfer == false` — callers that need a category for grouping should
  filter, callers that only need Income/Expense totals can ignore the field.
- **Canonical sign convention.** `SignedAmount` is positive for income
  (credits), negative for expenses (debits). Same formula as `AmountExt`,
  but carried on the row so consumers (chart methods) stop re-spelling
  `IsDebit ? -Amount : Amount` inline. See "Signed amount" above.
- **Source filter history.** Pre-migration, `ChartGetTransactionsAsync`
  filtered chart input via `Category.Parent.Id != transfer.Id`, which
  silently dropped every transaction whose category had no parent
  (any top-level category — Income, Food, Uncategorized, ...). The
  ReportingRow migration fixes this for all chart consumers; see ADR-0004.
  `ChartGetTransactionsAsync` and its overloads have been deleted; all
  chart and stats methods now route through `GetReportingRowsAsync`.
- **Why it lives on `TransactionQueryService`.** Same listability invariant,
  same `TransactionFilters` interface, same single-adapter strategy as
  `GetPage`/`GetStats` (see ADR-0003). A separate "reporting service" was
  considered and rejected during Candidate 3 grilling.
- **Known debt.** `IsUncategorized` flag is not yet on `ReportingRow`.
  Chart methods don't need it (Uncategorized transactions count as
  expenses by category). When `GetStatsAsync` or `ApplyAll` migrate to
  consume `ReportingRow`, the flag should be added — see Q3 grilling.
