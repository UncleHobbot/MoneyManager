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
        // Intentionally not `await using`: the returned IQueryable is bound to this
        // context, which callers enumerate (ToListAsync/FirstOrDefaultAsync) after
        // this method returns. Disposing here would break them. Materializing this
        // method to a list (so the context can be disposed) is a separate change.
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
        await using var ctx = await contextFactory.CreateDbContextAsync();
        if (transaction.Id != 0)
        {
            ctx.Transactions.Update(transaction);
            await ctx.SaveChangesAsync();
        }
        return await GetTransactionsAsync();
    }

    /// <summary>
    /// Creates a new manually-entered transaction.
    /// </summary>
    /// <param name="accountId">The account the transaction belongs to.</param>
    /// <param name="date">The transaction date.</param>
    /// <param name="description">The transaction description (also stored as the original description).</param>
    /// <param name="amount">The absolute amount; its sign is ignored and the magnitude is stored.</param>
    /// <param name="isDebit"><c>true</c> for an expense, <c>false</c> for income.</param>
    /// <param name="categoryId">Optional category to assign.</param>
    /// <returns>
    /// The created transaction with its account and category loaded, or <c>null</c> if the
    /// account does not exist.
    /// </returns>
    /// <remarks>
    /// The account and (optional) category are resolved within the same context so they are
    /// tracked as existing entities and not re-inserted.
    /// </remarks>
    public async Task<Transaction?> AddTransactionAsync(
        int accountId,
        DateTime date,
        string description,
        decimal amount,
        bool isDebit,
        int? categoryId)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();

        var account = await ctx.Accounts.FindAsync(accountId);
        if (account is null)
            return null;

        Category? category = null;
        if (categoryId.HasValue)
            category = await ctx.Categories.FindAsync(categoryId.Value);

        var transaction = new Transaction
        {
            Account = account,
            Date = date,
            Description = description,
            OriginalDescription = description,
            Amount = Math.Abs(amount),
            IsDebit = isDebit,
            Category = category,
            IsRuleApplied = false,
        };

        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();

        return await ctx.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Category!.Parent)
            .FirstOrDefaultAsync(t => t.Id == transaction.Id);
    }
}
