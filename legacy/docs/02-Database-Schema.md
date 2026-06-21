# Database Schema Documentation

## Overview

MoneyManager uses SQLite with Entity Framework Core 10 as its persistence layer. The database follows a relational model with foreign key relationships between transactions, accounts, categories, and rules.

## Database File

- **Location**: `Data/MoneyManager.db`
- **Format**: SQLite
- **Template**: `Data/MoneyManagerEmpty.db` (fresh database schema without data)
- **Connection String**: `Data Source=Data\\MoneyManager.db`

## Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│    Accounts     │       │   Transactions   │       │    Categories    │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ Id (PK)         │◄──────│ Id (PK)         │       │ Id (PK)         │
│ Name            │       │ AccountId (FK)──┼───────►│ Id              │
│ ShownName       │       │ Date            │       │ Name            │
│ Description     │       │ Description     │       │ Icon            │
│ Type            │       │ OriginalDesc    │       │ ParentId (FK)───┼───┐
│ Number          │       │ Amount          │       │ IsNew           │   │
│ IsHideFromGraph │       │ IsDebit         │       └─────────────────┘   │
│ AlternativeName1│       │ CategoryId (FK)┼─────────────────────────────┘
│ AlternativeName2│       │ IsRuleApplied   │
│ AlternativeName3│       └─────────────────┘
│ AlternativeName4│
│ AlternativeName5│
└─────────────────┘

┌─────────────────┐       ┌─────────────────┐
│      Rules      │       │    Balances     │
├─────────────────┤       ├─────────────────┤
│ Id (PK)         │       │ Id (PK)         │
│ OriginalDesc    │       │ AccountId (FK)──┼───────┐
│ NewDescription  │       │ Date            │       │
│ CompareType     │       │ Amount          │       │
│ CategoryId (FK)─┼───────►│                │       │
└─────────────────┘       └─────────────────┘       │
                                                   │
                                                   │
                      (Self-referencing)           │
                      ┌─────────────────┐          │
                      │    Categories    │          │
                      ├─────────────────┤          │
                      │ ParentId (FK)───┼──────────┘
                      └─────────────────┘
```

## Tables

### 1. Accounts

Stores bank and financial account information.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| `Name` | TEXT | NOT NULL | Account name (from bank import) |
| `ShownName` | TEXT | NOT NULL | Display name for user |
| `Description` | TEXT | NULLABLE | Account description |
| `Type` | INTEGER | NOT NULL | Account type (0=Cash, 1=CreditCard, 2=Investment, 3=Other) |
| `Number` | TEXT | NULLABLE | Account number |
| `IsHideFromGraph` | INTEGER (BOOLEAN) | DEFAULT 0 | Hide from spending charts |
| `AlternativeName1` | TEXT | NULLABLE | Alternative name for matching (import) |
| `AlternativeName2` | TEXT | NULLABLE | Alternative name for matching (import) |
| `AlternativeName3` | TEXT | NULLABLE | Alternative name for matching (import) |
| `AlternativeName4` | TEXT | NULLABLE | Alternative name for matching (import) |
| `AlternativeName5` | TEXT | NULLABLE | Alternative name for matching (import) |

**Indexes**:
- Primary key on `Id`
- Unique index on `Name` (implicitly)

**Relationships**:
- One-to-many: Transactions (Account → Transactions)

**Business Rules**:
- Alternative names support flexible account matching during CSV imports
- `IsHideFromGraph` excludes accounts from financial visualizations

**Example Data**:
```sql
INSERT INTO Accounts (Name, ShownName, Type, IsHideFromGraph)
VALUES
  ('RBC Chequing', 'RBC Chequing', 0, 0),
  ('TD Visa', 'TD Visa Card', 1, 0),
  ('RBC Investment', 'RBC TFSA', 2, 0);
```

---

### 2. Categories

Hierarchical category structure for transaction classification.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| `ParentId` | INTEGER | NULLABLE, FK | Parent category (self-referencing) |
| `Name` | TEXT | NOT NULL | Category name |
| `Icon` | TEXT | NULLABLE | Icon identifier (enum string) |
| `IsNew` | INTEGER (BOOLEAN) | DEFAULT 0 | Flag for newly created categories |

**Indexes**:
- Primary key on `Id`
- Foreign key index on `ParentId`

**Relationships**:
- Self-referencing: Categories can have parent/child relationships
- One-to-many: Transactions (Category → Transactions)
- One-to-many: Rules (Category → Rules)

**Category Icons** (22 types):
```
Auto, Bills, Business, Education, Entertainment, Fees, Financial,
Food, Gifts, Health, Home, Income, Investment, Kids, Loans, Misc,
Personal, Pets, Shopping, Taxes, Transfer, Uncategorized
```

**Business Rules**:
- Flat structure with parent reference (not nested in same query)
- Top-level categories have icons, subcategories inherit parent icon
- `Transfer` category is filtered from spending charts
- `Income` category treated specially in financial calculations

**Example Data**:
```sql
INSERT INTO Categories (Name, Icon) VALUES ('Food', 'Food');
INSERT INTO Categories (ParentId, Name) VALUES (
  (SELECT Id FROM Categories WHERE Name = 'Food'),
  'Groceries'
);
```

---

### 3. Transactions

Core transaction data for all financial movements.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| `AccountId` | INTEGER | FK, NOT NULL | Associated account |
| `Date` | DATETIME | NOT NULL | Transaction date |
| `Description` | TEXT | NOT NULL | Display description (may be modified by rules) |
| `OriginalDescription` | TEXT | NOT NULL | Original description from import |
| `Amount` | DECIMAL | NOT NULL | Transaction amount (positive) |
| `IsDebit` | INTEGER (BOOLEAN) | NOT NULL | True if expense, False if income |
| `CategoryId` | INTEGER | FK, NULLABLE | Assigned category |
| `IsRuleApplied` | INTEGER (BOOLEAN) | DEFAULT 0 | Flag if auto-categorized by rule |

**Indexes**:
- Primary key on `Id`
- Foreign key indexes on `AccountId`, `CategoryId`
- Composite index recommended: `(AccountId, Date, Amount, IsDebit)` for duplicate detection

**Relationships**:
- Many-to-one: Account (Transactions → Accounts)
- Many-to-one: Category (Transactions → Categories)

**Computed Properties** (in C#):
- `AmountExt` - Extended amount: `IsDebit ? -Amount : Amount`

**Business Rules**:
- Amount is always stored as positive; `IsDebit` indicates direction
- `OriginalDescription` preserved for rule matching
- `Description` can be modified by rules
- Duplicate detection based on Date, Amount, IsDebit, Account, and fuzzy description match

**Example Data**:
```sql
INSERT INTO Transactions (AccountId, Date, Description, OriginalDescription, Amount, IsDebit, CategoryId, IsRuleApplied)
VALUES
  (1, '2025-01-15', 'Grocery Store', 'Loblaws Supermarket', 150.50, 1, 5, 1),
  (1, '2025-01-16', 'Salary Deposit', 'ABC COMPANY PAYROLL', 2500.00, 0, 13, 0);
```

---

### 4. Rules

Auto-categorization rules for transaction classification.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| `OriginalDescription` | TEXT | NOT NULL | Pattern to match in transaction description |
| `NewDescription` | TEXT | NOT NULL | New description (optional transformation) |
| `CompareType` | INTEGER | NOT NULL | Match type (0=Contains, 1=StartsWith, 2=EndsWith, 3=Equals) |
| `CategoryId` | INTEGER | FK, NOT NULL | Category to assign |

**Indexes**:
- Primary key on `Id`
- Foreign key index on `CategoryId`

**Relationships**:
- Many-to-one: Category (Rules → Categories)

**Match Types** (Enum `RuleCompareType`):
```csharp
0 - Contains     // Description contains pattern
1 - StartsWith    // Description starts with pattern
2 - EndsWith      // Description ends with pattern
3 - Equals        // Description exactly matches pattern
```

**Business Rules**:
- Rules applied in order when importing or manually triggered
- Can transform description (optional)
- Rule matching is case-insensitive in practice

**Example Data**:
```sql
INSERT INTO Rules (OriginalDescription, NewDescription, CompareType, CategoryId)
VALUES
  ('LOBLAWS', 'Grocery Store', 0, 5),  -- Contains
  ('NETFLIX', 'Netflix Subscription', 3, 6),  -- Equals
  ('AMAZON', '', 1, 18);  -- StartsWith, no description change
```

---

### 5. Balances

Historical balance snapshots for trend analysis.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | INTEGER | PRIMARY KEY, AUTOINCREMENT | Unique identifier |
| `AccountId` | INTEGER | FK, NOT NULL | Associated account |
| `Date` | DATETIME | NOT NULL | Balance snapshot date |
| `Amount` | DECIMAL | NOT NULL | Account balance amount |

**Indexes**:
- Primary key on `Id`
- Foreign key index on `AccountId`
- Composite index recommended: `(AccountId, Date)` for time-series queries

**Relationships**:
- Many-to-one: Account (Balances → Accounts)

**Business Rules**:
- Used for historical balance tracking
- Can be imported or manually entered
- Supports balance trend visualizations

**Example Data**:
```sql
INSERT INTO Balances (AccountId, Date, Amount)
VALUES
  (1, '2025-01-01', 5000.00),
  (1, '2025-02-01', 5200.00),
  (1, '2025-03-01', 5100.00);
```

---

## Database Constraints and Relationships

### Foreign Keys

| Table | Foreign Key | References |
|-------|-------------|------------|
| Transactions | AccountId | Accounts(Id) |
| Transactions | CategoryId | Categories(Id) |
| Categories | ParentId | Categories(Id) |
| Rules | CategoryId | Categories(Id) |
| Balances | AccountId | Accounts(Id) |

### Cascade Behavior

EF Core default cascade delete behavior:
- Deleting an Account → Cascades to Transactions and Balances
- Deleting a Category → Cascades to Transactions (sets CategoryId to NULL due to nullable)
- Deleting a Rule → No cascade (rule removal only)

## EF Core Interceptors

### MMQueryInterceptor
Logs all SQL queries to console for debugging:
```csharp
Console.WriteLine(command.CommandText);
```

### MMSaveChangeInterceptor
Logs EF Core change tracker state before saving:
```csharp
var q = eventData.Context.ChangeTracker.DebugView.LongView;
```

## Migration Strategy

### Creating Migrations

```bash
# Add a new migration
dotnet ef migrations add AddNewField

# Apply to database
dotnet ef database update

# Generate SQL script
dotnet ef migrations script
```

### Migration History
- Stored in `__EFMigrationsHistory` table
- Tracks applied migration names and timestamps

## Database Optimization

### Recommended Indexes

```sql
-- Duplicate detection optimization
CREATE INDEX IF NOT EXISTS IX_Transactions_DuplicateCheck
ON Transactions(AccountId, Date, Amount, IsDebit);

-- Chart queries optimization
CREATE INDEX IF NOT EXISTS IX_Transactions_DateRange
ON Transactions(Date, AccountId);

-- Category hierarchy optimization
CREATE INDEX IF NOT EXISTS IX_Categories_ParentId
ON Categories(ParentId);

-- Balance trend optimization
CREATE INDEX IF NOT EXISTS IX_Balances_AccountDate
ON Balances(AccountId, Date);
```

### Performance Considerations

1. **Include vs NoTracking**:
   - Use `.AsNoTracking()` for read-only queries (better performance)
   - Use `.Include()` when you need to modify related entities

2. **Batch Operations**:
   - `RemoveRange()` and `AddRange()` for bulk operations
   - EF Core batches changes automatically

3. **Static Caching**:
   - `DataService.Accounts` and `DataService.Categories` cached at startup
   - Reduces database queries for reference data

## Data Integrity

### Validation

- Entity-level validation via Data Annotations
- Transaction validation before save (duplicate detection)
- CSV import validation (required fields, date formats)

### Transactions

All multi-entity operations wrapped in database transactions:
```csharp
await using var transaction = await ctx.Database.BeginTransactionAsync();
try {
    // Multiple operations
    await ctx.SaveChangesAsync();
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
}
```

## Backup and Restore

### DBService Implementation

```csharp
// Backup
await File.CopyAsync("Data\\MoneyManager.db", backupPath);

// Restore (with confirmation dialog)
await File.CopyAsync(backupPath, "Data\\MoneyManager.db", overwrite: true);
```

### Backup Strategy

- Automatic backups before major operations (optional)
- User-initiated backups via Settings → Backup Path
- Timestamped backup files: `MoneyManagerBackup_YYYYMMDDHHmmss.db`

## Security

### File Permissions
- Database file should have restricted permissions (user-only read/write)
- Backups stored in secure location (user-selected)

### Encryption (Future)
- SQLite supports encryption via SQLCipher
- Consider for deployments requiring additional security

## Data Retention

- No automatic deletion of old transactions
- User responsible for data archiving if needed
- Export functionality available (future enhancement)

## Database Size Estimates

| Data Volume | Approximate Size |
|-------------|------------------|
| 1,000 transactions | ~100 KB |
| 10,000 transactions | ~1 MB |
| 100,000 transactions | ~10 MB |
| 1,000,000 transactions | ~100 MB |

SQLite scales well to millions of rows for personal finance use cases.
