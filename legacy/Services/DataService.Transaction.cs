namespace MoneyManager.Services;

/// <summary>
/// Provides methods for managing financial transactions in the MoneyManager system.
/// </summary>
/// <remarks>
/// This partial class implements CRUD operations for <see cref="Data.Transaction"/> entities.
/// All queries eagerly load Account and Category navigation properties.
    /// Filters out accounts marked as hidden from graphs.
    /// Supports adding, updating, and deleting transactions.
/// </remarks>
public partial class DataService
{
    /// <summary>
    /// Gets all transactions with related account and category data.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a queryable collection of all transactions.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Eagerly loads Account and Category.Parent navigation properties to avoid N+1 query problems.
    /// Filters out transactions where <see cref="Data.Account.IsHideFromGraph"/> is true.
    /// Returns an <see cref="IQueryable{T}"/> for further filtering in UI.
    /// </remarks>
    public async Task<IQueryable<Transaction>> GetTransactions()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return ctx.Transactions.Include(x => x.Account).Include(x => x.Category).Include(x => x.Category.Parent)
            .Where(x => !x.Account.IsHideFromGraph).Select(x => x);
    }

    /// <summary>
    /// Updates an existing transaction in the database.
    /// </summary>
    /// <param name="transaction">
    /// The <see cref="Data.Transaction"/> to update. Must have a non-zero Id.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a queryable collection of all transactions after the update.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Only updates transactions with a non-zero Id (existing transactions).
    /// Does not add new transactions; use <see cref="AddTransaction(Data.Transaction)"/> for that.
    /// Returns refreshed transaction list after successful update.
    /// </remarks>
    public async Task<IQueryable<Transaction>> ChangeTransaction(Transaction transaction)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (transaction.Id != 0)
        {
            ctx.Transactions.Update(transaction);
            await ctx.SaveChangesAsync();
        }

        return await GetTransactions();
    }

    /// <summary>
    /// Deletes all transactions from the database.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous delete operation.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Uses <see cref="Microsoft.EntityFrameworkCore.DbSet{T}.RemoveRange(IEnumerable{T})"/> for efficient bulk deletion.
    /// This is a destructive operation and cannot be undone without a database backup.
    /// Typically used during data reset or testing.
    /// </remarks>
    public async Task DeleteAllTransactions()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        ctx.Transactions.RemoveRange(ctx.Transactions);
        await ctx.SaveChangesAsync();
    }
}
