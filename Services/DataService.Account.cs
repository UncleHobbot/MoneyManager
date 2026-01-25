namespace MoneyManager.Services;

/// <summary>
/// Provides methods for managing financial accounts in the MoneyManager system.
/// </summary>
/// <remarks>
/// This partial class implements CRUD operations for <see cref="Data.Account"/> entities.
/// Uses static caching via <see cref="Accounts"/> property for performance.
/// All methods return updated account list to refresh UI.
/// </remarks>
public partial class DataService
{
    /// <summary>
    /// Gets or sets the in-memory cache of all accounts.
    /// </summary>
    /// <value>
    /// A <see cref="HashSet{T}"/> containing all accounts from database.
    /// Initialized empty and populated by <see cref="InitStaticStorage"/>.
    /// </value>
    /// <remarks>
    /// Static cache provides O(1) lookups for account operations.
    /// Updated whenever accounts are added, modified, or deleted.
    /// </remarks>
    private static HashSet<Account> Accounts { get; set; } = [];

    /// <summary>
    /// Gets all accounts from the in-memory cache.
    /// </summary>
    /// <returns>
    /// A list of all <see cref="Data.Account"/> objects in the system.
    /// </returns>
    /// <remarks>
    /// Returns data from static cache, not from database.
    /// Returns a fresh list to prevent modification of cached collection.
    /// Use this for displaying account lists in UI.
    /// </remarks>
    public List<Account> GetAccounts() => Accounts.ToList();

    /// <summary>
    /// Adds a new account to the database or updates an existing account.
    /// </summary>
    /// <param name="account">
    /// The <see cref="Data.Account"/> to add or update.
    /// If Id is 0, a new account is added; otherwise, existing account is updated.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a list of all accounts after the operation.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Updates static cache after successful database operation.
    /// Synchronous save operations (no explicit transaction).
    /// </remarks>
    public async Task<List<Account>> ChangeAccount(Account account)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (account.Id == 0)
            ctx.Accounts.Add(account);
        else
            ctx.Accounts.Update(account);
        await ctx.SaveChangesAsync();

        Accounts = (await ctx.Accounts.ToListAsync()).ToHashSet();
        return Accounts.ToList();
    }
}
