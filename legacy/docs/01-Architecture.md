# Architecture Overview

## Executive Summary

MoneyManager is a Windows desktop application for personal financial management, designed as a modern replacement for Mint.com. The application uses a hybrid architecture combining Windows Forms as a shell with Blazor for the user interface, backed by Entity Framework Core and SQLite for data persistence.

## Technology Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| **UI Framework** | Blazor Hybrid (WebAssembly in WebView) | Component-based UI rendering |
| **Shell** | Windows Forms | Desktop application container and window management |
| **UI Components** | Microsoft Fluent UI Blazor | Modern, accessible UI component library |
| **Data Access** | Entity Framework Core 10 | ORM with LINQ support |
| **Database** | SQLite | Lightweight, file-based database |
| **Visualization** | ApexCharts (Blazor-ApexCharts) | Interactive financial charts |
| **AI Integration** | OpenAI API (via Microsoft.Extensions.AI) | Financial analysis and insights |
| **CSV Processing** | CsvHelper | Bank transaction import parsing |
| **Logging** | Serilog | Structured logging |
| **.NET Version** | .NET 10.0-windows | Platform-specific Windows application |

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Program.cs (Entry Point)                 │
│                [Windows Application Entry]                   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   MainForm.cs (Shell)                        │
│        [Windows Forms Host for Blazor WebView]               │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   Main.razor (Root)                          │
│               [Blazor Router & Layout]                       │
└──────────────────────────┬──────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
    ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
    │   Pages/    │  │Components/  │  │  Services/  │
    │   .razor    │  │  .razor     │  │   .cs       │
    │  (Views)    │  │ (Reusable)  │  │ (Business)  │
    └──────┬──────┘  └──────┬──────┘  └──────┬──────┘
           │                │                 │
           └────────────────┼─────────────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │   DataService   │
                   │ (CRUD Operations)│
                   └────────┬────────┘
                            │
                            ▼
              ┌─────────────────────────────┐
              │   Entity Framework Core     │
              │    (DataContext)             │
              │  + Query Interceptors       │
              │  + Save Interceptors        │
              └─────────────────────────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │  SQLite Database│
                   │ MoneyManager.db │
                   └─────────────────┘
```

## Design Patterns

### 1. **Service Pattern**
Business logic is encapsulated in service classes that are injected via dependency injection:
- `DataService` - Core data operations (split into partial classes)
- `TransactionService` - Bank import processing
- `AIService` - OpenAI integration
- `DBService` - Database backup/restore
- `SettingsService` - User preferences

### 2. **Repository Pattern**
Entity Framework Core acts as the repository layer, with `DataService` providing a higher-level abstraction:
- Contexts are created per-operation using `IDbContextFactory<DataContext>`
- Includes related entities with `.Include()`
- Static caching for reference data (Accounts, Categories)

### 3. **DTO Pattern**
Data Transfer Objects separate domain entities from UI concerns:
- `TransactionDto` - UI binding for transactions
- `CategoryDropItem` - Category selector data
- `CategoryTree` - Hierarchical category structure

### 4. **Partial Classes**
Large services are split across multiple files for organization:
- `DataService.cs` (core)
- `DataService.Account.cs` (account operations)
- `DataService.Transaction.cs` (transaction operations)
- `DataService.Category.cs` (category operations)
- `DataService.Rule.cs` (rule management)
- `DataService.Chart.cs` (chart data aggregation)
- `DataService.AI.cs` (AI data preparation)

### 5. **Component-Based Architecture**
Blazor components provide reusable UI elements:
- Dialogs for editing entities
- Data grids for lists
- Chart components for visualizations
- Form components for data entry

## Data Flow

### Transaction Import Flow
```
1. User selects CSV file
   ↓
2. TransactionService.{Bank}.cs parses CSV
   ↓
3. Duplicate detection (IsTransactionExists)
   ↓
4. Account resolution (GetAccount - matches name or alternative names)
   ↓
5. Category resolution (GetCategory - creates if not exists)
   ↓
6. Transaction creation and SaveChangesAsync
   ↓
7. Rule application (DataService.ApplyRules)
   ↓
8. UI refresh
```

### AI Analysis Flow
```
1. User requests analysis (period, type)
   ↓
2. DataService.AIGetTransactionsCSV(period) extracts data
   ↓
3. AIService constructs prompt with financial context
   ↓
4. HTTP request to OpenAI API
   ↓
5. Parse response and display bilingual (English/Russian) results
```

## Key Architectural Decisions

### Why Blazor Hybrid?
- **Modern UI**: Leverages web technologies for rich, responsive interfaces
- **Component Reuse**: Shared components with potential web version
- **Familiar Development**: Blazor uses C# and Razor syntax
- **Fluent UI Integration**: Professional, accessible component library

### Why SQLite?
- **Zero Configuration**: No database server setup required
- **Portability**: Single file database, easy to backup
- **Performance**: Fast for personal finance scale (thousands of transactions)
- **Offline-First**: Works without internet connection

### Why Partial Classes?
- **Organization**: Large services split by domain (Account, Transaction, etc.)
- **Team Collaboration**: Multiple developers can work on different partials
- **File Grouping**: Visual Studio groups them in Solution Explorer

### Why DbContext Factory?
- **Thread Safety**: Creates fresh context per operation
- **Connection Pooling**: EF Core manages connections efficiently
- **Disposal**: Contexts are properly disposed after use

## Security Considerations

1. **API Keys**: Stored in User Secrets, never committed to git
2. **Database**: SQLite file can be encrypted for additional security
3. **Input Validation**: CSV imports validated before processing
4. **Injection Prevention**: EF Core parameterizes queries automatically

## Performance Characteristics

| Feature | Performance Consideration |
|---------|--------------------------|
| Transaction List | Paging via FluentDataGrid for large datasets |
| Chart Rendering | ApexCharts uses canvas for high-performance rendering |
| Database Queries | Indexed columns on foreign keys |
| AI Requests | Cached where possible, async operations |
| Static Caching | Accounts and Categories loaded at startup |

## Scalability Limitations

- **Single User**: Designed for personal use (not multi-user)
- **SQLite**: Best for <100K transactions (consider PostgreSQL/SQL Server for larger scale)
- **Desktop Only**: Windows-specific (no cross-platform support currently)
- **No Cloud Sync**: Local database only (future improvement opportunity)

## Deployment Model

```bash
# Build for release
dotnet build --configuration Release

# Publish as single executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Output: bin/Release/net10.0-windows/win-x64/publish/MoneyManager.exe
```

## Configuration Management

| Configuration Type | Location | Purpose |
|--------------------|----------|---------|
| **App Settings** | `appsettings.json` | Non-sensitive settings, connection strings |
| **User Secrets** | Development machine | API keys (not in source control) |
| **Runtime Settings** | `settings.json` | User preferences (dark mode, backup path) |

## Monitoring and Debugging

### Query Interceptors
- `MMQueryInterceptor` - Logs SQL queries to console
- `MMSaveChangeInterceptor` - Logs EF Core change tracker

### Logging (Serilog)
- Console output during development
- Configurable log levels (Information, Warning, Error)
- Structured logging with context (Application, Environment)

## Future Architectural Considerations

1. **Cross-Platform**: Could migrate to pure Blazor WASM for Mac/Linux support
2. **Cloud Sync**: Consider Azure Cosmos DB or PostgreSQL for multi-device sync
3. **Microservices**: Split AI and data services for better scalability
4. **API Layer**: Add REST/GraphQL API for third-party integrations
5. **Testing**: Add unit and integration test projects
6. **CI/CD**: Implement automated build and deployment pipelines
