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
- **EF translation limit.** EF Core 10 cannot translate `AmountExt` inside a
  server-side query expression (`OrderBy`, `Where`, `Select`-to-SQL): the
  expression-bodied property is treated as unmapped. Code that must sort/filter
  on the signed value *in SQL* re-spells the formula inline.
- **Consolidated by ReportingRow.** Chart and stats methods no longer re-spell
  the formula: they consume `ReportingRow.SignedAmount`, computed once
  client-side from `AmountExt` (`GetReportingRowsAsync` materializes, then
  projects in memory). `DataService.Chart.cs` has zero inline copies — the
  `-row.SignedAmount` usages are a "signed → expense magnitude" convention flip,
  not the sign formula.
- **One intentional inline.** The amount-sort branch of
  `TransactionQueryService.ApplySort` re-spells the formula because an `OrderBy`
  must translate to SQL. This is the single remaining copy and is deliberate, not
  debt. A persisted signed-amount column to remove it was considered and
  **rejected** (ADR-0010): the duplication it targeted is already gone, and the
  column would mean the repo's first EF migration + altering the prod database
  for one `OrderBy`.

## Analysis Type (AI prompt catalog)

A read-side catalog of financial-analysis types: each entry binds together
a wire-format key (what the frontend sends), a user-facing label/group/
description (what the dropdown renders), the prompt text (what the AI sees),
and the temperature (creativity setting). Replaces the legacy
`AnalysisTypePrompts` static class, which had two parallel switches over
the same vocabulary plus a parallel `{ type, name, group, description }`
array in `AIEndpoints`.

- **Shape.** `AnalysisType(Key, Name, Group, Description, Prompt, Temperature)`.
  Static `All` list (14 entries grouped by domain: Spending, Debt & Savings,
  Planning, Behavior, Canadian-Specific); `Find(key)` lookup.
- **Single source of truth.** The `/api/ai/analysis-types` endpoint
  projects from `AnalysisType.All` directly (`type`, `name`, `group`,
  `description` exposed; `Prompt` and `Temperature` are internal). The
  `/api/ai/analyze` endpoint validates via `Find`. `AIService.GetAnalysisAsync`
  resolves the prompt and temperature via `Find`. No parallel sources.
- **Temperatures.** Three-tier convention: `0.3` for analyses requiring
  precision (debt, forecasts, anomalies, income, seasonal), `0.5` for
  balanced recommendations (budgets, savings, planning, tax), `0.7` for
  creative exploratory insights (general spending, trends, behavioral,
  subscriptions).
- **Default behavior for unknown keys.** `Find` returns null;
  `AIService.GetAnalysisAsync` treats null as empty prompt + 0.7
  temperature, matching the pre-migration default-branch behavior of
  `AnalysisTypePrompts.GetPrompt` / `GetTemperature`. The `/api/ai/analyze`
  endpoint rejects unknown keys with 400 Bad Request (preserves today's
  validation behavior).
- **System prompt.** Kept as `private const` on `AIService`. Extracting to
  a resource is a separate candidate; the const keeps it co-located with
  the only consumer.

## Bank Import Adapter

A read-side abstraction over per-bank CSV formats. Each bank (Mint, RBC,
CIBC) implements `IBankImporter` with two operations: `Validate(stream)`
checks structure, `ReadRows(stream)` yields `NormalizedRow` values. The
import pipeline (`TransactionService.ImportAsync`) consumes normalized
rows and owns account/category resolution, dedup, rule application, and
persistence. **Backup is no longer the pipeline's concern** — see
"Database backup" below and ADR-0008.

- **Adapter properties.** Each `IBankImporter` declares: `BankType`
  (wire-format identifier), `ApplyRules` (whether the pipeline applies
  auto-categorization), `UseFuzzyDateMatch` (±5 day dedup window),
  `HasHeaderRecord` (line-count adjustment).
- **Adapter is pure.** Adapters do not touch the database, the cache, or
  rule application. They read the stream and produce `NormalizedRow`
  values; the pipeline does the rest.
- **Pipeline is thread-safe.** Each `ImportAsync` call uses method-local
  caches for accounts and categories. The legacy instance state
  (`_accounts`, `_categories`) is deleted.
- **Adding a new bank.** Add one class implementing `IBankImporter`.
  Register its string code in `ImportEndpoints.ParseBankType` (and add a
  detection heuristic to `DetectBankType` if it should be auto-detected).
  The pipeline, helpers, and tests do not change.
- **Endpoint responsibility.** `ImportEndpoints.Upload` owns HTTP receipt,
  bank detection, and file archive (file-system side-effects). The
  pipeline owns everything else **except backup**, which is now the
  caller's concern (see "Database backup").
- **Batch import.** Multiple CSV files are uploaded as a batch: the
  frontend takes one backup, then uploads the files **sequentially** over
  the single-file endpoint (per-file result + error isolation). A failed
  pre-batch backup aborts the batch. Bank type is one batch-wide choice
  (Auto-detect by default, which sniffs each file individually).
- **Archive filename format.** `{yyyy-MM-dd HHmmss} {bankType} {original}.csv`
  with the original filename sanitized. The timestamp + original name keep
  batch uploads of the same bank on the same day unique (the previous
  `{date} {bankType}.csv` form silently overwrote them). Old archive files
  remain readable.

## Database backup

A point-in-time copy of the SQLite database, taken as a rollback point before
a destructive or bulk operation (today: before an import batch) or on demand.

- **Location.** Derived from the database file's directory, not configured.
  Backups live in `{dataDir}/backup`, CSV archives in `{dataDir}/imported`,
  where `dataDir` is the folder of the DB file from the connection string
  (`/app/data` in prod, `../../data` in dev). There is no `BackupPath` /
  `CsvArchivePath` override — the DB directory is the single source of truth
  (ADR-0008).
- **Ownership.** Backup is **caller-orchestrated**, not a side-effect of the
  import pipeline. The frontend takes exactly one backup before a batch via
  `POST /system/backup`; if it fails, the batch is aborted. `DBService` owns
  create/list/restore/cleanup; the import pipeline does not back up.
- **Retention.** Cleanup (keep newest N, default 10) is **manual only** — no
  auto-cleanup runs. Surfaced on the Settings page alongside the backup list
  and restore.

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

- **Shape.** `ReportingRow(Date, SignedAmount, EffectiveCategory?, IsIncome, IsTransfer, Description)`.
  `EffectiveCategory` is `Category?.Parent ?? Category` (rolled up); `null`
  when the transaction has no category. `Description` is the transaction's
  display label, used as the merchant grouping key — see "Merchant / Payee".
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

## Merchant / Payee

The counterparty of a transaction, as shown to the user. There is intentionally
**no `Merchant` entity**: the merchant is the transaction's `Description` field,
and merchant-name normalization is owned by **Rules**.

- **Operational rule.** "Top merchants" and any merchant-level grouping group by
  `Transaction.Description`, not `OriginalDescription`. `OriginalDescription` is
  the raw bank string (e.g. `AMZN*1234`, `AMAZON.CA`) used for dedup and rule
  matching; `Description` is the display label a Rule can rewrite via
  `NewDescription` (e.g. `NETFLIX` → `Netflix Subscription`).
- **Normalization owner.** A `Rule` (pattern → `NewDescription` + category) is the
  canonical "merchant mapping". The Rules page is the merchant-management UI. The
  quality of any merchant view scales with rule coverage; un-ruled rows surface as
  their raw bank string, which is a deliberate nudge to add a rule.
- **No parallel mechanism.** A separate `Merchant`/`Payee` entity was considered
  and rejected — it would duplicate the "pattern → canonical name" idea Rules
  already own. If merchant-level features later outgrow Rules (logos, per-merchant
  limits), introduce the entity then and supersede this entry.
- **Drill-down.** Clicking a merchant routes to the canonical Transactions surface
  filtered by description search (`search=<Description>`), per ADR-0005.

## Transactions drill-in

The single way a chart links to the canonical Transactions surface (ADR-0005).
`transactionsUrl(criteria)` (`lib/transactionsUrl.ts`) owns the URL grammar; the
five chart pages resolve their facets and call it, never hand-build a query string.

- **Criteria.** `TransactionsCriteria { period?, from?, to?, categoryId?, search?,
  uncategorized? }` — a flat object whose fields map 1:1 to the query params
  `TransactionsPage` reads. Emission mirrors the page's own `updateParams`:
  empty/undefined dropped, `uncategorized` only when true (`=1`), `search`
  URL-encoded. A drill link and a user-applied filter therefore produce the same URL.
- **Module is a pure string builder.** No date math, no React, no defaults
  injected (it never adds `period='12'` or strips conflicting keys — the server
  owns `from`/`to`-over-`period` priority). `monthRange(dateISO) → { from, to }`,
  co-located, owns the one piece of real date logic (calendar-month boundaries,
  feeding the Net Income month click).
- **Facet resolution stays on the pages.** Name→id lookup (SpendingByCategory),
  `node.kind`/`categoryId` checks (CashFlow, SpendingTrend) are the page's
  knowledge of its chart data; the module receives a resolved facet.
- **Owner.** `lib/transactionsUrl.ts`. Adding a drill is one `transactionsUrl({…})`
  call; adding a facet means one field here plus the matching read in
  `TransactionsPage`.

## Budget

An opt-in, recurring **monthly spending limit attached to a top-level (parent)
category**. The single forward-looking primitive in an otherwise retrospective
chart suite (see ADR-0007).

- **Shape (v1).** `Budget(Category, Amount)` — exactly one row per parent category,
  interpreted as the same limit every month ("recurring"). Categories without a
  `Budget` row have no limit and are simply absent from budget views.
- **Operational rule.** Budget-vs-actual compares, for a given month, the
  category's `Amount` against the summed expense `ReportingRow`s whose
  `EffectiveCategory` is that parent category. Both sides use the parent-rollup
  grouping, so "actual" matches every other chart.
- **Level.** Parent categories only, to match the `EffectiveCategory` rollup.
  Leaf-level budgets were considered and deferred (ADR-0007).
- **Evolution (Y).** Per-month overrides / budget history are a documented future
  extension: introduce effective-dated rows (`EffectiveFrom`) or an override table,
  where "effective budget for month M" = the latest row with `EffectiveFrom <= M`.
  v1's single recurring row is the special case of that model, so the change is
  additive — do not design v1 in a way that blocks it.
- **Unlocks.** A "Budget vs Actual" chart (per-category actual-vs-limit bars,
  over/under coloured) and a budget **pace** overlay on the cumulative-spending
  chart (expected-by-today vs actual).

## Reference Data Cache

The read-mostly lookup sets that the UI and the import pipeline resolve against:
**Accounts and Categories**. A single deep module (`ReferenceDataCache`) owns their
in-memory storage and lifecycle; the domain lookups built on top of them
(by id, by name, the category tree, `IsNew` filtering) stay in the consuming
services, not in the cache.

- **Interface.** `GetAccounts()` / `GetCategories()` return the cached
  collections; `InvalidateAccounts()` / `InvalidateCategories()` evict by type;
  `Warm()` pre-loads both. That is the whole surface — collections in, eviction
  and warm out.
- **Eviction, not refresh.** Invalidation is **evict-only**; the next read lazily
  reloads via read-through. There is no "refresh-and-return" — a caller that needs
  the fresh collection invalidates, then calls `Get*`.
- **Read-only contract.** `Get*` hands back the live cached collection typed as
  `IReadOnlyCollection<T>` — callers must not mutate it; the defensive `.ToList()`
  copy lives in the consumer (e.g. `GetAccountsAsync`), not the cache.
- **Category invariant.** Categories are always loaded with their `Parent`
  (`Include(c => c.Parent)`), at warm and on lazy load. Consumers rely on
  `category.Parent` being populated. Accounts are flat.
- **Invalidation owners.** Every write to an Account (`ChangeAccountAsync`,
  `DeleteAccountAsync`) invalidates Accounts; every write to a Category
  (`ChangeCategoryAsync` and everything routed through it) invalidates Categories.
  **Import** mutates reference data (it may create Accounts/Categories), so it
  unconditionally invalidates both at the end of a run — fixing the stale-UI-cache
  bug where a newly imported account did not appear until the next warm.
- **Not the import's per-call memoization.** The import pipeline keeps its own
  per-transaction `Dictionary<string, Account/Category>` (see "Bank Import
  Adapter"). That is a different concept: it must see uncommitted inserts within
  the same import and uses richer matching (alternative names, number
  normalization). It does not read this cache; it only invalidates it on completion.
- **Single adapter.** No `IReferenceDataCache` interface — consistent with
  ADR-0003. The seam is the module's own interface; it is a concrete `Singleton`
  wrapping `IMemoryCache`.

## AI Transport / Chat Completion

The seam between AI analysis logic and the network. `AIService` assembles the
request (system persona + per-type user prompt + CSV) and maps the result; the
actual call lives behind `IChatCompletion`, implemented by
`OpenAiCompatibleChatCompletion` (see ADR-0011).

- **Interface.** `IChatCompletion.CompleteAsync(ChatRequest) → ChatResult`.
  `ChatRequest(Endpoint, ApiKey, Model, SystemPrompt, UserPrompt, Data, Temperature)`
  is provider-neutral; `ChatResult(Success, Content, TotalTokens)` carries the
  error text in `Content` on failure.
- **Adapter is pure transport, never throws.** Non-2xx and transport exceptions
  both map to `ChatResult(false, …)`. The `Authorization: Bearer` header is set
  **per request**, not on the shared `HttpClient` — concurrent calls with
  different keys cannot race (the bug the seam fixed).
- **One adapter for all OpenAI-compatible providers.** OpenAI, **DeepSeek**, and
  **Z.AI GLM** share the wire format; they differ only by `AiProvider.ApiUrl` +
  `Model`. Adding one is a new `AiProvider` row, not new code.
- **`ProviderType` is the future discriminator.** A provider needing a genuinely
  different wire format (e.g. Anthropic `x-api-key`) gets its **own**
  `IChatCompletion` adapter selected by `ProviderType` — added only when it
  actually exists, not speculatively (ADR-0003, ADR-0011). Do not build a provider
  factory while every adapter would be identical.
- **Provider resolution stays in `AIService`.** It resolves the `AiProvider`
  (by id or default) and passes `Endpoint/ApiKey/Model` into the adapter; the
  "no provider configured" failure is the service's concern, transport failures
  are the adapter's.
- **Resilience opt-out.** The AI `HttpClient` drops the global
  `StandardResilienceHandler` (10s/30s, retries) for a plain 100s timeout — LLM
  calls run long and a paid, non-idempotent completion must not be retried.
- **Wire types are adapter-internal.** The `OpenAi*` request/response types are
  `internal` to `OpenAiCompatibleChatCompletion`; only the fields actually
  read/written are modelled. `AnalysisResult` stays a domain type in `Model/AI`.

## Categorization

The act of matching a transaction against the auto-categorization **Rules** and,
when exactly one matches, assigning that rule's `NewDescription` and `Category`
and marking `IsRuleApplied`. Owned by a single module, `CategorizationService`.

- **Distinct from Rule CRUD.** Managing the rules themselves (create / edit /
  delete / list) is a separate, shallow concern. `CategorizationService` owns
  *applying* rules to transactions, not editing rules. See "Merchant / Payee"
  (Rules own merchant-name normalization).
- **Matching is the deep core.** `GetMatchingRulesAsync(transaction)` returns
  **all** rules whose pattern matches the transaction's `OriginalDescription`,
  via a two-stage filter: a coarse SQL `LIKE` pre-filter, then a precise
  in-memory pass applying each rule's `CompareType` (Contains / StartsWith /
  EndsWith / Equals, case-insensitive). The same matcher serves "show the user
  the possible rules" and auto-apply.
- **Ambiguity rule.** Auto-application assigns a rule only when **exactly one**
  rule matches. Zero or multiple matches leave the transaction unchanged. This
  rule lives in the apply paths, not in the matcher.
- **Apply seams (three).** `AutoApplyAsync(transaction, ctx)` mutates an
  in-flight transaction inside the caller's unit of work (used by import; no
  save). `ApplyRuleAsync(transactionId, ruleId)` and
  `RecategorizePendingAsync()` own their context and save. `DataContext` appears
  in the interface only on the import-participating path — see ADR-0009.
- **Pending predicate.** `RecategorizePendingAsync` owns the candidate predicate
  `Category == null || !IsRuleApplied` (moved out of the `RuleEndpoints.ApplyAll`
  handler). This is the broader "needs categorization" set, not the "Uncategorized"
  state — do not conflate (see "Uncategorized"). Routing this predicate through
  `TransactionQueryService` is deferred (ADR-0002), not done here.
