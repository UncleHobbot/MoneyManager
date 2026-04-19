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

MoneyManager is a personal financial management desktop application designed as a Mint.com replacement. It enables users to track financial transactions, manage accounts/categories, visualize spending patterns, and receive AI-powered financial insights.

## Technology Stack

- **.NET 10.0** (Windows target) with **Windows Forms** shell
- **Blazor Hybrid** for UI components
- **Entity Framework Core 10** with **SQLite** for persistence
- **Microsoft Fluent UI Blazor** for modern UI components
- **ApexCharts** for data visualization
- **OpenAI API** for AI-powered financial analysis

## Architecture

Service-based architecture with dependency injection:

```
Program.cs → MainForm.cs → Main.razor → Blazor Pages/Components
                              ↓
                         Services Layer
                              ↓
                    Entity Framework Core
                              ↓
                         SQLite Database
```

## Key Directories

- `/Data/` - EF Core models and DbContext
- `/Services/` - Business logic (partial classes for organization)
- `/Pages/` - Blazor pages (Home, Transactions, Accounts, Categories, Rules, Settings, AI, Charts/)
- `/Components/` - Reusable Blazor components
- `/Model/` - DTOs and enums
- `/Helpers/` - Utility classes
- `/Migrations/` - EF Core migrations

## Important Files

### Data Models (`/Data/`)
- `DBContext.cs` - EF Core context with query interceptors
- `Transaction.cs` - Financial transaction entity with TransactionDto
- `Account.cs` - Bank account with alternative names support
- `Category.cs` - Hierarchical categories with icons (22 types)
- `Rule.cs` - Auto-categorization rules (Contains, StartsWith, EndsWith, Equals)
- `Balance.cs` - Account balance snapshots

### Services (`/Services/`)
DataService is a partial class split into:
- `DataService.cs` - Core service with static caching
- `DataService.Account.cs` - Account CRUD
- `DataService.Transaction.cs` - Transaction management
- `DataService.Category.cs` - Category management
- `DataService.Rule.cs` - Rule management
- `DataService.Chart.cs` - Chart data aggregation
- `DataService.AI.cs` - AI data preparation

TransactionService handles bank imports:
- `TransactionService.cs` - Core processing with duplicate detection
- `TransactionService.Mint.cs` - Mint.com CSV import
- `TransactionService.RBC.cs` - RBC bank import
- `TransactionService.CIBC.cs` - CIBC bank import

Other services:
- `AIService.cs` - OpenAI integration
- `DBService.cs` - Database backup
- `SettingsService.cs` - User preferences

## Coding Patterns

1. **Partial classes** - Services split across multiple files for organization
2. **Static caching** - `DataService.Accounts` and `DataService.Categories` cached in memory
3. **DTO pattern** - `TransactionDto` for UI binding
4. **Async/await** - All database operations are async
5. **IDbContextFactory** - Context created per-operation for thread safety
6. **Fluent UI components** - Use `<FluentDataGrid>`, `<FluentDialog>`, `<FluentTextField>`, etc.

## Database Schema

Tables: Accounts, Categories, Transactions, Rules, Balances

Key relationships:
- Transactions → Accounts (many-to-one)
- Transactions → Categories (many-to-one)
- Categories → Categories (self-referencing hierarchy)
- Rules → Categories (many-to-one)

## Configuration

- `appsettings.json` - Connection strings, OpenAI config, Serilog
- User Secrets - API keys (UserSecretsId in csproj)
- `SettingsService` - Runtime settings (dark mode, backup path)

## AI Development Workflow

**IMPORTANT**: After making code changes, always attempt to build the project to verify changes:

```bash
dotnet build
```

If build errors occur:
1. Read and analyze the error messages
2. Fix the errors (common issues: syntax errors, type mismatches, missing namespaces, Razor compilation errors)
3. Build again to verify the fix
4. Repeat until the build succeeds

This ensures all changes are syntactically correct and the project remains in a working state.

## Common Tasks

### Adding a new bank import format
1. Create `TransactionService.{BankName}.cs` as partial class
2. Add import method following existing patterns (see Mint, RBC, CIBC)
3. Update csproj with `<DependentUpon>` for file grouping

### Adding a new page
1. Create `.razor` file in `/Pages/`
2. Add route with `@page "/route"`
3. Inject services: `@inject DataService DataService`
4. Add navigation in `NavMenu.razor`

### Adding a new category icon
1. Add enum value to `CategoryIconEnum` in `Category.cs`
2. Add icon mapping in `CategoryHelper.CategoryIcon()` switch

### Working with transactions
```csharp
// Get transactions with filters
await DataService.GetTransactionsAsync(accountId, categoryId, startDate, endDate);

// Apply rules to uncategorized
await DataService.ApplyRulesAsync();
```

## Build & Run

```bash
dotnet build
dotnet run
```

## EF Core Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Notes

- Database location: `Data/MoneyManager.db`
- Empty template: `Data/MoneyManagerEmpty.db`
- Chart periods: m1 (month), y1 (year), 12 (last 12 months), w (7 days), a (all)
- Transfer category is filtered from spending charts
- Canadian financial context in AI prompts (RRSP, TFSA, FHSA)

