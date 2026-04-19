using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Tests.TestHelpers;

/// <summary>
/// Provides helper methods for creating in-memory SQLite database contexts for testing.
/// </summary>
public static class DbContextHelper
{
    /// <summary>
    /// Creates a new in-memory SQLite DataContext. The caller must dispose the returned holder
    /// which keeps the connection alive for the lifetime of the context.
    /// </summary>
    public static TestDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;

        var context = new DataContext(options);
        context.Database.EnsureCreated();

        return new TestDbContext(context, connection);
    }

    /// <summary>
    /// Seeds the given context with sample accounts, categories, and transactions.
    /// </summary>
    public static void SeedTestData(DataContext ctx)
    {
        var chequing = new Account
        {
            Name = "RBC Chequing",
            ShownName = "Chequing",
            Type = 0,
            Number = "12345"
        };
        var visa = new Account
        {
            Name = "RBC Visa",
            ShownName = "Visa",
            Type = 1,
            Number = "67890",
            AlternativeName1 = "RBC VISA CARD"
        };
        var hidden = new Account
        {
            Name = "Transfer Account",
            ShownName = "Transfer",
            Type = 3,
            IsHideFromGraph = true
        };
        ctx.Accounts.AddRange(chequing, visa, hidden);

        var food = new Category { Name = "Food", Icon = "Food" };
        var groceries = new Category { Name = "Groceries", Parent = food };
        var income = new Category { Name = "Income", Icon = "Income" };
        var transfer = new Category { Name = "Transfer", Icon = "Transfer" };
        var uncategorized = new Category { Name = "Uncategorized", Icon = "Misc" };
        var autoCreated = new Category { Name = "AutoCategory", IsNew = true };
        ctx.Categories.AddRange(food, groceries, income, transfer, uncategorized, autoCreated);

        ctx.SaveChanges();

        ctx.Transactions.AddRange(
            new Transaction
            {
                Account = chequing,
                Date = new DateTime(2025, 1, 15),
                Description = "Loblaws Groceries",
                OriginalDescription = "LOBLAWS #1234",
                Amount = 85.50m,
                IsDebit = true,
                Category = groceries
            },
            new Transaction
            {
                Account = chequing,
                Date = new DateTime(2025, 1, 20),
                Description = "Salary Deposit",
                OriginalDescription = "PAYROLL DEPOSIT",
                Amount = 3000m,
                IsDebit = false,
                Category = income
            },
            new Transaction
            {
                Account = visa,
                Date = new DateTime(2025, 2, 1),
                Description = "Netflix",
                OriginalDescription = "NETFLIX.COM",
                Amount = 16.99m,
                IsDebit = true,
                Category = null
            },
            new Transaction
            {
                Account = visa,
                Date = new DateTime(2025, 2, 5),
                Description = "Restaurant",
                OriginalDescription = "MCDONALD'S #5678",
                Amount = 12.50m,
                IsDebit = true,
                Category = food
            },
            new Transaction
            {
                Account = hidden,
                Date = new DateTime(2025, 1, 10),
                Description = "Internal Transfer",
                OriginalDescription = "TRANSFER",
                Amount = 500m,
                IsDebit = true,
                Category = transfer
            }
        );

        ctx.Rules.AddRange(
            new Rule
            {
                OriginalDescription = "NETFLIX",
                NewDescription = "Netflix Subscription",
                CompareType = RuleCompareType.Contains,
                Category = uncategorized
            },
            new Rule
            {
                OriginalDescription = "LOBLAWS",
                NewDescription = "Loblaws",
                CompareType = RuleCompareType.StartsWith,
                Category = groceries
            },
            new Rule
            {
                OriginalDescription = "PAYROLL DEPOSIT",
                NewDescription = "Salary",
                CompareType = RuleCompareType.Equals,
                Category = income
            }
        );

        ctx.SaveChanges();
    }

    /// <summary>
    /// Creates an IDbContextFactory that returns contexts sharing the same in-memory SQLite connection.
    /// </summary>
    public static TestDbContextFactory CreateFactory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;

        // Create schema
        using (var ctx = new DataContext(options))
        {
            ctx.Database.EnsureCreated();
        }

        return new TestDbContextFactory(options, connection);
    }

    /// <summary>
    /// Creates a DataService backed by an in-memory SQLite database with optional seed data.
    /// </summary>
    public static ServiceBundle CreateServiceBundle(bool seed = true)
    {
        var factory = CreateFactory();
        var cache = new MemoryCache(new MemoryCacheOptions());

        if (seed)
        {
            using var ctx = factory.CreateDbContext();
            SeedTestData(ctx);
        }

        var dataService = new DataService(factory, cache);
        return new ServiceBundle(factory, dataService, cache);
    }
}

/// <summary>
/// Holds a DataContext and its underlying SQLite connection for proper disposal.
/// </summary>
public sealed class TestDbContext : IDisposable
{
    public DataContext Context { get; }
    private readonly SqliteConnection _connection;

    public TestDbContext(DataContext context, SqliteConnection connection)
    {
        Context = context;
        _connection = connection;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

/// <summary>
/// An IDbContextFactory implementation that creates contexts using a shared in-memory SQLite connection.
/// </summary>
public sealed class TestDbContextFactory : IDbContextFactory<DataContext>, IDisposable
{
    private readonly DbContextOptions<DataContext> _options;
    private readonly SqliteConnection _connection;

    public TestDbContextFactory(DbContextOptions<DataContext> options, SqliteConnection connection)
    {
        _options = options;
        _connection = connection;
    }

    public DataContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}

/// <summary>
/// Bundles test dependencies (factory, services, cache) for convenient test setup.
/// </summary>
public sealed class ServiceBundle : IDisposable
{
    public TestDbContextFactory Factory { get; }
    public DataService DataService { get; }
    public IMemoryCache Cache { get; }

    public ServiceBundle(TestDbContextFactory factory, DataService dataService, IMemoryCache cache)
    {
        Factory = factory;
        DataService = dataService;
        Cache = cache;
    }

    public void Dispose()
    {
        Factory.Dispose();
        (Cache as IDisposable)?.Dispose();
    }
}
