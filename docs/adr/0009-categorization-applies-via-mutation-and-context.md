---
status: accepted
---

# Categorization applies via mutation + DataContext, not a pure decision

Rule auto-categorization (matching a transaction against the Rules and assigning
the matched rule's description + category) was extracted from the `DataService`
grab-bag and the `RuleEndpoints.ApplyAll` handler into a single deep module,
`CategorizationService` (the outcome of the `/improve-codebase-architecture`
grilling on Candidate 2). The interface question was how "apply" crosses the seam.

We considered making the module **pure** — `DecideAsync(transaction)` returning a
`CategorizationDecision(NewDescription, CategoryId)` or null — and letting each
caller apply the decision. We rejected it. EF entity tracking is bound to a
`DataContext`: for `transaction.Category` to be persisted (not re-inserted), the
assigned `Category` must be tracked by whatever context saves the transaction. A
pure decision pushes that resolve-and-assign step back onto all three callers
(import, the by-id endpoint, the batch re-categorize), reintroducing the exact
shallow, duplicated "apply" we were removing.

We chose **mutation + context**:

- `GetMatchingRulesAsync(transaction)` — the matcher; returns all matching rules
  (the deep, pure-ish core; the two-stage `LIKE`-then-`CompareType` filter is the
  test surface).
- `AutoApplyAsync(transaction, DataContext ctx)` — mutates an in-flight
  transaction inside the caller's unit of work (import); no save. `DataContext`
  appears here because the method *must* join the import's transaction.
- `ApplyRuleAsync(transactionId, ruleId)` and `RecategorizePendingAsync()` — own
  their context and save; no `DataContext` in their signatures.

So `DataContext` leaks into exactly one method, the one place it is unavoidable,
and only there.

## Considered options

- **Pure `DecideAsync` (rejected).** Elegant matcher, but tracking is
  context-bound, so the category resolve-and-assign duplicates across three
  callers — a shallow leak. Same reasoning as ADR-0003's rejection of a
  pure-function extract whose callers must re-assemble the real behaviour.

- **`DataContext` on every method (rejected).** Uniform but leakier: the by-id and
  batch paths have no reason to expose a context, and every test of those paths
  would have to supply one.

- **Mutation + context only where unavoidable (chosen).** The import path takes a
  context because it genuinely participates in the import's unit of work — already
  the established pattern in this codebase (ADR-0003 normalizes passing
  `DataContext` through the import path). The other two paths stay clean.

## Consequences

- `CategorizationService` owns matching, the three apply paths, and the
  `RecategorizePendingAsync` candidate predicate (`Category == null ||
  !IsRuleApplied`), which moves out of the `RuleEndpoints.ApplyAll` handler.
  Rule CRUD stays separate.
- Consistent with ADR-0002: `ApplyAll` remains a write-side operation; it simply
  moves from the endpoint into a dedicated module instead of into the read-only
  `TransactionQueryService`. Routing the candidate predicate through the query
  module is still deferred, not done here.
- If a future change makes categorization a pure transformation over a
  `ReportingRow` (so tracking is no longer the obstacle), revisit and supersede
  rather than silently reversing.