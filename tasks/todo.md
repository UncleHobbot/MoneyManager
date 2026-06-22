# Plan — TransactionQueryService (Candidate 2)

Deepening outcome of the `/improve-codebase-architecture` grilling loop on
2026-06-21. See also `CONTEXT.md` (Listable Transaction term) and ADRs
0002, 0003.

## Locked decisions

| # | Decision | Choice |
|---|---|---|
| Q1 | Scope | Read-only: `GetAll` + `GetStats`. `ApplyAll` stays separate. |
| Q2 | Return shape | `Page<TransactionDto>` and named `TransactionStats`. |
| Q3 | Interface shape | Two methods on one module; shared `TransactionFilters`. |
| Q4 | Adapter strategy | One adapter (EF Core on `IDbContextFactory`). No `ITransactionQueryStore`. |
| Q5 | Where it lives | New class `TransactionQueryService` in `Services/`. `GetTransactionsAsync()` stays on `DataService`. |
| Q6 | `TransactionFilters` shape | `StartDate`/`EndDate` (not period code); `bool Uncategorized` with same string-match semantics. |
| Q7 | `IsHideFromGraph` rule | Invariant of the module, not an option. Concept "Listable Transaction" in `CONTEXT.md`. |
| Q8 | Migration | Incremental, four commits, TDD inside commit 1. |
| Q9 | Sort by amount | Use `AmountExt` (canonical source). |

## Implementation steps (commit-by-commit)

### Commit 1 — Add `TransactionQueryService` + value objects (TDD red→green)

- [ ] Create value objects in `src/MoneyManager.Api/Model/Query/` (or `Services/`):
  - `TransactionFilters` (record)
  - `TransactionSort` (record) + `SortDirection` (enum)
  - `Paging` (record, generic-use, no `Transaction` prefix)
  - `Page<T>` (record)
  - `TransactionStats` (record)
- [ ] Create `src/MoneyManager.Api/Services/TransactionQueryService.cs`:
  - Constructor injects `IDbContextFactory<DataContext>`
  - `Task<Page<TransactionDto>> GetPageAsync(TransactionFilters, TransactionSort, Paging, CancellationToken = default)`
  - `Task<TransactionStats> GetStatsAsync(TransactionFilters, CancellationToken = default)`
  - Both apply the listable-transaction invariant (`!Account.IsHideFromGraph`)
  - Eager-load `Account`, `Category`, `Category.Parent`
  - `Uncategorized` filter: `Category == null || Category.Name.ToLower() == "uncategorized"`
  - `Search` filter: `EF.Functions.Like` on `Description` and `OriginalDescription`
  - Amount sort uses `AmountExt`
- [ ] Register in `Program.cs`: `services.AddScoped<TransactionQueryService>();`
- [ ] Write `src/MoneyManager.Api.Tests/Services/TransactionQueryServiceTests.cs` (TDD red first):
  - `GetPage_ReturnsEmpty_WhenNoTransactions`
  - `GetPage_ExcludesHiddenAccounts` (Listable Transaction invariant)
  - `GetPage_FiltersByDateWindow`
  - `GetPage_FiltersByAccount`
  - `GetPage_FiltersByCategory`
  - `GetPage_FiltersBySearch_Description` and `_OriginalDescription`
  - `GetPage_FiltersUncategorized` (both `null` and `Name == "Uncategorized"`)
  - `GetPage_SortsByDate_Asc`, `_Desc` (default)
  - `GetPage_SortsByAmount_Asc`, `_Desc` (uses `AmountExt`; verify EF translates — fall back to inline only if test fails with `InvalidOperationException`)
  - `GetPage_SortsByDescription_Asc`, `_Desc`
  - `GetPage_PaginatesCorrectly` (totalCount, page, pageSize, items.Count)
  - `GetPage_StableOrdering` (ThenBy Id)
  - `GetStats_AggregatesIncomeAndExpenses`
  - `GetStats_RespectsFilters`
  - `GetStats_ExcludesHiddenAccounts`
- [ ] Extend `TestHelpers/DbContextHelper.cs` `ServiceBundle` with a `TransactionQueryService` property
- [ ] Verify: `dotnet build src/MoneyManager.Api` and `dotnet test src/MoneyManager.Api.Tests` both pass
- [ ] Verify: existing endpoint tests still pass (no behavior change)

### Commit 2 — Migrate `GetAll` endpoint to use `TransactionQueryService`

- [ ] Update `TransactionEndpoints.GetAll` to accept `TransactionQueryService` (DI)
- [ ] Inside `GetAll`: compute `StartDate`/`EndDate` from `period` via existing `dataService.GetDates(period, ...)`, build `TransactionFilters` + `TransactionSort` + `Paging`, call `GetPageAsync`, wrap result in `TypedResults.Ok(...)`
- [ ] Update existing `TransactionEndpointsTests.GetAll_*` tests if their signatures need the new dependency; otherwise they should pass unchanged
- [ ] Verify: `dotnet test` green

### Commit 3 — Migrate `GetStats` endpoint

- [ ] Same pattern for `TransactionEndpoints.GetStats`
- [ ] Replace anonymous-type return with named `TransactionStats`; keep endpoint's `TypedResults.Ok(stats)` wrapper
- [ ] Update `GetStats_RespectsSearchFilter` test — remove reflection (`GetProperty("count")`), cast to `TransactionStats` directly
- [ ] Verify: `dotnet test` green

### Commit 4 — Cleanup

- [ ] Delete `TransactionEndpoints.ApplyFilters` and `TransactionEndpoints.ApplySort` (now dead)
- [ ] Refactor `GetItems` reflection helper out of `TransactionsControllerTests.cs` if all uses are gone
- [ ] Verify: `dotnet build` shows no unused-private-member warnings, `dotnet test` green, `npm run lint` (frontend untouched, but check)

## Out-of-scope (deferred)

- `RuleEndpoints.ApplyAll` migration to use the new module for finding candidates (per ADR-0002, this is a separate operation)
- `GetById` / `GetPossibleRules` / `Update` migration off `GetTransactionsAsync`
- `TransactionDto.Transaction` self-reference cleanup
- `GetDates` period-code value object (Candidate 1)
- `ReportingRow` with `IsUncategorized` / `IsIncome` / `IsTransfer` flags (Candidate 3) — will replace the string-match in `TransactionQueryService` when it lands
- `ExportCsv` (uses `AIGetTransactionsCSVAsync`, untouched)

## Review checklist (after Commit 4)

- [ ] `TransactionQueryService` interface matches the locked decisions
- [ ] All Listable-Transaction invariants enforced inside the module
- [ ] No reflection-based assertions remain in `TransactionEndpointsTests`
- [ ] CONTEXT.md "Listable Transaction" entry still accurate
- [ ] ADR-0002 and ADR-0003 still describe the shipped design
- [ ] No new `IDbContextFactory` leaks introduced (visual review; tests won't catch)
