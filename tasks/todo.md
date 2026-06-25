# Extract CategorizationService (deepening Candidate 2)

Outcome of the `/improve-codebase-architecture` → `/grilling` session (2026-06-24).
Design in `CONTEXT.md` ("Categorization") and ADR-0009. The aim is the one genuinely
deep module hiding in the `DataService` grab-bag — rule matching + application — not a
mechanical CRUD split.

## Interface (chosen: mutation + context, ADR-0009)

```
Task<IReadOnlyList<Rule>> GetMatchingRulesAsync(Transaction tx)     // matcher (test surface)
Task AutoApplyAsync(Transaction tx, DataContext ctx)               // import: exactly-one → apply, no save
Task<Transaction?> ApplyRuleAsync(int transactionId, int ruleId)  // owns ctx + save
Task<int> RecategorizePendingAsync()                              // owns ctx + save; absorbs ApplyAll
```

## Phase 1 — The module

- [ ] `Services/CategorizationService.cs` — dep `IDbContextFactory<DataContext>` only.
  - `GetMatchingRulesAsync` — move the two-stage `LIKE` → `CompareType` filter from
    `DataService.GetPossibleRulesAsync`; return `IReadOnlyList<Rule>`.
  - `AutoApplyAsync(tx, ctx)` — match; if exactly one, set Description/Category
    (resolved in `ctx`)/IsRuleApplied; no save. (From `ApplyRuleAsync(tx, ctx)`.)
  - `ApplyRuleAsync(txId, ruleId)` — load tx + rule, apply, save. (Absorbs
    `TransactionEndpoints.ApplyRule`'s load/apply/save.)
  - `RecategorizePendingAsync` — load candidates (`Category == null || !IsRuleApplied`),
    loop `AutoApply` logic, save once, return count. (Absorbs `RuleEndpoints.ApplyAll`.)
  → verify: builds.

## Phase 2 — Trim DataService.Rule.cs to CRUD

- [ ] Remove `GetPossibleRulesAsync`, `ApplyRuleAsync(tx, rule)`, `ApplyRuleAsync(tx, ctx)`.
- [ ] Keep `GetRulesAsync`, `SaveNewRuleAsync`, `ChangeRuleAsync`, `DeleteRuleAsync`.
  → verify: builds.

## Phase 3 — Rewire consumers + DI

- [ ] `Program.cs`: `AddScoped<CategorizationService>()`.
- [ ] `RuleEndpoints.ApplyAll`: inject `CategorizationService`, body becomes
      `return Ok(new { applied = await categorization.RecategorizePendingAsync() })`;
      drop the `IDbContextFactory` param + the inline candidate query/loop.
- [ ] `TransactionEndpoints.GetPossibleRules`: load tx, then
      `categorization.GetMatchingRulesAsync(tx)`.
- [ ] `TransactionEndpoints.ApplyRule`: `categorization.ApplyRuleAsync(id, ruleId)`;
      404 when it returns null; return the updated DTO.
- [ ] `TransactionService`: drop `DataService` dep, add `CategorizationService`;
      `ImportAsync` calls `categorization.AutoApplyAsync(tx, ctx)`.
  → verify: builds; endpoints thin.

## Phase 4 — Tests

- [ ] `ServiceBundle`: construct + expose `CategorizationService`.
- [ ] Move the 4 `GetPossibleRulesAsync` matching tests + the apply test out of
      `DataServiceRuleTests` into `CategorizationServiceTests`, retargeted at the module
      (`GetMatchingRulesAsync`; `ApplyRuleAsync(txId, ruleId)` with a persisted rule).
      Keep Rule CRUD tests on `DataService`.
- [ ] Add a `RecategorizePendingAsync` test (pending tx + matching rule → applied, count 1).
- [ ] Import test construction: `TransactionService` now takes `CategorizationService`.
  → verify: `dotnet test` green; spot-check apply-all + import auto-categorize at runtime.

## Review

Implemented 2026-06-24. All phases done.

- `Services/CategorizationService.cs` — `GetMatchingRulesAsync` (two-stage matcher,
  `CompareType` switch extracted to a private `Matches`), `AutoApplyAsync(tx, ctx)`
  (import, no save), `ApplyRuleAsync(txId, ruleId)` (owns ctx + save, re-fetches with
  includes for the DTO), `RecategorizePendingAsync` (owns ctx + save; absorbs the
  ApplyAll candidate predicate + loop + count). Dep: `IDbContextFactory` only.
- `DataService.Rule.cs` trimmed to CRUD (`GetRules`/`SaveNewRule`/`ChangeRule`/`DeleteRule`).
- Consumers rewired: `RuleEndpoints.ApplyAll` is a one-liner; `TransactionEndpoints`
  `GetPossibleRules`/`ApplyRule` call the module; `TransactionService` dropped its
  `DataService` dep for `CategorizationService` and `ImportAsync` calls `AutoApplyAsync`.
  `Program.cs` registers it scoped.
- `CONTEXT.md` "Categorization" entry + ADR-0009 (mutation+ctx over pure decision).

**Verification**
- `dotnet test` → 236 passed. Matching/apply tests moved to `CategorizationServiceTests`
  (+ `RecategorizePending` + `ApplyRule` 404 cases); Rule CRUD tests stay on DataService;
  endpoint-handler tests retargeted at the module.
- Runtime: API boots (CategorizationService resolves); `/api/rules` 200,
  `/api/transactions/{id}/possible-rules` (module read path) 200.

**Not done / out of scope**
- Routing the `RecategorizePending` candidate predicate through `TransactionQueryService`
  (ADR-0002 direction) — deferred.
- The optional cheap peel (AccountService/CategoryService) — not done; DataService keeps
  account/category/transaction/chart/AI. The genuinely deep module (Categorization) is out.
