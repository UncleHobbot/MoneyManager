---
status: accepted
---

# Keep TransactionQueryService read-only; rule re-categorization stays in its own operation

When the transaction query module (`TransactionQueryService`) was introduced to
pull filtering, sorting, and pagination out of the HTTP layer, we considered
bundling the rule re-categorization operation (`RuleEndpoints.ApplyAll`, which
opens its own `DataContext`, loads candidate transactions, applies matching
rules, and saves) into the same module. The argument for bundling was
consolidation: `ApplyAll`'s candidate predicate
(`Category == null || !IsRuleApplied`) looks at first glance like a fourth
encoding of the "needs categorization" concept that the query module already
encodes.

We chose to keep them separate. The query module owns read paths only;
`ApplyAll` continues to live as a write-side operation that calls into the
query module for the "find candidates" step but owns its own save.

## Considered options

- **Bundle `ApplyAll` into `TransactionQueryService` as `RecategorizePendingAsync`.**
  Rejected. `GetAll` / `GetStats` are pure queries returning `Page<TransactionDto>`
  / `TransactionStats`. `ApplyAll` mutates entities and commits. Mixing the two
  widens the interface for every caller that only reads (which is all of them
  except one), and forces every fake/test adapter to model save semantics in
  addition to filter semantics. Two distinct seams (read vs. write), two
  distinct test surfaces.

- **Keep them in separate modules, but unify the "needs categorization"
  predicate via a shared expression.** Rejected as premature. The two
  predicates are *not* the same: `ApplyAll`'s is broader
  (`Category == null || !IsRuleApplied` includes transactions whose rules may
  have been edited since last application). Consolidating the *concept* belongs
  to Candidate 3 (ReportingRow with an `IsUncategorized` flag), not to this
  candidate.

- **Keep `ApplyAll` in `RuleEndpoints` with no changes.** Rejected as the
  endpoint still owns business logic (its own `DataContext`, batch loop,
  predicate). The chosen option moves the "find candidates" step onto the
  query module, leaving the endpoint as a thin handler that calls
  `queryService` for the read and `dataService.ApplyRuleAsync` for the write.

## Consequences

- `TransactionQueryService` exposes `GetPage` and `GetStats` only. No
  write-side method.
- `RuleEndpoints.ApplyAll` continues to exist, but its "find candidates"
  predicate is expressed via `TransactionFilters` (or a future
  `NeedsCategorization` filter on it), not via an inline EF expression.
- The fourth encoding of "needs categorization" stays alive until Candidate 3
  (ReportingRow) lands. This is an explicit, time-boxed debt, not an
  oversight.
- If a future deepening makes it clear that `ApplyAll` belongs inside the
  query module (e.g. rule application becomes a pure transformation of a
  `ReportingRow`), supersede this ADR rather than silently reversing it.
