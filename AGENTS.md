# AGENTS.md - AI Assistant Development Guide

## Project Overview

MoneyManager is a personal financial management desktop application (Mint.com replacement) built with **.NET 10.0** (Windows), **Blazor Hybrid**, **Entity Framework Core 10**, **SQLite**, **Fluent UI Blazor**, **ApexCharts**, and **OpenAI API**.

Architecture: Program.cs → MainForm.cs → Main.razor → Blazor Pages/Components → Services → EF Core → SQLite

## Build Commands

```bash
dotnet build              # Build project
dotnet run                # Run application
dotnet build --configuration Release
dotnet clean
dotnet restore
```

## EF Core Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef migrations script
```

## Code Style Guidelines

### XML Documentation (CRITICAL - MANDATORY)

**EVERY public class, method, property, event, and enum MUST have XML documentation comments.**

See `XML-Documentation-Standards.md` for complete guidelines. Summary:
- Add `///` XML comments to ALL public members
- Include `<summary>` for purpose
- Add `<param>` tags for all parameters
- Add `<returns>` for methods that return values
- Add `<value>` for properties
- Use `<remarks>` for implementation details and important notes
- Provide `<example>` for complex or non-obvious usage
- Use `<see cref="">` for cross-references
- Be detailed and meaningful, not generic

**This is NOT optional - documentation is considered part of the implementation.**

### Other Style Guidelines

- **Imports**: Implicit usings enabled (no explicit using for common namespaces)
- **Formatting**: Partial classes for services,4-space indent, opening braces on new line
- **Naming**: PascalCase (classes/methods/properties/constants), _camelCase (private fields), I-prefixed interfaces
- **Async**: All DB ops async/await, async method suffix, `IDbContextFactory<DataContext>` for contexts
- **EF Core**: Include related entities, `.ToHashSet()` for static caching (Accounts/Categories)
- **Blazor**: `@page` directive, `@inject` for services, Fluent UI components
- **Error Handling**: try-catch blocks, log errors, handle DB exceptions gracefully
- **DTOs**: Use DTOs for UI binding, create `ToDto()` methods on entities
- **Testing**: No test project; create xUnit/MSTest project when needed

## File Organization

- `/Data/` - EF Core models, DbContext (Transaction, Account, Category, Rule, Balance)
- `/Services/` - Business logic (partial classes: DataService, TransactionService, AIService, DBService, SettingsService)
- `/Pages/` - Blazor pages (Home, Transactions, Accounts, Categories, Rules, Settings, AI, Charts/)
- `/Components/` - Reusable Blazor components
- `/Model/` - DTOs and enums
- `/Helpers/` - Utility classes

## Database Schema

Tables: Accounts, Categories, Transactions, Rules, Balances

Relationships: Transactions→Accounts (many-to-one), Transactions→Categories (many-to-one), Categories→Categories (hierarchy), Rules→Categories (many-to-one)

## Configuration

- `appsettings.json` - Connection strings, OpenAI config, Serilog
- User Secrets - API keys (UserSecretsId in csproj)
- Database: `Data/MoneyManager.db` (template: `Data/MoneyManagerEmpty.db`)

## Important Files

**Data Models**: DBContext.cs, Transaction.cs, Account.cs, Category.cs (22 icons), Rule.cs (4 match types), Balance.cs

**Services**: DataService (partial: Account, Transaction, Category, Rule, Chart, AI), TransactionService (partial: Mint, RBC, CIBC), AIService, DBService, SettingsService

## Common Patterns

```csharp
public partial class DataService(IDbContextFactory<DataContext> contextFactory)
{
    public async Task<ReturnType> MethodNameAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        await ctx.SaveChangesAsync();
        return result;
    }
}
```

Static caching: `DataService.Accounts`, `DataService.Categories`

## Development Workflow

1. Read existing code to understand patterns
2. Implement following conventions
3. Build to verify: `dotnet build` (fix errors before proceeding)
4. Create EF migrations for schema changes
5. Update csproj with `<DependentUpon>` for partial class file grouping

## Common Tasks

**Add bank import**: Create `TransactionService.{BankName}.cs` partial class, follow existing patterns, update csproj

**Add page**: Create `.razor` in `/Pages/`, add `@page "/route"`, inject services, update NavMenu.razor

**Add category icon**: Add enum to `CategoryIconEnum`, update `CategoryHelper.CategoryIcon()` switch

## Important Notes

- Windows-only app (.NET 10.0-windows)
- Blazor Hybrid with Windows Forms shell
- Canadian financial context (RRSP, TFSA, FHSA) for AI features
- Transfer category filtered from spending charts
- Chart periods: m1 (month), y1 (year), 12 (last 12 months), w (7 days), a (all)
