using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides methods for managing financial transactions.
/// </summary>
public partial class DataService
{
    /// <summary>
    /// Gets all transactions with related account and category data, excluding hidden accounts.
    /// </summary>
    /// <returns>
    /// An <see cref="IQueryable{Transaction}"/> containing all visible transactions
    /// with their associated <see cref="Transaction.Account"/> and <see cref="Transaction.Category"/> loaded.
    /// </returns>
    /// <remarks>
    /// Transactions belonging to accounts where <see cref="Account.IsHideFromGraph"/> is <c>true</c>
    /// are excluded from the results. Related category parent entities are also included for
    /// hierarchical category display.
    /// </remarks>
    public async Task<IQueryable<Transaction>> GetTransactionsAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return ctx.Transactions
            .Include(x => x.Account)
            .Include(x => x.Category)
            .Include(x => x.Category!.Parent)
            .Where(x => !x.Account.IsHideFromGraph)
            .Select(x => x);
    }

    /// <summary>
    /// Updates an existing transaction in the database.
    /// </summary>
    /// <param name="transaction">The transaction entity with updated values.</param>
    /// <returns>
    /// An <see cref="IQueryable{Transaction}"/> containing all visible transactions after the update.
    /// </returns>
    /// <remarks>
    /// Only transactions with a non-zero <see cref="Transaction.Id"/> are persisted.
    /// New (unsaved) transactions with Id == 0 are ignored.
    /// After saving, the full transaction list is returned via <see cref="GetTransactionsAsync"/>.
    /// </remarks>
    public async Task<IQueryable<Transaction>> ChangeTransactionAsync(Transaction transaction)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (transaction.Id != 0)
        {
            ctx.Transactions.Update(transaction);
            await ctx.SaveChangesAsync();
        }
        return await GetTransactionsAsync();
    }

    /// <summary>
    /// Deletes a transaction by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to delete.</param>
    /// <returns>
    /// <c>true</c> if the transaction was found and deleted; <c>false</c> if no transaction
    /// with the specified ID exists.
    /// </returns>
    public async Task<bool> DeleteTransactionAsync(int id)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var transaction = await ctx.Transactions.FindAsync(id);
        if (transaction == null) return false;
        ctx.Transactions.Remove(transaction);
        await ctx.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deletes all transactions from the database.
    /// </summary>
    /// <remarks>
    /// This is a destructive operation that removes every transaction record.
    /// Use with caution — there is no undo.
    /// </remarks>
    public async Task DeleteAllTransactionsAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        ctx.Transactions.RemoveRange(ctx.Transactions);
        await ctx.SaveChangesAsync();
    }
}
