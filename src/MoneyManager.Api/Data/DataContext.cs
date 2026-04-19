using Microsoft.EntityFrameworkCore;

namespace MoneyManager.Api.Data;

/// <summary>
/// Entity Framework Core database context for the MoneyManager Web API.
/// </summary>
/// <remarks>
/// Manages the SQLite database connection and provides access to all database entities.
/// Connection string is configured externally via dependency injection in Program.cs.
/// Entities: Accounts, Categories, Transactions, Balances, Rules, AiProviders.
/// </remarks>
public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
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
    /// Gets or sets the DbSet for AiProvider entities.
    /// </summary>
    /// <value>
    /// A collection of <see cref="AiProvider"/> entities representing AI service provider configurations.
    /// </value>
    /// <remarks>
    /// Used for managing AI provider settings for financial analysis features.
    /// Supports multiple providers (OpenAI, ZAI, Custom) with encrypted API keys.
    /// </remarks>
    public DbSet<AiProvider> AiProviders { get; set; }

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
