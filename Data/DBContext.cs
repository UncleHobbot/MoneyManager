using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MoneyManager.Data;

/// <summary>
/// Entity Framework Core database context for the MoneyManager application.
/// </summary>
/// <remarks>
/// Manages the SQLite database connection and provides access to all database entities.
/// Configured with custom interceptors for query logging and change tracking debug output.
/// Entities: Accounts, Categories, Transactions, Balances, Rules.
/// Connection string is hardcoded to use SQLite file at Data/MoneyManager.db.
/// </remarks>
public class DataContext : DbContext
{
    /// <summary>
    /// Configures the database connection and registers interceptors.
    /// </summary>
    /// <param name="optionsBuilder">The builder used to configure the context options.</param>
    /// <remarks>
    /// Uses SQLite as the database provider with a file-based database.
    /// Registers <see cref="MMQueryInterceptor"/> for logging SQL queries to console.
    /// Registers <see cref="MMSaveChangeInterceptor"/> for debug view of changes being saved.
    /// Hardcoded path for development; in production, use configuration-based connection strings.
    /// </remarks>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder
        .UseSqlite(@"Data Source=c:\Projects\MoneyManager\Data\MoneyManager.db")
        .AddInterceptors([new MMQueryInterceptor(), new MMSaveChangeInterceptor()]);

    /// <summary>
    /// Gets or sets the DbSet for Account entities.
    /// </summary>
    /// <value>
    /// A collection of <see cref="Account"/> entities representing bank accounts.
    /// </value>
    /// <remarks>
    /// Used for CRUD operations on accounts.
    /// Supports querying, adding, updating, and deleting account records.
    /// </remarks>
    public DbSet<Account> Accounts { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for Category entities.
    /// </summary>
    /// <value>
    /// A collection of <see cref="Category"/> entities representing expense and income categories.
    /// </value>
    /// <remarks>
    /// Used for CRUD operations on categories.
    /// Supports hierarchical category structure with parent-child relationships.
    /// </remarks>
    public DbSet<Category> Categories { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for Transaction entities.
    /// </summary>
    /// <value>
    /// A collection of <see cref="Transaction"/> entities representing financial transactions.
    /// </value>
    /// <remarks>
    /// Used for CRUD operations on transactions.
    /// Supports importing, editing, and querying transaction records.
    /// Links to Accounts and Categories via foreign keys.
    /// </remarks>
    public DbSet<Transaction> Transactions { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for Balance entities.
    /// </summary>
    /// <value>
    /// A collection of <see cref="Balance"/> entities representing historical account balance snapshots.
    /// </value>
    /// <remarks>
    /// Used for CRUD operations on balance records.
    /// Supports tracking balance history over time for trend analysis.
    /// </remarks>
    public DbSet<Balance> Balances { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for Rule entities.
    /// </summary>
    /// <value>
    /// A collection of <see cref="Rule"/> entities representing auto-categorization rules.
    /// </value>
    /// <remarks>
    /// Used for CRUD operations on rules.
    /// Supports creating, modifying, and applying categorization rules to transactions.
    /// </remarks>
    public DbSet<Rule> Rules { get; set; }

    /// <summary>
    /// Configures the entity model relationships and properties.
    /// </summary>
    /// <param name="modelBuilder">The builder used to construct the entity model.</param>
    /// <remarks>
    /// Fluent API configuration for entity-specific settings.
    /// Configures Categories table name and value generation for Id property.
    /// Additional relationship configurations can be added here as needed.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().ToTable("Categories")
            .Property(f => f.Id).ValueGeneratedOnAdd();
    }
}

/// <summary>
/// Entity Framework Core interceptor for logging SQL queries to the console.
/// </summary>
/// <remarks>
/// Implements <see cref="DbCommandInterceptor"/> to intercept query execution.
/// Useful for debugging and understanding what SQL queries EF Core is generating.
/// Outputs both successful queries and failed commands.
/// </remarks>
public class MMQueryInterceptor : DbCommandInterceptor
{
    /// <summary>
    /// Called when a database reader is about to be executed.
    /// </summary>
    /// <param name="command">The database command being executed.</param>
    /// <param name="eventData">Event data for the command execution.</param>
    /// <param name="result">The interception result that can be modified.</param>
    /// <returns>
    /// The result of the interception, allowing modification of command behavior.
    /// </returns>
    /// <remarks>
    /// Logs the SQL command text to the console before execution.
    /// Helpful for debugging query generation and performance analysis.
    /// </remarks>
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Console.WriteLine(command.CommandText);
        return result;
    }

    /// <summary>
    /// Called when a database command fails to execute.
    /// </summary>
    /// <param name="command">The database command that failed.</param>
    /// <param name="eventData">Event data for the command failure.</param>
    /// <remarks>
    /// Logs the SQL command text to the console when an error occurs.
    /// Helps diagnose query errors and issues.
    /// Calls base implementation to allow standard error handling.
    /// </remarks>
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        Console.WriteLine(command.CommandText);
        base.CommandFailed(command, eventData);
    }
}

/// <summary>
/// Entity Framework Core interceptor for logging change tracker debug view during save operations.
/// </summary>
/// <remarks>
/// Implements <see cref="SaveChangesInterceptor"/> to intercept database save operations.
    /// Useful for debugging what changes EF Core is tracking and persisting.
    /// Outputs the detailed view of tracked entities before saving.
/// </remarks>
public class MMSaveChangeInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Called when SaveChangesAsync is executing, before changes are committed to the database.
    /// </summary>
    /// <param name="eventData">Event data containing the DbContext being saved.</param>
    /// <param name="result">The interception result that can be modified.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the interception result.
    /// </returns>
    /// <remarks>
    /// Captures the debug view of the ChangeTracker before saving.
    /// The debug view shows all tracked entities and their states (Added, Modified, Deleted).
    /// Useful for understanding what changes are being persisted to the database.
    /// </remarks>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = new CancellationToken())
    {
        var q = eventData.Context.ChangeTracker.DebugView.LongView;
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
