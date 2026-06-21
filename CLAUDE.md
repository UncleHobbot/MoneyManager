# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

## Project Overview

MoneyManager is a personal financial management app (Mint.com replacement): track transactions, manage accounts/categories, auto-categorize via rules, visualize spending, and get AI-powered insights.

**The current product is a web app** — an ASP.NET Core Minimal-API backend (`src/MoneyManager.Api`) plus a React + Vite single-page app (`src/moneymanager-web`). The original .NET 10 / Windows Forms + Blazor Hybrid **desktop** app has been retired to `legacy/`; **do not modify `legacy/`** unless explicitly asked.

## Repository Layout (monorepo)

- `src/MoneyManager.Api/` — ASP.NET Core Minimal API; also serves the built SPA in production
- `src/moneymanager-web/` — React + TypeScript + Vite frontend
- `src/MoneyManager.Api.Tests/` — xUnit backend tests
- `src/e2e/` — end-to-end tests
- `legacy/` — retired Blazor Hybrid / WinForms desktop app (do not modify)
- `docs/`, `Dockerfile`, `docker-compose*.yml`, `MoneyManager.sln`

## Technology Stack

**Backend** (`src/MoneyManager.Api`)
- **.NET 10** with **ASP.NET Core Minimal APIs**
- **Entity Framework Core 10** + **SQLite** (`IDbContextFactory`, context per operation)
- **Serilog** logging
- Tests: **xUnit** + **FluentAssertions** + **NSubstitute**

**Frontend** (`src/moneymanager-web`)
- **React 19** + **TypeScript**, **Vite 8**
- **Tailwind CSS v4**
- **TanStack Query v5** (server state), **React Router v7**
- **Axios** (`src/api/client.ts`, `baseURL: '/api'`)
- **ApexCharts**, **lucide-react** icons
- Tests: **Vitest** + **Testing Library** (+ **MSW**)

**AI** — OpenAI-compatible chat completions via `AIService`; provider config (base URL / model / key) stored as `AiProvider` entities, so the provider is pluggable.

## Architecture

```
React SPA (Vite dev server :5173, proxies /api → :5000)
        ↓  HTTP /api
ASP.NET Core Minimal API endpoints (src/MoneyManager.Api/Endpoints/*.cs)
        ↓
DataService / TransactionService / AIService (DI)
        ↓
EF Core (DataContext via IDbContextFactory)
        ↓
SQLite
```

In **production** the API also serves the built SPA from `wwwroot` (`UseDefaultFiles` + `UseStaticFiles` + `MapFallback` to `index.html`). **CORS** is enabled for local dev where the SPA runs on a separate port.

## Backend Layout (`src/MoneyManager.Api`)

- `Endpoints/` — Minimal-API route groups, one static class per resource: `TransactionEndpoints`, `AccountEndpoints`, `CategoryEndpoints`, `RuleEndpoints`, `ChartEndpoints`, `ImportEndpoints`, `AIEndpoints`, `SystemEndpoints`. Each maps routes under `api/<resource>` and holds the `internal static` handlers (unit-tested directly).
- `Services/`
  - `DataService` (partial): `.cs` (core + `IMemoryCache` caching), `.Account`, `.Transaction`, `.Category`, `.Rule`, `.Chart`, `.AI`
  - `TransactionService` (partial): `.cs` (duplicate detection) + `.Mint`, `.RBC`, `.CIBC` bank CSV imports
  - `AIService`, `AiProviderService`, `DBService` (backup/restore), `SettingsService`
- `Data/` — EF entities + `DataContext`: `Transaction`, `Account`, `Category`, `Rule`, `Balance`, `AiProvider`
- `Model/` — request/response DTOs (e.g. `Model/Api/TransactionRequest.cs` → `CreateTransactionRequest`, `UpdateTransactionRequest`)
- `Helpers/`, `Migrations/`, `Program.cs`, `appsettings*.json`, `wwwroot/` (built SPA)

## Frontend Layout (`src/moneymanager-web/src`)

- `pages/` — one component per route (`DashboardPage`, `TransactionsPage`, `AccountsPage`, `CategoriesPage`, `RulesPage`, `ImportPage`, `SettingsPage`, `AIAnalysisPage`, chart pages)
- `components/` — shared components; `components/ui/` is the design system (`Button`, `Input`, `Select`, `Dialog`, `DataTable`, `Badge`, `Card`, `Spinner`, `CategoryIcon`, …); `components/layout/` is the nav/shell
- `hooks/` — one TanStack Query hook module per resource (`useTransactions`, `useAccounts`, `useCategories`, `useCharts`, …) plus helpers (`useDebouncedValue`)
- `api/client.ts` — axios instance, `baseURL: '/api'`
- `types/` — shared TS types/DTOs; `test/` — Vitest setup; tests live in `__tests__/` next to the code

## Coding Patterns

**Backend**
1. Endpoints are static `Map*Endpoints` extension classes; handlers are `internal static` so tests call them directly.
2. `DataService` / `TransactionService` are partial classes split by concern.
3. `IDbContextFactory<DataContext>` → a fresh context per operation (thread safety). Accounts/Categories cached via `IMemoryCache`.
4. Entities expose `ToDto()`; endpoints return DTOs, never raw entities.
5. Amounts are stored **positive**; the signed value is `AmountExt => IsDebit ? -Amount : Amount`.

**Frontend**
1. Server state via TanStack Query hooks; mutations invalidate keys like `['transactions']`. Use `placeholderData: keepPreviousData` to avoid list flicker on refetch.
2. Filtering/sorting/paging are **server-side** (query params). `DataTable` supports controlled sort via `sortKey`/`onSortChange`.
3. Build UI from the `components/ui` primitives + Tailwind utilities; match existing component style.

## Database

Tables: **Accounts, Categories, Transactions, Rules, Balances, AiProviders**

Relationships:
- Transactions → Accounts (n:1)
- Transactions → Categories (n:1, nullable)
- Categories → Categories (self-referencing hierarchy)
- Rules → Categories (n:1)

- DB file (dev): `src/MoneyManager.Api/Data/MoneyManager.db`. Docker/prod: `/app/data/MoneyManager.db` (see `appsettings.json` vs `appsettings.Development.json`).
- **No EF migration files currently exist** — the schema comes from `DataContext` / the shipped SQLite file. If you introduce migrations, target the API project.
- Backups (`DBService`): default dir is `{AppContext.BaseDirectory}/backups`, i.e. **inside `bin/.../backups` in dev** (`BackupPath` is unset). Gotcha: backups live in build output, and in Docker land in `/app/backups` rather than the mounted `/app/data` volume. Files: `MoneyManagerBackup_yyyyMMddHHmmss.db`; newest 10 kept.

## Domain Gotchas (learned)

- **"Uncategorized" is a real category named "Uncategorized", not a `null` category.** In practice ~no transactions have `Category == null`; uncategorized ones point at the "Uncategorized" category (this is how the dashboard finds them). Any "uncategorized only" filter must match `Category == null || Category.Name == "Uncategorized"`.
- **Transactions cannot be deleted.** There is intentionally no delete endpoint / UI / service method — they are real historical data.
- Accounts with `IsHideFromGraph == true` are excluded from transaction listings and charts. A transaction added to a hidden account won't appear in the grid.
- Chart period codes: `m1` (month), `y1` (year), `12` (last 12 months), `w` (7 days), `a` (all).
- The Transfer category is filtered out of spending charts. AI prompts use Canadian context (RRSP, TFSA, FHSA).

## Build, Test & Run

Both halves must run for the app to work locally.

**Backend** (`src/MoneyManager.Api`)
```bash
dotnet build
dotnet test src/MoneyManager.Api.Tests
dotnet run --project src/MoneyManager.Api --urls http://localhost:5000
```

**Frontend** (`src/moneymanager-web`)
```bash
npm install
npm run dev      # Vite dev server on :5173, proxies /api → :5000
npm run build    # tsc -b && vite build
npm run lint     # eslint
npm test         # vitest
```

After any change, build + test the affected side (and `npm run lint` for the web) before considering it done. If the API is already running, stop it before rebuilding — a locked `MoneyManager.Api.exe` fails the build.

**Gotcha:** the test project pins EF Core packages to match the API (e.g. `Microsoft.EntityFrameworkCore.Sqlite` `10.0.6`). Keep them aligned or `dotnet test` fails with an NU1605 "package downgrade" error.

## Common Tasks

### Add an API endpoint
1. Add/extend a `*Endpoints.cs` static class and map the route in `Map*Endpoints`.
2. Put logic in the matching `DataService.*` / service; return `ToDto()`.
3. Add an xUnit test in `MoneyManager.Api.Tests` (call the handler directly).

### Add a web page
1. Create `src/moneymanager-web/src/pages/Foo.tsx` and register the route in `App.tsx`.
2. Add navigation in `components/layout`.
3. Add a TanStack Query hook in `hooks/`.

### Add a bank import format
Create `TransactionService.{Bank}.cs` as a partial class following the Mint / RBC / CIBC pattern.

### Add a category icon
Extend `CategoryIconEnum` in `Category.cs` + the `CategoryHelper` icon switch (backend) and the `CategoryIcon` component (frontend).

## Recommended Claude Code Skills

Most useful for working on MoneyManager (all available globally — nothing is vendored into this repo):

- **webapp-testing** — Playwright-drive the React SPA (`src/moneymanager-web`) to verify UI behavior, capture screenshots, and debug the frontend.
- **test-driven-development** / **diagnose** — red-green-refactor and disciplined bug/perf diagnosis loops; apply to both the xUnit and Vitest suites.
- **vercel-react-best-practices** — React performance guidance (note: Next.js-flavored; this app is Vite + React Router, so the Next-specific rules don't apply).
- **pdf** / **pdf-to-markdown** — extract transactions from bank/credit-card PDF statements to extend the CSV importers (`TransactionService.{Mint,RBC,CIBC}.cs`).
- **xlsx** — read/produce spreadsheet financial reports beyond the CSV export.
- **frontend-design** — polished Tailwind/React UI for new pages and components.
- **claude-api** — reference for the AI-insights feature (`AIService.cs`) when adding or migrating providers.

## Agent skills

Configuration for Matt Pocock's engineering skills (`to-issues`, `triage`, `to-prd`, `diagnosing-bugs`, `tdd`, …). Run `/setup-matt-pocock-skills` to change any of these.

### Issue tracker

Issues and PRDs live as local markdown under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five-role vocabulary (`needs-triage` / `needs-info` / `ready-for-agent` / `ready-for-human` / `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context — one `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
