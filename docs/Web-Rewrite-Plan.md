# MoneyManager Web Rewrite — Implementation Plan

## Problem Statement

Rewrite the existing WinForms/Blazor Hybrid desktop app as a web application deployable as a Docker container on a Synology NAS (x86_64). Backend: ASP.NET 10 Web API. Frontend: React (Vite + TypeScript). No authentication (internal-only). Reuse ~85% of existing service logic.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Repo | Same repo, new `src/` folder | Keep Blazor app running during migration |
| React | Vite + React + TypeScript | Fast builds, modern tooling |
| UI library | shadcn/ui + Tailwind CSS | Modern, dark mode native, no runtime dep |
| Charts | react-apexcharts | Matches current Blazor ApexCharts for easy migration |
| Container | Single (API serves React static) | Simpler for NAS deployment |
| NAS arch | x86_64 | Standard dotnet base image |
| AI keys | SQLite (encrypted) | Single source of truth |
| Database | **Reuse existing `Data/MoneyManager.db` as-is** | Preserves ALL existing data |
| Bilingual AI | Keep (EN + RU) | Existing behavior |

## ⚠️ CRITICAL: Database Preservation Strategy

**The existing `Data/MoneyManager.db` contains all financial data and MUST be preserved.**

### How existing data is preserved:

1. **Same schema, same file:** The new API connects to the **exact same SQLite file** (`Data/MoneyManager.db`). No data migration, no export/import — it reads the same tables the Blazor app writes to.

2. **EF Core migration baseline:** Instead of creating a fresh `InitialCreate` migration, we use `EnsureCreated()` or a **baseline migration** that describes the existing schema without executing DDL. This means:
   - Existing tables (Accounts, Categories, Transactions, Rules, Balances) are untouched
   - New tables (AiProviders, etc.) are added via incremental migrations
   - `__EFMigrationsHistory` table is seeded with a marker indicating the baseline is already applied

3. **Docker volume mount:** The SQLite DB file lives on a Docker volume (`/app/data`), so:
   - Container updates/rebuilds do NOT delete the database
   - The volume persists across container restarts
   - Backup before any migration: `cp MoneyManager.db MoneyManager.db.bak`

4. **Development workflow:**
   - Copy `Data/MoneyManager.db` to `src/MoneyManager.Api/Data/` for local dev
   - `.gitignore` the copy (never commit the live DB)
   - API `appsettings.Development.json` points to this local copy
   - API `appsettings.json` (production/Docker) points to `/app/data/MoneyManager.db`

5. **Pre-deployment safety:**
   - Before first Docker deployment: auto-backup via DBService
   - Migration script reviewed manually before applying to production DB
   - `dotnet ef migrations script` generates SQL for review
   - Rollback plan: restore from backup

### New tables added (incremental migration — NOT destructive):
```sql
CREATE TABLE AiProviders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    ProviderType TEXT NOT NULL,        -- "OpenAI", "ZAI", etc.
    ApiKey TEXT NOT NULL,              -- encrypted
    ApiUrl TEXT NOT NULL,
    Model TEXT NOT NULL,
    IsDefault INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```
All existing tables remain unchanged. No columns dropped, no types changed, no data modified.

## Folder Structure

```
MoneyManager/
├── src/
│   ├── MoneyManager.Api/           # ASP.NET 10 Web API
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Data/
│   │   ├── Model/
│   │   ├── Helpers/
│   │   ├── Migrations/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Dockerfile
│   │   └── MoneyManager.Api.csproj
│   │
│   └── moneymanager-web/           # React + Vite + TS
│       ├── src/
│       │   ├── components/
│       │   │   ├── ui/             # shadcn/ui
│       │   │   ├── charts/
│       │   │   ├── dialogs/
│       │   │   └── layout/
│       │   ├── pages/
│       │   ├── hooks/
│       │   ├── api/
│       │   ├── types/
│       │   ├── lib/
│       │   └── App.tsx
│       ├── package.json
│       ├── tailwind.config.ts
│       └── vite.config.ts
│
├── docker-compose.yml
├── .dockerignore
└── (existing Blazor files untouched)
```

---

## Execution Strategy — Fleet-Optimized Parallel Batches

The plan is structured into **sequential gates** and **parallel batches**. Each batch contains tasks that can be executed simultaneously by independent sub-agents with **zero file conflicts**.

```
GATE 0: Foundation (sequential — creates project skeleton)
    ↓
BATCH 1: Backend services (9 parallel agents — each owns distinct files)
    ↓
GATE 1: Verify backend builds
    ↓
BATCH 2: API controllers + React foundation (12 parallel agents)
    ↓
GATE 2: Verify API + React builds
    ↓
BATCH 3: React pages + features (12 parallel agents)
    ↓
GATE 3: Verify full integration
    ↓
BATCH 4: Polish, tests, Docker (4 parallel agents)
```

---

## GATE 0 — Foundation (Sequential)

> **Must complete before any parallel work. Creates the skeleton both projects depend on.**

### G0: Project Scaffolding

**Output files created by this step:**

Backend:
- `src/MoneyManager.Api/MoneyManager.Api.csproj`
- `src/MoneyManager.Api/Program.cs` (minimal: DI, CORS, Swagger, static files, SPA fallback, health endpoint)
- `src/MoneyManager.Api/appsettings.json`

Frontend:
- `src/moneymanager-web/` (full Vite scaffold with deps installed)
- `src/moneymanager-web/package.json` (all deps: tailwind, shadcn/ui, react-router, react-apexcharts, axios, @tanstack/react-query, lucide-react)
- `src/moneymanager-web/vite.config.ts` (proxy `/api` → localhost:5000)
- `src/moneymanager-web/tailwind.config.ts`

Docker:
- `Dockerfile` (multi-stage: node build → dotnet publish → runtime)
- `.dockerignore`
- `docker-compose.yml`

Solution:
- `MoneyManager.sln` updated with new API project

**Verification:** `dotnet build src/MoneyManager.Api` ✓, `cd src/moneymanager-web && npm run build` ✓

---

## BATCH 1 — Backend Services (9 parallel agents)

> Each agent owns a **distinct set of files**. No two agents write to the same file.
> Agents reference existing Blazor code at `C:\Projects\MoneyManager\` for logic to port.

### B1-DATA: Migrate Data Layer

**Owns:** `src/MoneyManager.Api/Data/` (all files)

| File to create | Source to port from | Key changes |
|---|---|---|
| `Data/DataContext.cs` | `Data/DBContext.cs` | Remove hardcoded path → use DI connection string from `appsettings.json`. Remove interceptors (Serilog replaces). Keep OnModelCreating. Add `DbSet<AiProvider>` for new table. |
| `Data/Transaction.cs` | `Data/Transaction.cs` | Keep entity + ToDto(). Keep AmountExt computed prop. |
| `Data/TransactionDto.cs` | (extract from Transaction.cs) | Separate file for DTO |
| `Data/Account.cs` | `Data/Account.cs` | Remove `TypeIcon` ([NotMapped] Icon type). Add `string TypeIconName` (string version for React). Remove Fluent UI using. |
| `Data/Category.cs` | `Data/Category.cs` | Remove `objIcon` (Icon type). Keep `pIcon` string. Keep CategoryIconEnum, CategoryTree, CategoryDropItem. Remove Fluent UI using. |
| `Data/Rule.cs` | `Data/Rule.cs` | Direct copy minus Icon. Keep RuleCompareType enum. |
| `Data/Balance.cs` | `Data/Balance.cs` | Direct copy. |

**Database connection config:**
```json
// appsettings.Development.json — points to local copy of existing DB
{ "ConnectionStrings": { "DefaultConnection": "Data Source=Data/MoneyManager.db" } }

// appsettings.json — Docker production path (volume mount)
{ "ConnectionStrings": { "DefaultConnection": "Data Source=/app/data/MoneyManager.db" } }
```

**Does NOT create destructive migrations.** Gate 1 handles the baseline migration safely.

---

### B1-MODEL: Migrate Model Layer

**Owns:** `src/MoneyManager.Api/Model/` (all files)

| File to create | Source |
|---|---|
| `Model/Enums.cs` | `Model/Enums.cs` |
| `Model/SettingsModel.cs` | `Model/SettingsModel.cs` |
| `Model/Chart/BalanceChart.cs` | `Model/Chart/BalanceChart.cs` |
| `Model/Chart/CategoryChart.cs` | `Model/Chart/CategoryChart.cs` |
| `Model/Chart/CumulativeSpendingChart.cs` | `Model/Chart/CumulativeSpendingChart.cs` |
| `Model/AI/TransactionAI.cs` | `Model/AI/TransactionAI.cs` |
| `Model/AI/OpenAIMessages.cs` | `Model/AI/OpenAIMessages.cs` — adapt OpenAISettings to be loaded from DB |
| `Model/Import/ImportTypeEnum.cs` | `Model/Import/ImportTypeEnum.cs` |
| `Model/Import/ImportFileParams.cs` | `Model/Import/ImportFileParams.cs` |
| `Model/Import/MintCSV.cs` | `Model/Import/MintCSV.cs` |
| `Model/Import/RBCCSV.cs` | `Model/Import/RBCCSV.cs` |
| `Model/Import/CIBCCSV.cs` | `Model/Import/CIBCCSV.cs` |
| `Model/AI/AiProvider.cs` | **NEW** — Entity: Id, Name, ProviderType, ApiKey (encrypted), ApiUrl, Model, IsDefault, CreatedAt |
| `Model/Api/` | **NEW** — API request/response DTOs as needed |

---

### B1-HELPERS: Migrate Helpers

**Owns:** `src/MoneyManager.Api/Helpers/` (all files)

| File to create | Source |
|---|---|
| `Helpers/Extensions.cs` | `Helpers/Extensions.cs` — keep StartOfMonth etc. |
| `Helpers/JSONHelper.cs` | `Helpers/JSONHelper.cs` — keep ReadJSON/WriteJSON |

Remove any `Application.UserAppDataPath` or WinForms references.

---

### B1-DS-CORE: Migrate DataService Core + Account + Category

**Owns:** `src/MoneyManager.Api/Services/DataService.cs`, `DataService.Account.cs`, `DataService.Category.cs`

**Source:** `Services/DataService.cs`, `Services/DataService.Account.cs`, `Services/DataService.Category.cs`

**Critical change:** Replace static `HashSet<Account>` and `HashSet<Category>` with `IMemoryCache`:
```csharp
public partial class DataService(IDbContextFactory<DataContext> contextFactory, IMemoryCache cache)
{
    private const string AccountsCacheKey = "accounts";
    private const string CategoriesCacheKey = "categories";
    // Cache get/set helper methods
}
```
- `InitStaticStorage()` → `WarmCacheAsync()` (called at startup from Program.cs)
- `GetAccounts()` → read from cache, fallback to DB
- `ChangeAccount()` → invalidate cache after write
- `GetCategoriesTree()` → same logic, read from cache
- `ChangeCategory()` → invalidate cache after write

---

### B1-DS-TRANS: Migrate DataService Transaction + Rule

**Owns:** `src/MoneyManager.Api/Services/DataService.Transaction.cs`, `DataService.Rule.cs`

**Source:** `Services/DataService.Transaction.cs`, `Services/DataService.Rule.cs`

Port all transaction query methods and rule CRUD/application methods. `ApplyRule()` must work with IMemoryCache instead of static Categories.

**New:** Add `GetRuleUsageCount(int ruleId)` method — counts transactions matching the rule pattern.

---

### B1-DS-CHART: Migrate DataService Chart + AI

**Owns:** `src/MoneyManager.Api/Services/DataService.Chart.cs`, `DataService.AI.cs`

**Source:** `Services/DataService.Chart.cs`, `Services/DataService.AI.cs`

Port all chart aggregation methods unchanged:
- `GetDates()`, `ChartGetTransactions()` (3 overloads), `ChartNetIncome()`, `ChartCumulativeSpending()`
- `AIGetTransactions()` (3 overloads), `AIGetTransactionsCSV()`

These are pure LINQ with no UI coupling — minimal changes needed.

---

### B1-IMPORT: Migrate TransactionService (Import Engine)

**Owns:** `src/MoneyManager.Api/Services/TransactionService.cs`, `TransactionService.Mint.cs`, `TransactionService.RBC.cs`, `TransactionService.CIBC.cs`

**Source:** All 4 `Services/TransactionService*.cs` files

**Changes:**
- Remove file move/delete logic at end of each import method → return result, let controller handle file lifecycle
- Change `Action<int> progress` callback → return `ImportResult { Count, Skipped, Errors }`
- Remove hardcoded date limits (2023/2024) → make optional parameter with default
- Keep: duplicate detection, account resolution (5 alt names), category resolution, rule application
- Constructor depends on `IDbContextFactory<DataContext>`, `DataService`, `DBService`

---

### B1-SYSTEM: Migrate DBService + SettingsService + New AiProviderService

**Owns:** `src/MoneyManager.Api/Services/DBService.cs`, `Services/SettingsService.cs`, `Services/AiProviderService.cs`

**DBService changes:**
- Replace `Application.UserAppDataPath` → configurable path from `IConfiguration`
- Add `SemaphoreSlim` for concurrent backup safety
- **New methods:** `ListBackups()` → list files with dates/sizes, `RestoreBackup(filename)`, `CleanupBackups(keepCount)`

**SettingsService changes:**
- Settings path from env var (`SETTINGS_PATH`) or `IConfiguration`
- Remove WinForms `Application.UserAppDataPath` references

**AiProviderService (NEW):**
- CRUD operations for AiProvider entity
- `GetDefaultProvider()`, `TestConnection(id)` (sends minimal API request)
- API key encryption using ASP.NET Data Protection API

---

### B1-AI: Migrate AIService

**Owns:** `src/MoneyManager.Api/Services/AIService.cs`

**Source:** `Services/AIService.cs`

**Changes:**
- Remove `IOptions<OpenAISettings>` → inject `AiProviderService` to load keys from DB
- `GetAnalysis()` takes optional `providerId` parameter, defaults to `GetDefaultProvider()`
- Keep all 14 analysis types, all prompts, all temperature settings
- Keep bilingual output (EN + RU)
- Keep `HttpClient` as static (already thread-safe)

---

## GATE 1 — Backend Build Verification (Sequential)

1. `dotnet build src/MoneyManager.Api` — must succeed with zero errors
2. **Database preservation — baseline migration approach:**
   - Copy existing `Data/MoneyManager.db` to `src/MoneyManager.Api/Data/` for local dev
   - Add to `.gitignore` (never commit the live DB — only the empty template)
   - Create a **baseline migration** describing the existing schema: `dotnet ef migrations add Baseline`
   - Mark baseline as already applied (insert into `__EFMigrationsHistory`) so EF doesn't try to recreate existing tables
   - Create incremental migration for new tables: `dotnet ef migrations add AddAiProviders`
   - **Verify:** Run `dotnet ef database update` against a **copy** of the existing DB → all existing data intact + new AiProviders table added
   - Generate review script: `dotnet ef migrations script` → inspect SQL to confirm zero destructive DDL
3. Smoke test: verify API starts and can query existing transactions from the real DB

---

## BATCH 2 — API Controllers + React Foundation (12 parallel agents)

> Backend controllers and React foundation can be built simultaneously.
> Each agent owns distinct files with zero overlap.

### B2-CTRL-TRANS: Transactions Controller

**Owns:** `src/MoneyManager.Api/Controllers/TransactionsController.cs`

```
GET    /api/transactions?period=12&accountId=&categoryId=&search=&page=1&pageSize=50&sortBy=date&sortDir=desc
GET    /api/transactions/{id}
PUT    /api/transactions/{id}           body: { categoryId, description }
DELETE /api/transactions/{id}
DELETE /api/transactions/bulk            body: { ids: [...] }
GET    /api/transactions/stats?period=   → { income, expenses, net, count }
GET    /api/transactions/export?period=  → CSV file download
```

---

### B2-CTRL-ACCT: Accounts Controller

**Owns:** `src/MoneyManager.Api/Controllers/AccountsController.cs`

```
GET    /api/accounts
GET    /api/accounts/{id}
POST   /api/accounts                     body: Account fields
PUT    /api/accounts/{id}                body: Account fields
DELETE /api/accounts/{id}                → 409 Conflict if transactions exist
```

---

### B2-CTRL-CAT: Categories Controller

**Owns:** `src/MoneyManager.Api/Controllers/CategoriesController.cs`

```
GET    /api/categories                   flat list
GET    /api/categories/tree              hierarchical
GET    /api/categories/{id}
POST   /api/categories                   body: { name, parentId?, icon? }
PUT    /api/categories/{id}
DELETE /api/categories/{id}
```

---

### B2-CTRL-RULES: Rules Controller

**Owns:** `src/MoneyManager.Api/Controllers/RulesController.cs`

```
GET    /api/rules
GET    /api/rules/{id}
POST   /api/rules                        body: Rule fields
PUT    /api/rules/{id}
DELETE /api/rules/{id}
POST   /api/rules/{id}/apply             apply single rule to uncategorized
POST   /api/rules/apply-all
GET    /api/rules/{id}/stats             → { usageCount }
```

---

### B2-CTRL-CHARTS: Charts Controller

**Owns:** `src/MoneyManager.Api/Controllers/ChartsController.cs`

```
GET    /api/charts/net-income?period=12
GET    /api/charts/cumulative-spending
GET    /api/charts/spending-by-category?period=12
GET    /api/charts/periods               → list of { code, label } period options
```

---

### B2-CTRL-IMPORT: Import Controller

**Owns:** `src/MoneyManager.Api/Controllers/ImportController.cs`

```
POST   /api/import/upload                multipart: file + bankType + createAccounts
GET    /api/import/csv-archive           list archived CSVs
GET    /api/import/csv-archive/{file}    parse & return CSV as JSON table
POST   /api/import/csv-archive/{file}/reimport
```

File lifecycle: save upload to temp → import → rename to `YYYY-MM-DD {Bank}.csv` → move to archive dir.
Auto-backup before and after import.

---

### B2-CTRL-SYSTEM: System Controller

**Owns:** `src/MoneyManager.Api/Controllers/SystemController.cs`

```
POST   /api/system/backup
GET    /api/system/backups               → [{ filename, date, sizeBytes }]
POST   /api/system/backups/{file}/restore
DELETE /api/system/backups/cleanup?keep=10
GET    /api/system/settings
PUT    /api/system/settings              body: SettingsModel
```

---

### B2-CTRL-AI: AI Controller

**Owns:** `src/MoneyManager.Api/Controllers/AiController.cs`

```
GET    /api/ai/providers
POST   /api/ai/providers                 body: AiProvider fields
PUT    /api/ai/providers/{id}
DELETE /api/ai/providers/{id}
POST   /api/ai/providers/{id}/test       test connection
POST   /api/ai/analyze                   body: { period, analysisType, providerId? }
GET    /api/ai/analysis-types            → list all 14 types with descriptions
```

---

### B2-REACT-LAYOUT: React Layout + Navigation + Theme

**Owns:** `src/moneymanager-web/src/components/layout/`, `src/moneymanager-web/src/App.tsx`, `src/moneymanager-web/src/index.css`

- Sidebar nav: Home, Transactions, Trends (collapsible: Net Income, Cumulative, By Category), AI Analysis, Settings (collapsible: App Settings, Accounts, Categories, Rules)
- Header with "Money Manager" + dark mode toggle
- Responsive: sidebar collapses on mobile (shadcn Sheet)
- Dark mode default, class-based toggle, localStorage persistence
- shadcn/ui theme tokens

---

### B2-REACT-ROUTING: React Routing

**Owns:** `src/moneymanager-web/src/router.tsx` (or routes in App.tsx)

All routes with lazy-loaded pages:
```
/                          → Dashboard
/transactions              → Transaction list
/charts/income             → Net Income
/charts/cumulative         → Cumulative Spending
/charts/spending           → By Category
/charts/month/:month       → Monthly detail
/ai                        → AI Analysis
/settings                  → App Settings
/settings/accounts         → Accounts
/settings/categories       → Categories
/settings/rules            → Rules
```

---

### B2-REACT-API: API Client + Types + Hooks

**Owns:** `src/moneymanager-web/src/api/`, `src/moneymanager-web/src/types/`, `src/moneymanager-web/src/hooks/`

- `api/client.ts` — Axios instance with base URL `/api`
- `types/` — TypeScript interfaces matching all C# models: Transaction, Account, Category, Rule, Balance, BalanceChart, CumulativeSpendingChart, TransactionAI, AiProvider, SettingsModel, ImportResult
- `hooks/useTransactions.ts` — React Query: list, get, update, delete, stats, export
- `hooks/useAccounts.ts` — React Query: list, create, update, delete
- `hooks/useCategories.ts` — React Query: list, tree, create, update, delete
- `hooks/useRules.ts` — React Query: list, create, update, delete, apply, stats
- `hooks/useCharts.ts` — React Query: netIncome, cumulative, spendingByCategory, periods
- `hooks/useImport.ts` — React Query: upload, csvArchive, reimport
- `hooks/useSystem.ts` — React Query: backup, list backups, restore, cleanup, settings
- `hooks/useAi.ts` — React Query: providers CRUD, analyze, analysisTypes

---

### B2-REACT-UI: Shared shadcn/ui Components

**Owns:** `src/moneymanager-web/src/components/ui/` (shadcn init), `src/moneymanager-web/src/components/dialogs/`, `src/moneymanager-web/src/components/shared/`

Initialize shadcn/ui components needed across pages:
- Button, Input, Select, Checkbox, Dialog, Sheet, DropdownMenu, Table, Card, Badge, Tabs, Separator, Tooltip, Label, RadioGroup, DatePicker, Command (for combobox), Popover, Progress, Skeleton

Shared custom components:
- `PeriodSelector` — dropdown for chart period codes (all existing codes + labels)
- `CategorySelector` — combobox with hierarchical categories
- `ConfirmDialog` — reusable confirmation modal

---

## GATE 2 — Full Stack Build Verification (Sequential)

1. `dotnet build src/MoneyManager.Api` — all controllers compile
2. `cd src/moneymanager-web && npm run build` — React builds with no TS errors
3. Swagger shows all endpoints
4. Fix any integration issues

---

## BATCH 3 — React Pages & Features (12 parallel agents)

> Each agent owns one page/feature. No file conflicts.

### B3-PAGE-TRANSACTIONS: Transaction List Page

**Owns:** `src/moneymanager-web/src/pages/TransactionsPage.tsx`, `src/moneymanager-web/src/components/TransactionEditDialog.tsx`

- DataTable with @tanstack/react-table: Date, Account, Amount (colored), Category, Description, Rule Applied icon
- Period filter dropdown (PeriodSelector component)
- Column filters: date (exact/range), account (dropdown), amount (exact/range), category (dropdown), description (text search)
- **Click-to-filter:** click Account/Category/Description cell → sets that as filter
- Footer stats bar: $ Income, $ Expenses, $ Net, Quantity
- Server-side pagination
- Edit dialog: Category selector, Description field, tabs for Apply Rule / Create Rule

---

### B3-PAGE-ACCOUNTS: Account Management Page

**Owns:** `src/moneymanager-web/src/pages/AccountsPage.tsx`, `src/moneymanager-web/src/components/AccountEditDialog.tsx`

- Table: Type icon, Shown Name (searchable), Description, Alt Names, Number, Hidden
- Add/Edit dialog: Type dropdown, Shown Name, Description, Number, Alt Names 1-5, Hide checkbox
- Delete with guard (409 → show message)

---

### B3-PAGE-CATEGORIES: Category Management Page

**Owns:** `src/moneymanager-web/src/pages/CategoriesPage.tsx`, `src/moneymanager-web/src/components/CategoryEditPanel.tsx`

- Left panel: tree view (parent categories with Lucide icons, indented children)
- Right panel: edit form (Name, Icon selector with Lucide icon previews, Parent selector)
- Add new category button
- Delete with confirmation

---

### B3-PAGE-RULES: Rule Management Page

**Owns:** `src/moneymanager-web/src/pages/RulesPage.tsx`, `src/moneymanager-web/src/components/RuleEditDialog.tsx`

- Table: Compare Type (badge), Original Description, New Description, Category (with icon), Usage Count, Actions
- Add/Edit dialog: Compare Type, Original Description, New Description, Category selector
- Delete confirmation
- "Apply All Rules" bulk action

---

### B3-PAGE-INCOME: Net Income Chart Page

**Owns:** `src/moneymanager-web/src/pages/charts/IncomePage.tsx`, `src/moneymanager-web/src/components/charts/NetIncomeChart.tsx`

- react-apexcharts mixed bar + line: Income bars (green), Expense bars (red), Net line
- PeriodSelector dropdown
- Data table below: Month | Income | Expenses | Net
- **Clickable months** → navigate to `/charts/month/{yyMM}`

---

### B3-PAGE-CUMULATIVE: Cumulative Spending Chart Page

**Owns:** `src/moneymanager-web/src/pages/charts/CumulativePage.tsx`, `src/moneymanager-web/src/components/charts/CumulativeSpendingChart.tsx`

- react-apexcharts area chart: Last month (muted) vs This month (accent)
- X: day 1-31, Y: cumulative $ spent
- Always current vs previous month (no period selector)

---

### B3-PAGE-SPENDING: Spending by Category Chart Page

**Owns:** `src/moneymanager-web/src/pages/charts/SpendingPage.tsx`, `src/moneymanager-web/src/components/charts/SpendingByCategoryChart.tsx`

- Two donut charts side-by-side: Income by category, Expenses by category
- PeriodSelector dropdown
- Click slice → show filtered transaction list below
- Labels with amounts and percentages

---

### B3-PAGE-MONTH: Monthly Detail Page

**Owns:** `src/moneymanager-web/src/pages/charts/MonthDetailPage.tsx`

- Route: `/charts/month/:month` (yyMM format)
- Donut charts (income + expense by category for that month)
- Transaction list filtered to that month

---

### B3-PAGE-IMPORT: Import & CSV Archive

**Owns:** `src/moneymanager-web/src/pages/ImportPage.tsx`, `src/moneymanager-web/src/components/ImportUploadZone.tsx`, `src/moneymanager-web/src/pages/CsvArchivePage.tsx`

- Drag-drop upload zone with bank auto-detect
- Manual bank selector fallback
- "Create accounts" toggle
- Progress indicator
- CSV Archive: list files, view as table, re-import, delete

---

### B3-PAGE-BACKUP: Backup Management

**Owns:** `src/moneymanager-web/src/pages/BackupPage.tsx`

- Manual backup button
- List backups table: filename, date, size
- Restore button per backup (ConfirmDialog)
- Cleanup button with keep-count input

---

### B3-PAGE-AI: AI Analysis Page

**Owns:** `src/moneymanager-web/src/pages/AiAnalysisPage.tsx`, `src/moneymanager-web/src/pages/AiProvidersPage.tsx`

**AI Analysis:**
- PeriodSelector, Provider selector dropdown
- Sidebar: 5 collapsible groups × 14 analysis types
- Run Analysis button → loading spinner → rendered results (HTML/Markdown)

**AI Providers (Settings sub-page):**
- CRUD table/dialog for providers: Name, Type, API Key (masked), URL, Model, Default toggle
- Test connection button

---

### B3-PAGE-DASHBOARD: Dashboard Home Page

**Owns:** `src/moneymanager-web/src/pages/DashboardPage.tsx`

- 6-card responsive grid:
  1. **Import Card** — upload button, bank detect, create-accounts toggle, backup button
  2. **Uncategorized** — mini transaction list (last 12mo, inline edit link)
  3. **Recent Transactions** — mini list (last 2 weeks)
  4. **Cumulative Spending** — compact chart (reuse CumulativeSpendingChart with height prop)
  5. **Net Income** — PeriodSelector + compact NetIncomeChart
  6. **Spending by Category** — PeriodSelector + compact donut chart

---

### B3-PAGE-SETTINGS: App Settings Page

**Owns:** `src/moneymanager-web/src/pages/SettingsPage.tsx`

- Backup path text field + save button
- Dark mode toggle
- Links to sub-pages (Accounts, Categories, Rules, AI Providers)

---

## GATE 3 — Integration Verification (Sequential)

1. Full build: `dotnet build` + `npm run build`
2. Docker build: `docker build -t moneymanager .`
3. Smoke test: docker run → verify dashboard loads → API endpoints return data
4. Fix integration issues

---

## BATCH 4 — Polish, Tests & Improvements (4 parallel agents)

### B4-DOCKER: Docker Finalization

**Owns:** `Dockerfile`, `docker-compose.yml`, `docs/deployment.md`

- Finalize multi-stage Dockerfile
- Environment variables: `DATABASE_PATH`, `BACKUP_PATH`, `CSV_ARCHIVE_PATH`, `SETTINGS_PATH`
- **Volume mounts (critical for data preservation):**
  - `/app/data` → SQLite database (**MoneyManager.db lives here, persists across container rebuilds**)
  - `/app/backups` → backup files
  - `/app/csv-archive` → imported CSV files
- docker-compose.yml for local dev with volume bind mounts
- `docs/deployment.md`: Synology NAS guide including:
  - How to copy existing `MoneyManager.db` to the NAS volume
  - First-run: API auto-applies pending migrations to existing DB
  - Backup procedure before container updates
  - Container update workflow that preserves data volumes

---

### B4-BACKEND-TESTS: Backend Tests (TUnit)

**Owns:** `src/MoneyManager.Api.Tests/` (new test project)

**Framework:** TUnit (`TUnit` NuGet package) — modern .NET testing framework with built-in DI, parallel execution, and `TestWebApplicationFactory` support.

**Project setup:**
```xml
<PackageReference Include="TUnit" Version="*" />
<PackageReference Include="TUnit.Assertions" Version="*" />
<PackageReference Include="TUnit.Engine" Version="*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.*" />
```

**Test infrastructure:**
```
src/MoneyManager.Api.Tests/
├── Infrastructure/
│   ├── TestWebAppFactory.cs      # Custom WebApplicationFactory with in-memory SQLite
│   ├── TestDatabaseSeeder.cs     # Seeds test DB with realistic financial data
│   └── TestBase.cs               # Base class with shared setup
├── Services/
│   ├── DataService.Account.Tests.cs
│   ├── DataService.Category.Tests.cs
│   ├── DataService.Transaction.Tests.cs
│   ├── DataService.Rule.Tests.cs
│   ├── DataService.Chart.Tests.cs
│   ├── TransactionService.Import.Tests.cs
│   ├── TransactionService.DuplicateDetection.Tests.cs
│   ├── DBService.Tests.cs
│   ├── AIService.Tests.cs
│   └── AiProviderService.Tests.cs
├── Controllers/
│   ├── TransactionsController.Tests.cs
│   ├── AccountsController.Tests.cs
│   ├── CategoriesController.Tests.cs
│   ├── RulesController.Tests.cs
│   ├── ChartsController.Tests.cs
│   ├── ImportController.Tests.cs
│   ├── SystemController.Tests.cs
│   └── AiController.Tests.cs
└── Integration/
    ├── ImportWorkflow.Tests.cs    # Full import → verify → chart flow
    ├── RuleApplication.Tests.cs   # Import → apply rules → verify categories
    └── DatabaseMigration.Tests.cs # Verify migrations on copy of real DB
```

**Test coverage targets:**

| Area | Tests | What's covered |
|------|-------|----------------|
| **DataService.Account** | 8+ | CRUD, cache invalidation, duplicate names, hide from graph |
| **DataService.Category** | 10+ | CRUD, tree hierarchy, parent-child, icon mapping, IsNew flag |
| **DataService.Transaction** | 12+ | Query with filters (period, account, category), pagination, stats (income/expense/net), update, delete, bulk delete |
| **DataService.Rule** | 8+ | CRUD, all 4 CompareTypes (Contains/StartsWith/EndsWith/Equals), ApplyRule to transaction, usage count |
| **DataService.Chart** | 10+ | GetDates (all period codes), ChartNetIncome aggregation, CumulativeSpending day-by-day, Transfer category exclusion, hidden account exclusion |
| **TransactionService.Import** | 15+ | CIBC/RBC/Mint CSV parsing, validation (malformed CSV, empty file, wrong format), account resolution (name/number/alt names), category resolution, auto-create accounts, date limit filtering |
| **Duplicate Detection** | 8+ | Exact match, fuzzy date ±5 days, description substring match, same amount different account, same description different amount |
| **DBService** | 5+ | Backup creation, list backups, restore, cleanup (keep N), concurrent backup safety |
| **AIService** | 5+ | All 14 analysis types return valid prompts, temperature ranges, CSV export format, provider selection |
| **Controllers** | 30+ | HTTP status codes, request/response serialization, validation errors, auth-free access, pagination, file upload |
| **Integration** | 5+ | Full import-to-chart workflow, rule application workflow, migration on real DB schema |

**Estimated total: 110+ backend tests**

---

### B4-FRONTEND-TESTS: Frontend Tests (Vitest + React Testing Library)

**Owns:** `src/moneymanager-web/vitest.config.ts`, `src/moneymanager-web/src/__tests__/`, `src/moneymanager-web/src/**/*.test.tsx`

**Framework:** Vitest + React Testing Library + MSW (Mock Service Worker) for API mocking.

**Setup:**
```json
// package.json devDependencies
"vitest": "latest",
"@testing-library/react": "latest",
"@testing-library/jest-dom": "latest",
"@testing-library/user-event": "latest",
"msw": "latest",
"@vitejs/plugin-react": "latest"
```

**Test structure:**
```
src/moneymanager-web/src/
├── __tests__/
│   ├── setup.ts                    # Vitest setup + MSW server
│   └── mocks/
│       ├── handlers.ts             # MSW request handlers for all API endpoints
│       └── data.ts                 # Realistic mock data matching DB schema
├── components/
│   ├── layout/
│   │   ├── Sidebar.test.tsx        # Navigation items, collapse, active state
│   │   └── Header.test.tsx         # Dark mode toggle, title
│   ├── shared/
│   │   ├── PeriodSelector.test.tsx  # All period codes, selection, callback
│   │   ├── CategorySelector.test.tsx # Tree rendering, search, selection, hierarchy
│   │   └── ConfirmDialog.test.tsx   # Open/close, confirm/cancel callbacks
│   ├── charts/
│   │   ├── NetIncomeChart.test.tsx  # Renders with data, handles empty data
│   │   ├── CumulativeChart.test.tsx # Two series rendering, day labels
│   │   └── SpendingChart.test.tsx   # Donut rendering, click events
│   └── dialogs/
│       ├── TransactionEditDialog.test.tsx  # Form fields, save, rule creation
│       ├── AccountEditDialog.test.tsx      # All fields, validation
│       └── RuleEditDialog.test.tsx         # CompareType selector, validation
├── pages/
│   ├── TransactionsPage.test.tsx    # Filter interaction, sorting, stats
│   ├── AccountsPage.test.tsx        # CRUD flow, delete guard message
│   ├── CategoriesPage.test.tsx      # Tree interaction, edit panel
│   ├── RulesPage.test.tsx           # Table, apply all, usage count
│   ├── DashboardPage.test.tsx       # Card layout, data loading states
│   ├── ImportPage.test.tsx          # File upload, bank detection, progress
│   └── AiAnalysisPage.test.tsx      # Analysis type selection, loading, results
├── hooks/
│   ├── useTransactions.test.ts      # Query params, mutation, cache invalidation
│   ├── useAccounts.test.ts          # CRUD mutations, optimistic updates
│   ├── useCategories.test.ts        # Tree data transformation
│   ├── useCharts.test.ts            # Period param passing, data shape
│   └── useImport.test.ts            # File upload, progress tracking
└── api/
    └── client.test.ts               # Base URL, error handling, interceptors
```

**Coverage targets:**

| Area | Tests | What's covered |
|------|-------|----------------|
| **Layout** | 8+ | Nav items render, active link highlighting, collapse/expand, dark mode toggle, responsive breakpoints |
| **Shared components** | 12+ | PeriodSelector (all codes), CategorySelector (tree traversal, search, selection), ConfirmDialog (open/close/confirm) |
| **Chart components** | 8+ | Data rendering, empty states, click handlers, period changes |
| **Dialogs** | 10+ | Form validation, save/cancel, field types, error display |
| **Pages** | 20+ | Data loading states (loading/error/success), filter interactions, CRUD flows, pagination |
| **Hooks** | 15+ | React Query cache behavior, mutation triggers, error states, data transformation |
| **API client** | 5+ | Request/response interceptors, error handling, base URL |

**Estimated total: 80+ frontend tests**

---

### B4-E2E-TESTS: Playwright End-to-End Tests

**Owns:** `src/e2e/` (new directory at src level)

**Framework:** Playwright (`@playwright/test`) — cross-browser E2E testing.

**Setup:**
```
src/e2e/
├── playwright.config.ts         # Base URL, browser matrix, screenshots on failure
├── fixtures/
│   ├── test-database.db         # Seeded SQLite DB for E2E (copy of real DB structure)
│   └── sample-cibc.csv          # Sample CIBC CSV for import tests
│   └── sample-rbc.csv           # Sample RBC CSV for import tests
├── pages/                       # Page Object Models
│   ├── DashboardPage.ts
│   ├── TransactionsPage.ts
│   ├── AccountsPage.ts
│   ├── CategoriesPage.ts
│   ├── RulesPage.ts
│   ├── ChartsPage.ts
│   ├── ImportPage.ts
│   ├── AiPage.ts
│   └── SettingsPage.ts
├── tests/
│   ├── navigation.spec.ts       # All nav links work, sidebar collapse
│   ├── dashboard.spec.ts        # Cards render, chart widgets load
│   ├── transactions.spec.ts     # List loads, filter by period/account/category, click-to-filter, edit dialog, stats update
│   ├── accounts.spec.ts         # CRUD: create → edit → verify → delete guard
│   ├── categories.spec.ts       # Tree display, add/edit/delete, parent-child
│   ├── rules.spec.ts            # CRUD: create → verify → apply all → check transactions
│   ├── import.spec.ts           # Upload CSV → progress → verify transactions appear → file in archive
│   ├── charts.spec.ts           # Net income chart loads, period selector changes data, clickable months
│   ├── backup.spec.ts           # Create backup → list → restore → cleanup
│   ├── ai-providers.spec.ts     # CRUD: add provider → set default → delete
│   ├── dark-mode.spec.ts        # Toggle theme, persists across navigation
│   └── responsive.spec.ts       # Mobile viewport: sidebar collapses, layout adapts
└── global-setup.ts              # Start API server with test DB, seed data
```

**Test scenarios:**

| Scenario | Steps | Validates |
|----------|-------|-----------|
| **Full import workflow** | Upload CIBC CSV → verify progress → check new transactions in list → verify chart data updated | Import engine, file handling, UI refresh |
| **Transaction management** | Filter by period → click account to filter → edit category → verify stats update | Filtering, click-to-filter, CRUD, stats |
| **Rule creation from transaction** | Open transaction → Create Rule tab → fill form → save → verify rule in rules page → apply all → verify categorization | Rule engine, cross-page state |
| **Category hierarchy** | Create parent → create child under parent → verify tree → verify in CategorySelector dropdown | Tree CRUD, hierarchy display |
| **Chart navigation** | Net Income page → click month bar → navigates to monthly detail → donut charts show data | Chart interaction, routing |
| **Backup/Restore** | Create backup → verify in list → import CSV → create another backup → restore first → verify transactions reverted | Backup lifecycle |
| **AI analysis** | Add provider → select analysis type → run analysis → verify results render | AI workflow |
| **Responsive** | Resize to mobile → sidebar collapses → hamburger menu works → tables scroll | Responsive design |

**Estimated total: 40+ E2E tests**

**Playwright config highlights:**
```ts
// playwright.config.ts
export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,  // Serial in CI (shared DB)
  use: {
    baseURL: 'http://localhost:5000',
    screenshot: 'only-on-failure',
    trace: 'on-first-retry',
  },
  webServer: {
    command: 'dotnet run --project ../MoneyManager.Api',
    port: 5000,
    reuseExistingServer: !process.env.CI,
    env: { DATABASE_PATH: 'fixtures/test-database.db' },
  },
});
```

---

## GitHub Actions CI/CD

**Owns:** `.github/workflows/ci.yml`, `.github/workflows/deploy.yml`

### CI Workflow (`.github/workflows/ci.yml`)

Triggered on: push to `main`, pull requests

```yaml
name: CI
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  backend-build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore src/MoneyManager.Api
      - run: dotnet build src/MoneyManager.Api --no-restore --configuration Release
      - run: dotnet publish src/MoneyManager.Api -c Release -o publish

  backend-tests:
    runs-on: ubuntu-latest
    needs: backend-build
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore src/MoneyManager.Api.Tests
      - run: dotnet run --project src/MoneyManager.Api.Tests --report-trx --report-trx-filename results.trx
      - uses: dorny/test-reporter@v1
        if: always()
        with:
          name: 'Backend Tests (TUnit)'
          path: '**/results.trx'
          reporter: dotnet-trx

  frontend-build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: src/moneymanager-web/package-lock.json
      - run: cd src/moneymanager-web && npm ci
      - run: cd src/moneymanager-web && npm run build
      - run: cd src/moneymanager-web && npx tsc --noEmit  # Type checking

  frontend-tests:
    runs-on: ubuntu-latest
    needs: frontend-build
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: src/moneymanager-web/package-lock.json
      - run: cd src/moneymanager-web && npm ci
      - run: cd src/moneymanager-web && npx vitest run --reporter=junit --outputFile=results.xml
      - uses: dorny/test-reporter@v1
        if: always()
        with:
          name: 'Frontend Tests (Vitest)'
          path: '**/results.xml'
          reporter: java-junit

  e2e-tests:
    runs-on: ubuntu-latest
    needs: [backend-tests, frontend-tests]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: src/moneymanager-web/package-lock.json
      - run: dotnet build src/MoneyManager.Api -c Release
      - run: cd src/moneymanager-web && npm ci && npm run build
      - run: cp src/moneymanager-web/dist/* src/MoneyManager.Api/wwwroot/ -r
      - run: cd src/e2e && npm ci
      - run: cd src/e2e && npx playwright install --with-deps chromium
      - run: cd src/e2e && npx playwright test
      - uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-report
          path: src/e2e/playwright-report/

  docker-build:
    runs-on: ubuntu-latest
    needs: [backend-tests, frontend-tests]
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/build-push-action@v5
        with:
          context: .
          push: false
          tags: moneymanager:latest
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

### Deploy Workflow (`.github/workflows/deploy.yml`)

Triggered manually or on release tags:

```yaml
name: Deploy
on:
  workflow_dispatch:
  push:
    tags: ['v*']

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: |
            ghcr.io/${{ github.repository }}:latest
            ghcr.io/${{ github.repository }}:${{ github.ref_name }}
          platforms: linux/amd64
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

### CI Pipeline Summary

```
                    ┌──────────────┐   ┌────────────────┐
                    │ backend-build│   │ frontend-build  │
                    └──────┬───────┘   └───────┬────────┘
                           │                   │
                    ┌──────▼───────┐   ┌───────▼────────┐
                    │ backend-tests│   │ frontend-tests  │
                    │   (TUnit)    │   │   (Vitest)      │
                    └──────┬───────┘   └───────┬────────┘
                           │                   │
                           └─────────┬─────────┘
                                     │
                    ┌────────────────▼─────────────────┐
                    │  e2e-tests (Playwright)           │
                    │  docker-build (build only, no push)│
                    └──────────────────────────────────┘
```

---

### B4-IMPROVEMENTS: Suggested New Features

**Owns:** Various files (each improvement is a self-contained feature)

Prioritized improvement suggestions (implement user's picks):

1. **Budget Tracking** — Set monthly budgets per category, show remaining vs spent, alerts
2. **Recurring Transaction Detection** — Auto-detect subscriptions, show monthly recurring total
3. **Net Worth Tracking** — Account balance history, net worth trend chart
4. **Full-Text Search** — Search across all transaction descriptions
5. **Bulk Categorization** — Multi-select uncategorized → assign category
6. **Split Transactions** — Split one transaction into multiple categories
7. **Tags/Labels** — Custom tags for flexible grouping
8. **Monthly Reports** — Auto-generated summary
9. **Import History Log** — Track imports with stats
10. **Keyboard Shortcuts** — Power-user navigation
11. **Data Integrity Checks** — Periodic validation

---

## Summary: Agent Count Per Batch

| Batch | Parallel Agents | Sequential Gates |
|-------|-----------------|------------------|
| Gate 0 | 1 (sequential) | Creates project skeleton |
| Batch 1 | **9** | Backend services — zero file overlap |
| Gate 1 | 1 (sequential) | Verify build + safe baseline migration |
| Batch 2 | **12** | 8 controllers + 4 React foundation — zero file overlap |
| Gate 2 | 1 (sequential) | Verify full stack builds |
| Batch 3 | **12** | 12 React pages/features — zero file overlap |
| Gate 3 | 1 (sequential) | Integration smoke test |
| Batch 4 | **6** | Docker, TUnit tests, Vitest tests, Playwright E2E, CI/CD workflows, improvements |

**Total: 39 parallelizable work packages + 4 sequential verification gates**
**Total estimated tests: 110+ backend (TUnit) + 80+ frontend (Vitest) + 40+ E2E (Playwright) = ~230+ tests**

---

## Risk Register

| Risk | Impact | Mitigation |
|------|--------|------------|
| EF migration damages existing DB | 🔴 **Data loss** | Baseline migration approach (no destructive DDL). Always backup before `database update`. Review generated SQL script. Test on DB copy first. |
| Docker rebuild loses data | 🔴 **Data loss** | Volume mount `/app/data` persists across rebuilds. Documented in deployment guide. |
| Static cache removal breaks service logic | 🟡 Bugs | Gate 1 build verification catches compile errors |
| Parallel agents create inconsistent interfaces | 🟡 Integration bugs | Gate 2 + Gate 3 catch mismatches |
| Docker volume permissions on Synology | 🟡 DB access failure | Document in deployment guide. Test on actual NAS. |
| Large CSV upload limits | 🟡 Import fails | Configure Kestrel max request body (50MB+) |
| AI key encryption on Linux | 🟡 Security | ASP.NET Data Protection API (cross-platform) |

---

## Notes

- Existing Blazor app remains **untouched** throughout migration
- New app uses **fresh EF migration chain** targeting the **same SQLite schema**
- No authentication by design — add as future Phase 9 if needed
- All chart date filtering on backend (as specified)
- Transfer category exclusion and hidden account exclusion preserved
- Canadian financial context (RRSP, TFSA, FHSA) preserved in AI prompts
- Bilingual output (EN + RU) preserved
