---
status: accepted
---

# TransactionQueryService uses a single adapter (EF Core); no ITransactionQueryStore abstraction

`TransactionQueryService` depends on `IDbContextFactory<DataContext>` directly.
There is no `ITransactionQueryStore` interface, no in-memory fake adapter, no
specification pattern. Tests exercise the module against in-memory SQLite via
the existing `TestDbContextFactory`, which shares one open connection across
contexts.

This is a deliberate rejection of the "interface everywhere" reflex. The
codebase argument for an interface would be testability; the codebase
argument against it is that the module's bugs live in EF translation, not in
filter logic, and a fake adapter bypasses exactly the bug surface that
matters.

## Considered options

- **Introduce `ITransactionQueryStore` with two adapters** (EF Core for
  production, in-memory `List<TransactionDto>` fake for tests). Rejected.
  The real bugs in this module — `EF.Functions.Like` translation, `ToLower()`
  collation, date-window comparisons, signed-amount ordering via `AmountExt`,
  `ThenBy(t => t.Id)` stability — are EF translation bugs. An in-memory fake
  filters a list with LINQ-to-Objects and catches none of them. The fake would
  test "given these rows, does the filter return the right ones", which is
  trivially readable from the implementation and not where the bugs are.

- **Hybrid: depend on `IDbContextFactory` directly, but extract a static
  `FilterExpressions` helper (`Expression<Func<Transaction, bool>>`) and unit
  test it in isolation.** Rejected. This is the "pure-function extract that
  exists only for testability" smell: the bugs hide in how the expressions are
  composed into real queries, not in the expressions themselves. Testing them
  in isolation tests the wrong thing.

- **Single adapter (chosen).** `TransactionQueryService` accepts
  `IDbContextFactory<DataContext>` via constructor injection. Tests use the
  existing `TestDbContextFactory` (`DbContextHelper.cs:155-171`). The seam
  is the module's interface (`GetPage` / `GetStats`), not the persistence
  adapter.

## Consequences

- One adapter does *not* make the seam hypothetical in the Ousterhout sense:
  the module's interface (`GetPage` / `GetStats` returning `Page<TransactionDto>`
  / `TransactionStats`) is itself the test surface, and tests cross it without
  knowing about EF.
- The `IDbContextFactory` lifetime discipline (currently: most services in
  this codebase create contexts and abandon them — see the systemic DbContext
  leak) must be respected inside `TransactionQueryService`. The module opens,
  materializes, and disposes its context within each method call. Tests will
  not catch leaks (the in-memory connection is shared and never closed), so
  this is a code-review invariant, not a test-enforced one.
- ADR-0001 explicitly rejected migrating to Postgres, so cross-database
  portability is not a load-bearing concern. If persistence ever varies
  (Postgres, read-replica, search index), revisit and supersede.
- The pattern set here is local to `TransactionQueryService`. Other services
  (`AIService`, `TransactionService`, `DBService`) continue to take their
  concrete dependencies directly. We are not introducing a project-wide
  "every service has an interface" rule.
