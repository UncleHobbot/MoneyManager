# Architecture cleanup — Candidates 3, 4, 5 (one PR)

Outcome of the `/improve-codebase-architecture` → `/grilling` session (2026-06-25).
The user chose one combined PR. Each candidate was grilled first.

## Candidate 3 — Signed amount: DROPPED (docs only) ✅

The duplication it targeted is already consolidated by `ReportingRow` (ADR-0004);
the only remaining inline is one intentional `OrderBy` in `ApplySort`. A persisted
column would mean the repo's first EF migration + altering the prod DB for one
expression — poor trade-off.

- [x] ADR-0010 records the rejection.
- [x] `CONTEXT.md` "Signed amount" updated (stale "3 duplicates in Chart.cs / Candidate 3").
- [x] `TransactionQueryService.ApplySort` comment updated.

## Candidate 4 — queryKeys factory (frontend)

Factory in `src/lib/queryKeys.ts`; **exact** preservation of current key arrays
(behaviour-neutral); cover only existing keys. Cross-domain invalidations reference
the factory.

- [ ] `src/lib/queryKeys.ts` — domains: transactions (all/list/infinite/stats),
      charts (all + 8 builders), categories (all/tree/icons), accounts (all),
      rules (all/possible), budgets (all), ai (providers/analysisTypes),
      backups, csvArchive.
- [ ] Rewire hooks: `useTransactions`, `useCharts`, `useCategories`, `useAccounts`,
      `useRules`, `useBudgets`, `useAI`, `useSystem`.
- [ ] Rewire pages with inline keys: `ImportPage` (csvArchive + transactions).
- [ ] Verify each rewired key equals the old array (same prefix/segments).
  → verify: `npm run lint`, `npm run build`, `npm test` green.

## Candidate 5 — DbContext lifetime (`await using`)

Add `await using` to every bare `var ctx = await contextFactory.CreateDbContextAsync();`
(~25 sites). No `WithContext` helper, no ADR — mechanical, compiler-enforced disposal.

- [ ] `DataService.Account.cs` (2), `DataService.Category.cs` (5),
      `DataService.Rule.cs` (4), `DataService.Transaction.cs` (3),
      `DataService.AI.cs` (1).
- [ ] `AiProviderService.cs` (6), `DBService.cs` (2), `CategoryEndpoints.cs` (1),
      `Program.cs` (1, startup).
- [ ] Watch for methods that return a value built from `ctx` after the using scope —
      ensure the value is materialized before dispose (most already are).
  → verify: `dotnet test` green; API boots.

## Review

Implemented 2026-06-25. One combined PR.

**C3 (dropped, docs only)** — ADR-0010 records the rejection; `CONTEXT.md` "Signed
amount" + the `ApplySort` comment updated. The duplication was already consolidated
by `ReportingRow`; one intentional inline remains in `ApplySort`.

**C4 (queryKeys factory)** — `src/lib/queryKeys.ts` owns all key shapes (exact
preservation, behaviour-neutral). Rewired `useTransactions`, `useCharts`,
`useCategories`, `useAccounts`, `useRules`, `useBudgets`, `useAI`, `useSystem`, and
`ImportPage`. No raw key arrays remain. `import type` avoids a runtime cycle with
`useTransactions`.

**C5 (`await using`)** — added to 23 of 25 bare context sites (`DataService.*`,
`AiProviderService`, `DBService`, `CategoryEndpoints`, `Program.cs`). The two
remaining — `GetRulesAsync`, `GetTransactionsAsync` — return a context-bound
`IQueryable` that callers enumerate after return; disposing would break them, so they
carry a comment. Materializing them (so they can dispose) is a deferred follow-up
(grilling variant A).

**Verification**
- Backend: `dotnet test` → 236 passed; API boots, CRUD + backup endpoints 200, no
  "disposed" errors.
- Frontend: `npm run lint` clean, `npm run build` ok, `npm test` → 88 passed.

**Follow-up**
- Materialize `GetRulesAsync` / `GetTransactionsAsync` to `IReadOnlyList<T>` and update
  callers, removing the last two context leaks.
