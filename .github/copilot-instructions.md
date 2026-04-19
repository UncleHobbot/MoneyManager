# Copilot instructions for MoneyManager

## Build, test, and data commands

```bash
dotnet restore
dotnet build
dotnet run
dotnet build --configuration Release

dotnet ef migrations add <MigrationName>
dotnet ef database update
dotnet ef migrations script
```

- There is currently no test project in the repository, so there is no `dotnet test` command or single-test command to run yet.
- There is currently no dedicated lint command configured in the repository.

## High-level architecture

- This is a Windows-only Blazor Hybrid desktop app. `Program.cs` starts `MainForm`, and `MainForm.cs` is the real composition root: it configures DI, User Secrets, Serilog, Fluent UI, `AddDbContextFactory<DataContext>()`, copies `Data\MoneyManagerEmpty.db` to `Data\MoneyManager.db` when needed, and mounts `Main.razor` into the WinForms `BlazorWebView`.
- `Main.razor` is only the router. Most UI behavior lives in `Pages\*.razor` + `Pages\*.razor.cs`, while reusable dialogs and lists live in `Components\`.
- The service layer is the application boundary. `DataService` is the main CRUD/query service and is split into partials by domain (`Account`, `Category`, `Transaction`, `Rule`, `Chart`, `AI`). `TransactionService` is also partial, with one file per import source (`Mint`, `RBC`, `CIBC`) plus shared import helpers in `TransactionService.cs`.
- Data access uses EF Core with SQLite. `Data\DBContext.cs` defines `DataContext`, registers EF interceptors, and hardcodes the SQLite file path in `OnConfiguring` as `c:\Projects\MoneyManager\Data\MoneyManager.db`. `MainForm.cs` also configures the same absolute path for the DI-created context factory, so database-path changes need to update both places.
- Settings and financial data are stored separately: the main database uses the absolute path under `c:\Projects\MoneyManager\Data\MoneyManager.db`, while `SettingsService` persists user settings to `%APPDATA%\MoneyManager\settings.json`.
- Import flow is: CSV file -> bank-specific `TransactionService.*` parser -> duplicate detection -> account/category resolution -> optional account/category auto-creation -> `dataService.ApplyRule(transaction, context)` during import -> save -> UI refresh.

## Key conventions

- XML documentation is mandatory for every public class, method, property, event, and enum. Follow `XML-Documentation-Standards.md`; documentation quality is treated as part of the implementation.
- When adding new service partials, keep the existing file-grouping pattern in `MoneyManager.csproj` by adding the matching `<DependentUpon>` entry.
- Use `IDbContextFactory<DataContext>` and create a fresh context inside each async service operation. Do not keep a long-lived `DbContext` in components.
- `DataService` keeps private static account/category caches behind service methods such as `GetAccounts()`, `GetCategories()`, and `InitStaticStorage()`. Use those service methods and preserve cache refresh behavior when account/category data changes.
- Transaction UI code frequently projects EF entities to `TransactionDto` via `Transaction.ToDto()`. That pattern matters because UI filters often rely on DTO-only/computed fields such as signed `AmountExt`.
- Bank import matching is built around `Account.Name`, `Account.Number`, and `AlternativeName1` through `AlternativeName5`. Import code is allowed to auto-create missing accounts and categories, and duplicate detection may use fuzzy date matching.
- `TransactionService` is documented as not thread-safe and keeps mutable in-memory dictionaries during import. Even though it is registered as a singleton, import work should stay sequential unless you first redesign that state handling.
- New routable pages should follow the existing split partial pattern (`Page.razor` + `Page.razor.cs`) and usually need a corresponding navigation entry in `Layout\NavMenu.razor`.
- Charting and spending code intentionally filters out hidden accounts and transfer data in multiple places. Preserve those exclusions when changing chart or transaction-query logic.
- AI features use OpenAI settings from `appsettings.json` plus User Secrets, and the prompts assume Canadian financial context (for example RRSP, TFSA, and FHSA).
