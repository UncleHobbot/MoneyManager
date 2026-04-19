using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides core data access and business logic operations for MoneyManager application.
/// </summary>
/// <remarks>
/// This is a partial class split across multiple files for organization:
/// <list type="bullet">
/// <item><description><see cref="DataService"/> - Core service and cache initialization</description></item>
/// <item><description>DataService.Account.cs - Account CRUD operations</description></item>
/// <item><description>DataService.Category.cs - Category management</description></item>
/// </list>
/// Uses <see cref="IMemoryCache"/> for thread-safe account/category caching in the web environment.
/// Uses <see cref="IDbContextFactory{TContext}"/> for creating per-operation database contexts.
/// </remarks>
public partial class DataService(IDbContextFactory<DataContext> contextFactory, IMemoryCache cache)
{
    private const string AccountsCacheKey = "accounts";
    private const string CategoriesCacheKey = "categories";

    /// <summary>
    /// Initializes the in-memory cache with Accounts and Categories from the database.
    /// </summary>
    /// <remarks>
    /// Should be called once at application startup to warm the cache.
    /// Loads reference data into <see cref="IMemoryCache"/> for fast thread-safe access.
    /// </remarks>
    public async Task WarmCacheAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var accounts = (await ctx.Accounts.ToListAsync()).ToHashSet();
        var categories = (await ctx.Categories.Include(c => c.Parent).ToListAsync()).ToHashSet();
        cache.Set(AccountsCacheKey, accounts);
        cache.Set(CategoriesCacheKey, categories);
    }

    /// <summary>
    /// Gets the cached accounts, loading from DB if cache is empty.
    /// </summary>
    /// <returns>A <see cref="HashSet{T}"/> of all accounts.</returns>
    private async Task<HashSet<Account>> GetCachedAccountsAsync()
    {
        if (cache.TryGetValue(AccountsCacheKey, out HashSet<Account>? accounts) && accounts != null)
            return accounts;
        var ctx = await contextFactory.CreateDbContextAsync();
        accounts = (await ctx.Accounts.ToListAsync()).ToHashSet();
        cache.Set(AccountsCacheKey, accounts);
        return accounts;
    }

    /// <summary>
    /// Gets the cached categories, loading from DB if cache is empty.
    /// </summary>
    /// <returns>A <see cref="HashSet{T}"/> of all categories with parent references loaded.</returns>
    private async Task<HashSet<Category>> GetCachedCategoriesAsync()
    {
        if (cache.TryGetValue(CategoriesCacheKey, out HashSet<Category>? categories) && categories != null)
            return categories;
        var ctx = await contextFactory.CreateDbContextAsync();
        categories = (await ctx.Categories.Include(c => c.Parent).ToListAsync()).ToHashSet();
        cache.Set(CategoriesCacheKey, categories);
        return categories;
    }

    /// <summary>
    /// Refreshes the accounts cache from the database.
    /// </summary>
    /// <returns>The refreshed <see cref="HashSet{T}"/> of accounts.</returns>
    private async Task<HashSet<Account>> RefreshAccountsCacheAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var accounts = (await ctx.Accounts.ToListAsync()).ToHashSet();
        cache.Set(AccountsCacheKey, accounts);
        return accounts;
    }

    /// <summary>
    /// Refreshes the categories cache from the database.
    /// </summary>
    /// <returns>The refreshed <see cref="HashSet{T}"/> of categories with parent references loaded.</returns>
    private async Task<HashSet<Category>> RefreshCategoriesCacheAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var categories = (await ctx.Categories.Include(c => c.Parent).ToListAsync()).ToHashSet();
        cache.Set(CategoriesCacheKey, categories);
        return categories;
    }

    /// <summary>
    /// Gets or sets the default chart period for net income visualization. Defaults to "12" (last 12 months).
    /// </summary>
    /// <value>
    /// A string representing the default chart period.
    /// Supported period codes: m1 (this month), y1 (this year), 12 (last 12 months), w (last 7 days), a (all time).
    /// </value>
    public static string NetIncomeChartPeriod { get; set; } = "12";
}
