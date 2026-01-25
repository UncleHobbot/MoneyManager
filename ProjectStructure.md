# MoneyManager Project Structure

## Technology Stack

| Layer | Technology | Version |
|-------|------------|---------|
| Runtime | .NET | 10.0 |
| UI Shell | Windows Forms | - |
| UI Framework | Blazor Hybrid | - |
| UI Components | Fluent UI Blazor | 4.13.2 |
| ORM | Entity Framework Core | 10.0.1 |
| Database | SQLite | - |
| Charts | ApexCharts | 6.0.2 |
| CSV Parsing | CsvHelper | 33.1.0 |
| Logging | Serilog | 4.3.1 |
| AI | Microsoft.Extensions.AI.OpenAI | 10.1.1 |

## Directory Structure

```
MoneyManager/
│
├── Data/                           # Data layer
│   ├── DBContext.cs                # EF Core context with interceptors
│   ├── Transaction.cs              # Transaction entity + TransactionDto
│   ├── Account.cs                  # Account entity (5 alt names support)
│   ├── Category.cs                 # Category with hierarchy + icons
│   ├── Rule.cs                     # Auto-categorization rules
│   └── Balance.cs                  # Account balance snapshots
│
├── Services/                       # Business logic layer
│   ├── DataService.cs              # Core service (static cache)
│   ├── DataService.Account.cs      # Account CRUD operations
│   ├── DataService.Transaction.cs  # Transaction queries/updates
│   ├── DataService.Category.cs     # Category management
│   ├── DataService.Rule.cs         # Rule management
│   ├── DataService.Chart.cs        # Chart data aggregation
│   ├── DataService.AI.cs           # AI data preparation
│   ├── TransactionService.cs       # Core import processing
│   ├── TransactionService.Mint.cs  # Mint.com CSV import
│   ├── TransactionService.RBC.cs   # RBC bank CSV import
│   ├── TransactionService.CIBC.cs  # CIBC bank CSV import
│   ├── AIService.cs                # OpenAI integration
│   ├── DBService.cs                # Database backup
│   ├── SettingsService.cs          # User preferences
│   └── FolderPicker.cs             # Windows folder dialog
│
├── Pages/                          # Blazor pages
│   ├── Home.razor                  # Dashboard with import & charts
│   ├── Transactions.razor          # Transaction list & filters
│   ├── Accounts.razor              # Account management
│   ├── Categories.razor            # Category tree management
│   ├── CategoriesS.razor           # Alternative category view
│   ├── Rules.razor                 # Rule management
│   ├── Settings.razor              # App settings
│   ├── AI.razor                    # AI analysis interface
│   └── Charts/                     # Visualization pages
│       ├── Income.razor            # Income charts
│       ├── Spending.razor          # Spending analysis
│       ├── MonthStat.razor         # Monthly statistics
│       └── CumulativeSpendingPage.razor
│
├── Components/                     # Reusable components
│   ├── EditAccountDialog.razor     # Account edit dialog
│   ├── EditRuleDialog.razor        # Rule edit dialog
│   ├── EditTransactionDialog.razor # Transaction edit dialog
│   ├── NewCategoryDialog.razor     # New category dialog
│   ├── CategorySelector.razor      # Category picker
│   ├── ImportFileDialog.razor      # Import configuration
│   ├── TransactionsList.razor      # Transaction grid
│   ├── CumulativeSpending.razor    # Cumulative chart
│   ├── NetIncome.razor             # Net income chart
│   └── Spending.razor              # Spending chart
│
├── Layout/                         # Layout components
│   ├── MainLayout.razor            # App shell layout
│   └── NavMenu.razor               # Navigation menu
│
├── Model/                          # DTOs and enums
│   ├── AI/                         # AI-related models
│   ├── Chart/                      # Chart data models
│   └── Import/                     # Import configuration
│
├── Helpers/                        # Utility classes
│   └── Extensions.cs               # Extension methods
│
├── Migrations/                     # EF Core migrations
│
├── wwwroot/                        # Static assets
│   ├── css/                        # Stylesheets
│   └── index.html                  # Blazor host page
│
├── Program.cs                      # Entry point
├── MainForm.cs                     # Windows Forms container
├── Main.razor                      # Blazor root component
├── _Imports.razor                  # Global using directives
├── appsettings.json                # Configuration
└── MoneyManager.csproj             # Project file
```

## Database Schema

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Accounts   │     │ Transactions│     │ Categories  │
├─────────────┤     ├─────────────┤     ├─────────────┤
│ Id (PK)     │◄────│ AccountId   │     │ Id (PK)     │
│ Name        │     │ Id (PK)     │────►│ ParentId    │◄──┐
│ AltName1-5  │     │ Date        │     │ Name        │   │
│ Type        │     │ Description │     │ Icon        │───┘
│ Number      │     │ Amount      │     └─────────────┘
│ Description │     │ IsDebit     │
│ IsHidden    │     │ CategoryId  │────►┌─────────────┐
└─────────────┘     │ IsRuleApplied│    │   Rules     │
                    └─────────────┘     ├─────────────┤
┌─────────────┐                         │ Id (PK)     │
│  Balances   │                         │ Pattern     │
├─────────────┤                         │ MatchType   │
│ Id (PK)     │                         │ Description │
│ AccountId   │                         │ CategoryId  │
│ Date        │                         └─────────────┘
│ Amount      │
└─────────────┘
```

## Key Patterns

### Partial Classes
Services are split across files for organization:
- `DataService.cs` + `DataService.*.cs`
- `TransactionService.cs` + `TransactionService.*.cs`

### Static Caching
Frequently accessed data cached in memory:
```csharp
public static HashSet<Account> Accounts { get; set; }
public static HashSet<Category> Categories { get; set; }
```

### DTO Pattern
Transactions use DTOs for UI binding:
```csharp
Transaction.ToDto() → TransactionDto
```

### Dependency Injection
Services registered in MainForm.cs:
```csharp
services.AddDbContextFactory<DataContext>()
services.AddSingleton<DataService>()
services.AddScoped<TransactionService>()
```
