# Extract ReferenceDataCache (deepening Candidate 1)

Outcome of the `/improve-codebase-architecture` → `/grilling` session (2026-06-24).
Design captured in `CONTEXT.md` ("Reference Data Cache"). No ADR (easily reversed;
ADR-0003 already covers the single-adapter stance).

## Goal

Pull the scattered account/category cache (read-through + refresh-on-write + warm,
spread across `DataService.*`) into one deep module with a small interface:
`GetAccounts / GetCategories / InvalidateAccounts / InvalidateCategories / Warm`.
Also fix the bug: import that creates an account/category never invalidates the
UI cache.

## Phase 1 — The module

- [ ] `Services/ReferenceDataCache.cs` — `Singleton`, deps `IDbContextFactory<DataContext>`
      + `IMemoryCache`. Owns cache keys, read-through, `Include(Parent)` for categories.
  - `Task<IReadOnlyCollection<Account>> GetAccountsAsync()`
  - `Task<IReadOnlyCollection<Category>> GetCategoriesAsync()`
  - `void InvalidateAccounts()` / `void InvalidateCategories()` (evict-only)
  - `Task WarmAsync()`
  → verify: builds.

## Phase 2 — Rewire DataService onto the module

- [ ] `DataService` ctor: swap `IMemoryCache cache` → `ReferenceDataCache cache`.
- [ ] Delete from `DataService.cs`: `AccountsCacheKey`/`CategoriesCacheKey`,
      `WarmCacheAsync`, `GetCachedAccountsAsync`, `GetCachedCategoriesAsync`,
      `RefreshAccountsCacheAsync`, `RefreshCategoriesCacheAsync`. Keep `NetIncomeChartPeriod`.
- [ ] `DataService.Account.cs`: `GetAccountsAsync` → `(await cache.GetAccountsAsync()).ToList()`;
      `ChangeAccountAsync` → `cache.InvalidateAccounts(); return (await cache.GetAccountsAsync()).ToList();`;
      `DeleteAccountAsync` → `cache.InvalidateAccounts()`.
- [ ] `DataService.Category.cs`: all `GetCachedCategoriesAsync()` calls →
      `cache.GetCategoriesAsync()`; `ChangeCategoryAsync` → `cache.InvalidateCategories()`.
      → verify: builds; lookups (by id/name/tree) unchanged in behaviour.

## Phase 3 — Warm call sites + DI

- [ ] `Program.cs`: `AddSingleton<ReferenceDataCache>()`; startup warm resolves the
      singleton directly from `app.Services` and calls `WarmAsync()` (no scope needed).
- [ ] `SystemEndpoints.RestoreBackup`: inject `ReferenceDataCache`, call `WarmAsync()`
      instead of `dataService.WarmCacheAsync()`; drop the now-unused `DataService` param.
      → verify: builds.

## Phase 4 — Import invalidation (the bug fix)

- [ ] `TransactionService` ctor: add `ReferenceDataCache cache`.
- [ ] At the end of `ImportAsync` (after save), unconditionally
      `cache.InvalidateAccounts(); cache.InvalidateCategories();`.
      → verify: an import that creates an account refreshes the cache.

## Phase 5 — Tests + verify

- [ ] `DbContextHelper.CreateServiceBundle`: build `ReferenceDataCache` from the
      `MemoryCache`, pass it to `DataService`.
- [ ] `TransactionServicePipelineTests` / `TransactionServiceImportTests`: build a
      `ReferenceDataCache` and pass it to both `DataService` and `TransactionService`.
- [ ] Add a focused test for the module: warm → get returns seeded data; invalidate →
      next get reflects a DB change.
- [ ] `dotnet test` green; spot-check runtime (import creates account → appears in
      `/api/accounts` without restart).

## Review

Implemented 2026-06-24. All phases done.

- `Services/ReferenceDataCache.cs` — Singleton; `GetAccountsAsync` / `GetCategoriesAsync`
  (→ `IReadOnlyCollection`, `Include(Parent)`), `InvalidateAccounts` / `InvalidateCategories`
  (evict-only), `WarmAsync`. Load methods use `await using` (no abandoned context).
- `DataService` now depends on `ReferenceDataCache` instead of `IMemoryCache`; the 5 cache
  methods (`WarmCacheAsync`, `GetCached*`, `Refresh*`) and the key constants are deleted.
  `GetChildren` takes `IReadOnlyCollection<Category>`.
- Writes invalidate by type: account writes → `InvalidateAccounts`; category writes →
  `InvalidateCategories`. `Program.cs` warms the singleton directly (no scope);
  `SystemEndpoints.RestoreBackup` warms via the cache (DataService param dropped).
- **Bug fix:** `TransactionService.ImportAsync` invalidates both sets after save, so an
  imported account/category appears in the UI without waiting for the next warm.
- `CONTEXT.md` "Reference Data Cache" entry added. No ADR (reversible; ADR-0003 covers
  single-adapter).

**Verification**
- `dotnet test` → 234 passed (+4 new `ReferenceDataCacheTests`: cold read-through,
  Parent population, invalidate-reflects-new-row, warm preloads both).
- Runtime: API boots (singleton warm succeeds); `/api/accounts` and `/api/categories`
  return 200 through the module.

**Not done / out of scope**
- Candidate 2 (split DataService into focused services) — now unblocked but separate.
- The wider DbContext-leak (Candidate 5) untouched except inside the new module.
