using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides methods for managing financial accounts in the MoneyManager system.
/// </summary>
/// <remarks>
/// This partial class implements CRUD operations for <see cref="Account"/> entities.
/// Uses <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> for thread-safe caching.
/// All methods return updated account lists to support UI refresh patterns.
/// </remarks>
public partial class DataService
{
    /// <summary>
    /// Gets all accounts from the cache.
    /// </summary>
    /// <returns>A list of all <see cref="Account"/> objects in the system.</returns>
    /// <remarks>
    /// Returns data from the memory cache, loading from database if cache is empty.
    /// Returns a fresh list to prevent modification of cached collection.
    /// </remarks>
    public async Task<List<Account>> GetAccountsAsync()
    {
        var accounts = await GetCachedAccountsAsync();
        return accounts.ToList();
    }

    /// <summary>
    /// Adds a new account to the database or updates an existing account.
    /// </summary>
    /// <param name="account">
    /// The <see cref="Account"/> to add or update.
    /// If Id is 0, a new account is added; otherwise, the existing account is updated.
    /// </param>
    /// <returns>A list of all accounts after the operation.</returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Refreshes the memory cache after successful database operation.
    /// </remarks>
    public async Task<List<Account>> ChangeAccountAsync(Account account)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (account.Id == 0)
            ctx.Accounts.Add(account);
        else
            ctx.Accounts.Update(account);
        await ctx.SaveChangesAsync();

        var accounts = await RefreshAccountsCacheAsync();
        return accounts.ToList();
    }

    /// <summary>
    /// Deletes an account if it has no linked transactions.
    /// </summary>
    /// <param name="accountId">The unique identifier of the account to delete.</param>
    /// <returns>
    /// <c>true</c> if the account was deleted successfully;
    /// <c>false</c> if the account has linked transactions or was not found.
    /// </returns>
    /// <remarks>
    /// Prevents deletion of accounts with existing transactions to maintain data integrity.
    /// Refreshes the memory cache after successful deletion.
    /// </remarks>
    public async Task<bool> DeleteAccountAsync(int accountId)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var hasTransactions = await ctx.Transactions.AnyAsync(t => t.Account.Id == accountId);
        if (hasTransactions) return false;

        var account = await ctx.Accounts.FindAsync(accountId);
        if (account == null) return false;

        ctx.Accounts.Remove(account);
        await ctx.SaveChangesAsync();
        await RefreshAccountsCacheAsync();
        return true;
    }
}
