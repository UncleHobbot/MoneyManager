using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Owns the in-memory storage and lifecycle of the read-mostly reference-data sets
/// the UI and the import pipeline resolve against: <see cref="Account"/> and
/// <see cref="Category"/>. See CONTEXT.md ("Reference Data Cache").
/// </summary>
/// <remarks>
/// The interface is the whole surface: collections in, eviction and warm out. Domain
/// lookups (by id/name, the category tree, <c>IsNew</c> filtering) live in the
/// consuming services, not here. Invalidation is evict-only — the next read lazily
/// reloads via read-through. Categories are always loaded with their
/// <see cref="Category.Parent"/>; accounts are flat. Single adapter, no interface
/// abstraction (ADR-0003); registered as a singleton wrapping <see cref="IMemoryCache"/>.
/// </remarks>
public class ReferenceDataCache(IDbContextFactory<DataContext> contextFactory, IMemoryCache cache)
{
    private const string AccountsCacheKey = "accounts";
    private const string CategoriesCacheKey = "categories";

    /// <summary>
    /// Pre-loads both reference-data sets. Call once at startup and after a database
    /// restore.
    /// </summary>
    public async Task WarmAsync()
    {
        cache.Set(AccountsCacheKey, await LoadAccountsAsync());
        cache.Set(CategoriesCacheKey, await LoadCategoriesAsync());
    }

    /// <summary>
    /// Gets the cached accounts, loading from the database on a miss. The returned
    /// collection is the live cached set — callers must not mutate it.
    /// </summary>
    public async Task<IReadOnlyCollection<Account>> GetAccountsAsync()
    {
        if (cache.TryGetValue(AccountsCacheKey, out HashSet<Account>? accounts) && accounts != null)
            return accounts;

        accounts = await LoadAccountsAsync();
        cache.Set(AccountsCacheKey, accounts);
        return accounts;
    }

    /// <summary>
    /// Gets the cached categories (with <see cref="Category.Parent"/> populated),
    /// loading from the database on a miss. The returned collection is the live cached
    /// set — callers must not mutate it.
    /// </summary>
    public async Task<IReadOnlyCollection<Category>> GetCategoriesAsync()
    {
        if (cache.TryGetValue(CategoriesCacheKey, out HashSet<Category>? categories) && categories != null)
            return categories;

        categories = await LoadCategoriesAsync();
        cache.Set(CategoriesCacheKey, categories);
        return categories;
    }

    /// <summary>Evicts the accounts set; the next read reloads it.</summary>
    public void InvalidateAccounts() => cache.Remove(AccountsCacheKey);

    /// <summary>Evicts the categories set; the next read reloads it.</summary>
    public void InvalidateCategories() => cache.Remove(CategoriesCacheKey);

    private async Task<HashSet<Account>> LoadAccountsAsync()
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();
        return (await ctx.Accounts.ToListAsync()).ToHashSet();
    }

    private async Task<HashSet<Category>> LoadCategoriesAsync()
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();
        return (await ctx.Categories.Include(c => c.Parent).ToListAsync()).ToHashSet();
    }
}
