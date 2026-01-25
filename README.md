# MoneyManager

A personal financial management desktop application designed as a Mint.com replacement.

## Features

- **Transaction Management** - Import, categorize, and track financial transactions
- **Multi-Bank Support** - Import from Mint.com, RBC, and CIBC CSV exports
- **Smart Categorization** - Auto-categorize transactions with customizable rules
- **Account Management** - Track multiple accounts with alternative name matching
- **Financial Visualizations** - Interactive charts for spending, income, and trends
- **AI-Powered Insights** - Get personalized financial analysis and budgeting advice

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10.0 (Windows) |
| UI Shell | Windows Forms |
| UI Components | Blazor Hybrid + Fluent UI |
| Database | SQLite with Entity Framework Core |
| Charts | ApexCharts |
| AI | OpenAI API |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows OS

### Installation

1. Clone the repository
2. Configure API keys in user secrets (optional, for AI features):
   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "your-api-key"
   ```
3. Build and run:
   ```bash
   dotnet build
   dotnet run
   ```

## Project Structure

```
MoneyManager/
├── Data/                    # Entity Framework models & DbContext
│   ├── DBContext.cs         # Database context
│   ├── Transaction.cs       # Transaction entity
│   ├── Account.cs           # Account entity
│   ├── Category.cs          # Category entity with icons
│   └── Rule.cs              # Auto-categorization rules
├── Services/                # Business logic layer
│   ├── DataService*.cs      # Data operations (partial classes)
│   ├── TransactionService*.cs # Bank import handlers
│   ├── AIService.cs         # OpenAI integration
│   └── SettingsService.cs   # User preferences
├── Pages/                   # Blazor pages
│   ├── Home.razor           # Dashboard
│   ├── Transactions.razor   # Transaction list
│   ├── Accounts.razor       # Account management
│   ├── Categories.razor     # Category management
│   ├── Rules.razor          # Rule management
│   ├── AI.razor             # AI analysis
│   └── Charts/              # Visualization pages
├── Components/              # Reusable Blazor components
├── Model/                   # DTOs and enums
├── Migrations/              # EF Core migrations
└── wwwroot/                 # Static assets
```

## Supported Import Formats

| Source | Format | Fields |
|--------|--------|--------|
| Mint.com | CSV | Date, Description, Amount, Category, Account |
| RBC | CSV | Account Type, Date, Description, Amount |
| CIBC | CSV | Date, Description, Debit, Credit |

## Category Icons

The application includes 22 category icons: Auto, Bills, Business, Education, Entertainment, Fees, Financial, Food, Gifts, Health, Home, Income, Investment, Kids, Loans, Misc, Personal, Pets, Shopping, Taxes, Transfer, Travel, Uncategorized.

## Auto-Categorization Rules

Rules support four matching types:
- **Contains** - Description contains the pattern
- **StartsWith** - Description starts with the pattern
- **EndsWith** - Description ends with the pattern
- **Equals** - Description exactly matches the pattern

Rules can also transform the transaction description when applied.

## Database

SQLite database stored at `Data/MoneyManager.db`. Use EF Core migrations for schema changes:

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Configuration

Application settings in `appsettings.json`:
- Connection strings
- OpenAI configuration (API URL, model)
- Serilog logging

Sensitive settings (API keys) should use .NET User Secrets:
```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-key"
```

## Libraries

- [Microsoft Fluent UI Blazor](https://www.fluentui-blazor.net/) - UI components
- [Blazor ApexCharts](https://github.com/apexcharts/Blazor-ApexCharts) - Charts
- [CsvHelper](https://joshclose.github.io/CsvHelper/) - CSV parsing
- [Serilog](https://serilog.net/) - Structured logging
- [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/) - AI integration

## License

Private project.
