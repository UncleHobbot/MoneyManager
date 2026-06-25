---
status: accepted
---

# No persisted signed-amount column; ReportingRow already consolidated the formula

An earlier note (CONTEXT.md "Signed amount", and the architecture review's
"Candidate 3") planned to persist the signed amount (`IsDebit ? -Amount : Amount`)
as a mapped/computed column so EF could `OrderBy`/`Where`/`Sum` it and the formula
would stop being re-spelled across chart and query code. We decided **not** to do
this.

The premise has since changed. The `ReportingRow` migration (ADR-0004) already
removed the chart-side duplicates: every chart and stats method now consumes
`ReportingRow.SignedAmount`, which is computed **once, client-side**, by reusing
the canonical `Transaction.AmountExt` property (`GetReportingRowsAsync`
materializes, then projects in memory). `DataService.Chart.cs` contains zero
inline spellings of the formula — the `-row.SignedAmount` usages are a deliberate
"signed → expense magnitude" convention flip, not a re-spelling of the sign rule.

The only genuine remaining inline copy is the amount-sort branch of
`TransactionQueryService.ApplySort`, which must translate to SQL in an `OrderBy`
and therefore cannot use the expression-bodied `AmountExt`. That is an intrinsic
EF Core 10 translation limit, not duplication debt.

Adding a persisted column to remove that single `OrderBy` expression would mean
introducing the **first EF migration in the repo** and altering the existing
production SQLite database (the schema currently comes from `DataContext` /
`EnsureCreated` and the shipped DB file) — meaningful risk for almost no payoff.

## Consequences

- `Transaction.AmountExt` stays the single canonical source; `ReportingRow`
  reuses it client-side. The one server-side inline in `ApplySort` is intentional
  and documented, not a TODO.
- The "Signed amount" entry in CONTEXT.md and the `ApplySort` comment are updated
  to reflect this; "Candidate 3 / persisted column" is no longer a planned item.
- If amount-sorting ever moves to a hot path where the client-side reuse is
  insufficient, or a true reporting column is introduced for other reasons,
  revisit and supersede.