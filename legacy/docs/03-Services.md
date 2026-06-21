# Services Documentation

## Overview

The Services layer implements business logic and acts as an intermediary between the Blazor UI and the Entity Framework Core data layer. Services are designed with dependency injection in mind and follow the partial class pattern for organization.

## Service Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Dependency Injection                     │
│           (Program.cs - ConfigureServices)                   │
└──────────────────────────┬──────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
    ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
    │ DataService │  │ Transaction │  │ AIService   │
    │ (7 partials)│  │  Service    │  │             │
    └──────┬──────┘  └──────┬──────┘  └──────┬──────┘
           │                │                 │
           └─────────────────┼─────────────────┘
                            │
                   ┌────────┼────────┐
                   ▼        ▼        ▼
            ┌──────────┐ ┌──────────┐ ┌──────────┐
            │DBService │ │ Settings │ │  Folder  │
            │          │ │ Service  │ │  Picker  │
            └──────────┘ └──────────┘ └──────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │ Entity Framework │
                   │     Core        │
                   └─────────────────┘
```

## DataService

**Primary Class**: `DataService(IDbContextFactory<DataContext> contextFactory)`

Core service for all CRUD operations, split into 7 partial files for organization.

### Static Properties

```csharp
public static HashSet<Account> Accounts { get; set; }
public static HashSet<Category> Categories { get; set; }
public static string NetIncomeChartPeriod { get; set; } = "12";
```

**Purpose**: In-memory caching of reference data for performance.

### Methods

#### Core Service (`DataService.cs`)

##### InitStaticStorage()
```csharp
public async Task InitStaticStorage()
```
- Loads Accounts and Categories from database into static cache
- Called at application startup
- Uses `.ToHashSet()` for fast lookups

#### Account Operations (`DataService.Account.cs`)

##### GetAccounts()
```csharp
public async Task<IQueryable<Account>> GetAccounts()
```
- Returns all accounts with eager loading
- Uses `ctx.Accounts.AsNoTracking()` for read-only queries

##### AddAccount(Account account)
```csharp
public async Task<IQueryable<Account>> AddAccount(Account account)
```
- Adds new account to database
- Updates static cache
- Returns updated account list

##### UpdateAccount(Account account)
```csharp
public async Task<IQueryable<Account>> UpdateAccount(Account account)
```
- Updates existing account
- Synchronizes with static cache
- Returns updated account list

##### DeleteAccount(Account account)
```csharp
public async Task<IQueryable<Account>> DeleteAccount(Account account)
```
- Removes account from database
- Cascades to related transactions (optional)
- Returns updated account list

#### Transaction Operations (`DataService.Transaction.cs`)

##### GetTransactions()
```csharp
public async Task<IQueryable<Transaction>> GetTransactions()
```
- Returns transactions with Account, Category, and Category.Parent loaded
- Filters out accounts marked `IsHideFromGraph`
- Uses LINQ for flexible querying in UI

##### ChangeTransaction(Transaction transaction)
```csharp
public async Task<IQueryable<Transaction>> ChangeTransaction(Transaction transaction)
```
- Updates or inserts transaction based on Id
- Returns refreshed transaction list
- Auto-commits to database

##### DeleteAllTransactions()
```csharp
public async Task DeleteAllTransactions()
```
- Bulk delete of all transactions
- Uses `RemoveRange()` for efficiency
- Useful for data reset

##### ApplyRules()
```csharp
public async Task ApplyRules()
```
- Applies auto-categorization rules to uncategorized transactions
- Iterates through all rules in order
- Updates `IsRuleApplied` flag

#### Category Operations (`DataService.Category.cs`)

##### GetCategories()
```csharp
public async Task<IQueryable<Category>> GetCategories()
```
- Returns all categories with Parent relationship
- Builds hierarchical structure

##### AddCategory(Category category)
```csharp
public async Task<IQueryable<Category>> AddCategory(Category category)
```
- Adds new category (parent or child)
- Updates static cache
- Returns updated category list

##### UpdateCategory(Category category)
```csharp
public async Task<IQueryable<Category>> UpdateCategory(Category category)
```
- Updates existing category
- Maintains parent-child relationships
- Returns updated category list

##### DeleteCategory(Category category)
```csharp
public async Task<IQueryable<Category>> DeleteCategory(Category category)
```
- Removes category from database
- Sets related transactions' CategoryId to NULL
- Returns updated category list

##### GetCategoryByName(string name)
```csharp
public async Task<Category?> GetCategoryByName(string name)
```
- Looks up category by name
- Case-sensitive matching
- Returns null if not found

#### Rule Operations (`DataService.Rule.cs`)

##### GetRules()
```csharp
public async Task<IQueryable<Rule>> GetRules()
```
- Returns all rules with Category relationship
- Includes category details for display

##### AddRule(Rule rule)
```csharp
public async Task<IQueryable<Rule>> AddRule(Rule rule)
```
- Adds new auto-categorization rule
- Returns updated rule list

##### UpdateRule(Rule rule)
```csharp
public async Task<IQueryable<Rule>> UpdateRule(Rule rule)
```
- Updates existing rule
- Returns updated rule list

##### DeleteRule(Rule rule)
```csharp
public async Task<IQueryable<Rule>> DeleteRule(Rule rule)
```
- Removes rule from database
- Returns updated rule list

#### Chart Operations (`DataService.Chart.cs`)

##### ChartGetTransactions(DateTime startDate, DateTime endDate)
```csharp
public async Task<List<Transaction>> ChartGetTransactions(DateTime startDate, DateTime endDate)
```
- Returns transactions within date range for charting
- Filters out Transfer category
- Excludes accounts marked `IsHideFromGraph`
- Includes Account, Category, and Category.Parent

##### ChartGetTransactionsP(string chartPeriod)
```csharp
public async Task<List<Transaction>> ChartGetTransactionsP(string chartPeriod)
```
- Convenience method using period codes (m1, y1, 12, w, a)
- Delegates to `GetDates()` for date calculation

##### GetDates(string chartPeriod, out DateTime startDate, out DateTime endDate)
```csharp
public void GetDates(string chartPeriod, out DateTime startDate, out DateTime endDate)
```
- Converts period codes to date ranges
- Supports: m1 (this month), y1 (this year), 12 (last 12 months), w (7 days), a (all), etc.

##### ChartNetIncome(string chartPeriod)
```csharp
public async Task<List<BalanceChart>> ChartNetIncome(string chartPeriod)
```
- Aggregates income vs expenses by month
- Returns `BalanceChart` objects with Month, Income, Expenses
- Filters Income category separately from other spending

##### ChartCumulativeSpending()
```csharp
public async Task<List<CumulativeSpendingChart>> ChartCumulativeSpending()
```
- Calculates day-by-day cumulative spending
- Compares current month vs last month
- Returns daily totals (1-31 days)

#### AI Operations (`DataService.AI.cs`)

##### AIGetTransactionsCSV(string chartPeriod)
```csharp
public async Task<string> AIGetTransactionsCSV(string chartPeriod)
```
- Exports transactions to CSV format for AI analysis
- Includes headers and formatted amounts
- Filters based on period parameter

## TransactionService

**Primary Class**: `TransactionService(IDbContextFactory<DataContext> contextFactory, DataService dataService, DBService dbService)`

Handles CSV imports from different banks with duplicate detection and account/category resolution.

### Core Methods (`TransactionService.cs`)

#### GetAccount(string? name, DataContext ctx, bool isCreateAccount = true)
```csharp
private async Task<Account?> GetAccount(string? name, DataContext ctx, bool isCreateAccount = true)
```
- Resolves account from name
- Matches by: Name, Number, AlternativeName1-5 (case-insensitive)
- Creates new account if not found and `isCreateAccount=true`
- Caches resolved accounts in dictionary for performance

#### GetCategory(string? name, DataContext ctx)
```csharp
private async Task<Category?> GetCategory(string? name, DataContext ctx)
```
- Resolves category from name
- Creates new category with `IsNew=true` if not found
- Caches resolved categories in dictionary

#### GetDefaultCategory(DataContext ctx)
```csharp
private async Task<Category?> GetDefaultCategory(DataContext ctx)
```
- Returns "Uncategorized" category
- Creates if doesn't exist

#### IsTransactionExists(...)
```csharp
private bool IsTransactionExists(DateTime date, decimal amount, bool isDebit, 
    string? originalDescription, Account? account, DataContext ctx, bool isDateFuzzy = false)
```
- Duplicate detection algorithm
- **Exact match**: Date, Amount, IsDebit, Account, Description
- **Fuzzy match** (isDateFuzzy): Date ±5 days, Amount, IsDebit, Account, Description substring
- Prevents duplicate imports

### Bank-Specific Importers

#### Mint CSV Import (`TransactionService.Mint.cs`)

##### ImportFileMint(string filePath, bool isCreateAccounts)
```csharp
public async Task<string> ImportFileMint(string filePath, bool isCreateAccounts)
```
- Parses Mint.com CSV format
- Expected columns: Date, Description, Amount, Category, Account
- Handles debit/credit from signed amounts
- Applies account/category matching
- Returns summary string with import stats

**CSV Format**:
```csv
Date,Description,Original Description,Amount,Transaction Type,Category,Account Name,Labels,Notes
01/15/2025,Grocery Store,Grocery Store,150.50,debit,Food,RBC Chequing,,
```

#### RBC CSV Import (`TransactionService.RBC.cs`)

##### ImportFileRBC(string filePath, bool isCreateAccounts)
```csharp
public async Task<string> ImportFileRBC(string filePath, bool isCreateAccounts)
```
- Parses RBC bank CSV format
- Expected columns: Account Type, Date, Description, Amount
- Account number matching
- Debit handling for withdrawals

**CSV Format**:
```csv
Account Type,Date,Description,Amount (CAD),...
Chequing,2025-01-15,"Loblaws Supermarket",150.50
```

#### CIBC CSV Import (`TransactionService.CIBC.cs`)

##### ImportFileCIBC(string filePath, bool isCreateAccounts)
```csharp
public async Task<string> ImportFileCIBC(string filePath, bool isCreateAccounts)
```
- Parses CIBC bank CSV format
- Expected columns: Date, Description, Debit, Credit
- Debit/Credit column separation
- Net amount calculation

**CSV Format**:
```csv
Date,Description,Debit,Credit,Account,Balance
2025-01-15,Grocery Store,150.50,000000000,Chequing,5000.00
```

### Import Summary Format

All import methods return a formatted summary:
```
Imported: 50 transactions
Skipped (duplicates): 5
New accounts created: 2
New categories created: 3
```

## AIService

**Primary Class**: `AIService(IOptions<OpenAISettings> options, DataService dataService)`

Integrates with OpenAI API for financial analysis and insights.

### Core Methods

#### GetAIResponse(string prompt, string? data, double temperature = 0.7)
```csharp
private async Task<AnalysisResult> GetAIResponse(string prompt, string? data, double temperature = 0.7)
```
- Makes HTTP POST request to OpenAI Chat Completions API
- Sends system prompt with Canadian financial context
- Appends user prompt and optional CSV data
- Returns `AnalysisResult` with success status, response text, and token usage

**System Prompt Features**:
- Financial advisor persona
- Canadian context (RRSP, TFSA, FHSA, bi-weekly payroll)
- Bilingual output (English and Russian)
- Structured output format (Summary, Result, Action Plan, Tips)
- Empathetic, non-judgmental tone

#### GetAnalysis(string period, string analysisType)
```csharp
public async Task<AnalysisResult> GetAnalysis(string period, string analysisType)
```
- Public method for Blazor UI
- Analysis types:
  - `SpendingGeneral`: Top categories and spending habits
  - `SpendingBudget`: 50/30/20 budget based on average income/expenses
  - `SpendingTrends`: Month-over-month comparison with table
- Delegates to `DataService.AIGetTransactionsCSV()` for data
- Returns AI analysis result

### AI Models (`Model/AI/OpenAIMessages.cs`)

#### OpenAISettings
```csharp
public class OpenAISettings
{
    public string ApiKey { get; set; }
    public string ApiUrl { get; set; }
    public string Model { get; set; }  // e.g., "gpt-4.1-mini"
}
```

#### OpenAIChatRequest
```csharp
public class OpenAIChatRequest
{
    public string model { get; set; }
    public List<OpenAIMessage> messages { get; set; }
    public double? temperature { get; set; }
}
```

#### OpenAIChatResponse
```csharp
public class OpenAIChatResponse
{
    public string Id { get; set; }
    public List<OpenAIChatChoice> Choices { get; set; }
    public OpenAIChatUsage Usage { get; set; }
}
```

#### AnalysisResult
```csharp
public class AnalysisResult(bool isSuccess, string result, int totalTokens)
{
    public bool IsSuccess { get; set; }
    public string Result { get; set; }
    public int TotalTokens { get; set; }
}
```

### Usage Examples

```csharp
// Get spending analysis for last 12 months
var result = await aiService.GetAnalysis("12", "SpendingGeneral");

// Check result
if (result.IsSuccess)
{
    Console.WriteLine(result.Result);  // Bilingual analysis
    Console.WriteLine($"Tokens used: {result.TotalTokens}");
}
```

## DBService

**Primary Class**: `DBService(IDbContextFactory<DataContext> contextFactory, SettingsService settingsService)`

Handles database backup and restore operations.

### Methods

#### BackupDatabase()
```csharp
public async Task BackupDatabase()
```
- Creates timestamped backup of `MoneyManager.db`
- Saves to configured backup path
- Filename format: `MoneyManagerBackup_YYYYMMDDHHmmss.db`
- Returns backup file path

#### RestoreDatabase(string backupPath)
```csharp
public async Task RestoreDatabase(string backupPath)
```
- Restores database from backup file
- Shows confirmation dialog before restore
- Copies backup to main database location
- Returns success/failure status

#### GetBackupFiles()
```csharp
public List<string> GetBackupFiles()
```
- Lists available backup files in backup directory
- Returns sorted list (newest first)

### Backup Dialog Integration

Uses Windows Forms `FolderBrowserDialog` for backup path selection.

## SettingsService

**Primary Class**: `SettingsService(string settingsFilePath)`

Manages user preferences and application settings.

### Methods

#### LoadSettings()
```csharp
public async Task<SettingsModel> LoadSettings()
```
- Loads settings from JSON file
- Returns `SettingsModel` instance
- Creates default settings if file doesn't exist

#### SaveSettings(SettingsModel settings)
```csharp
public async Task SaveSettings(SettingsModel settings)
```
- Persists settings to JSON file
- Overwrites existing file

### SettingsModel (`Model/SettingsModel.cs`)

```csharp
public class SettingsModel
{
    public bool IsDarkMode { get; set; } = true;
    public string? BackupPath { get; set; }
}
```

### Settings File Location

- File: `settings.json` (in application directory)
- Format: JSON with indented formatting
- Manages: Dark mode toggle, backup path

## FolderPicker

**Primary Class**: `FolderPicker`

Utility for Windows folder selection dialogs.

### Methods

#### SelectFolder(string description, string defaultPath)
```csharp
public static string? SelectFolder(string description, string defaultPath)
```
- Opens Windows Forms folder browser dialog
- Returns selected path or null if cancelled
- Uses `OpenFolderDialog` (Windows API Code Pack)

## Service Registration (Program.cs)

```csharp
// DbContext
builder.Services.AddDbContextFactory<DataContext>(options =>
    options.UseSqlite(@"Data Source=Data\MoneyManager.db")
           .AddInterceptors([new MMQueryInterceptor(), new MMSaveChangeInterceptor()]));

// Services
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<TransactionService>();
builder.Services.AddSingleton<AIService>();
builder.Services.AddSingleton<DBService>();
builder.Services.AddSingleton<SettingsService>();

// Configuration
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
```

## Common Patterns

### Async/Await Pattern
All database operations are async:
```csharp
public async Task<ReturnType> MethodAsync()
{
    var ctx = await contextFactory.CreateDbContextAsync();
    // operations
    await ctx.SaveChangesAsync();
    return result;
}
```

### Context Factory Pattern
Fresh context per operation for thread safety:
```csharp
var ctx = await contextFactory.CreateDbContextAsync();
try {
    // use context
    await ctx.SaveChangesAsync();
} finally {
    await ctx.DisposeAsync();
}
```

### Static Caching Pattern
Reference data cached at startup:
```csharp
await dataService.InitStaticStorage();

// Later access without DB call
var account = DataService.Accounts.FirstOrDefault(a => a.Name == "RBC");
```

### Partial Class Organization
Large services split by domain:
```csharp
// DataService.cs - Core
// DataService.Account.cs - Account operations
// DataService.Transaction.cs - Transaction operations
// DataService.Category.cs - Category operations
// DataService.Rule.cs - Rule operations
// DataService.Chart.cs - Chart operations
// DataService.AI.cs - AI operations
```

## Error Handling

### Database Exceptions
```csharp
try {
    await ctx.SaveChangesAsync();
} catch (DbUpdateException ex) {
    // Handle constraint violations, concurrency issues
    Console.WriteLine($"Database error: {ex.Message}");
}
```

### Validation
```csharp
if (string.IsNullOrWhiteSpace(transaction.Description))
{
    throw new ArgumentException("Description is required");
}
```

### Logging
All services use Serilog for structured logging (via EF interceptors).

## Performance Considerations

### AsNoTracking
Use for read-only queries:
```csharp
var accounts = await ctx.Accounts.AsNoTracking().ToListAsync();
```

### Include vs Separate Queries
```csharp
// Single query (good for small datasets)
var tx = await ctx.Transactions
    .Include(t => t.Account)
    .Include(t => t.Category)
    .ToListAsync();

// Separate queries (better for large datasets)
var accounts = await ctx.Accounts.ToListAsync();
var transactions = await ctx.Transactions.ToListAsync();
```

### Batch Operations
```csharp
ctx.Transactions.RemoveRange(transactions);  // Efficient
await ctx.SaveChangesAsync();
```

## Testing Considerations

### Mocking Services
```csharp
// In tests, use in-memory provider
var options = new DbContextOptionsBuilder<DataContext>()
    .UseInMemoryDatabase(databaseName: "TestDb")
    .Options;

var factory = new DbContextFactory<DataContext>(options);
var service = new DataService(factory);
```

### Test Data Factory
Create helper methods for test data generation:
```csharp
private Transaction CreateTestTransaction(decimal amount, string description)
{
    return new Transaction {
        Date = DateTime.Now,
        Amount = amount,
        IsDebit = true,
        Description = description,
        OriginalDescription = description
    };
}
```

## Future Service Enhancements

1. **Recurring Transactions Service**: Handle periodic transactions (subscriptions, bills)
2. **Budget Service**: Track spending against budgets
3. **Investment Service**: Portfolio tracking and performance
4. **Tax Service**: Canadian tax calculations and reporting
5. **Sync Service**: Cloud synchronization support
6. **Export Service**: PDF/Excel report generation
7. **Notification Service**: Reminders and alerts
