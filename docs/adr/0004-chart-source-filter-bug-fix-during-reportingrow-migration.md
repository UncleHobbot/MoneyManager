---
status: accepted
---

# Fix latent ChartGetTransactionsAsync filter bug as part of ReportingRow migration

When `ChartNetIncomeAsync` was migrated off `ChartGetTransactionsAsync` onto
`TransactionQueryService.GetReportingRowsAsync`, we discovered that the
existing chart source filter had a latent NULL-comparison bug that silently
excluded most real-world transactions from charts.

## The bug

`ChartGetTransactionsAsync` filtered its source with:

```csharp
.Where(x => x.Category != null
         && categoryTransfer != null
         && x.Category.Id != categoryTransfer.Id
         && x.Category.Parent.Id != categoryTransfer.Id)
```

EF Core translates `x.Category.Parent.Id` via a LEFT JOIN to the parent
category. When a transaction's category has no parent (any top-level
category — `Income`, `Food`, `Transfer`, `Uncategorized`, ...), the joined
`Parent.Id` is NULL. SQL evaluates `NULL != someId` as `UNKNOWN`, which the
`WHERE` clause treats as false. The row is silently dropped.

The effect: charts only saw transactions whose category had a parent
(i.e. subcategorized transactions). Transactions categorized at the top
level — which in practice is most of them, since `Income`, `Food`, and
`Uncategorized` are all top-level — were excluded. The Income/Expenses
chart showed mostly empty months.

The same file's `ChartNetIncomeAsync` used the *correct* rollup rule
(`(t.Category.Parent ?? t.Category).Id == catIncome.Id`) for its Income
check, but that check ran on the already-filtered set, so it never saw
the rows it was designed to classify.

## Considered options

- **Preserve the bug; add a characterization test that locks in current
  behavior.** Rejected. The chart module had zero test coverage, so there
  was no regression net to "preserve." Locking in the bug would have
  frozen incorrect behavior indefinitely, with no lever to fix it short
  of another deep refactor.

- **Preserve the bug; defer the fix to a future candidate.** Rejected.
  The bug lives in the source filter that the migration is *replacing*.
  Once `ChartNetIncomeAsync` stops calling `ChartGetTransactionsAsync`,
  the bug is gone for that consumer — we have to choose whether the new
  behavior matches the old (buggy) behavior or the intended behavior. It
  cannot "stay" in limbo. Choosing "match the bug" would require
  `GetReportingRowsAsync` to deliberately misclassify top-level categories
  as transfers, which is wrong on its face.

- **Fix the bug as part of the migration (chosen).** The new
  `GetReportingRowsAsync` computes `EffectiveCategory = Category?.Parent ??
  Category` exactly once per row, then derives `IsTransfer` and `IsIncome`
  from the effective category's name. This is the rollup rule the codebase
  already considered canonical (see `ChartEndpoints.cs:59,100`, which both
  use `Category?.Parent ?? Category` for grouping). The migration makes
  the canonical rule the only rule.

## Consequences

- **User-visible behavior change.** Charts that previously showed sparse
  or empty data will now show real Income and Expenses. This is an
  improvement, but it is a change: a dashboard that "looked quiet" may
  suddenly have data. Document this in release notes if/when the change
  ships to production.
- **`ChartGetTransactionsAsync` stays alive.** It still has three callers
  (`ChartCumulativeSpendingAsync`, `ChartGetTransactionsPAsync`,
  `ChartGetTransactionsAsync(month)` via the month convenience overload).
  Each of those still has the bug. They will be migrated in follow-up
  commits using the same `ReportingRow` pattern; the bug disappears from
  each consumer as it migrates. Do not "fix" `ChartGetTransactionsAsync`
  in place — that would change the behavior of the not-yet-migrated
  consumers implicitly.
- **`ChartNetIncomeAsync` no longer calls `ChartGetTransactionsAsync`.**
  Its only dependency on the chart source filter is gone; the
  listability invariant (hidden accounts) is enforced inside
  `TransactionQueryService`, and the Transfer/Income classification is
  enforced via `ReportingRow` flags.
- **Future chart migrations** (`ChartCumulativeSpendingAsync`, etc.) can
  follow this exact pattern: take `TransactionFilters`, call
  `GetReportingRowsAsync`, filter on `IsTransfer`/`IsIncome`, aggregate.
  The shape is now reproducible.
