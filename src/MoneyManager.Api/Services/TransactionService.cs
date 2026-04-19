using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides functionality for importing transactions from CSV files from various banks.
/// </summary>
/// <remarks>
/// This service handles CSV imports from multiple banks (Mint.com, RBC, CIBC) with format-specific parsing logic.
///
/// Key features:
/// <list type="bullet">
/// <item><description>Automatic account and category resolution with optional auto-creation</description></item>
/// <item><description>Duplicate transaction detection (exact and fuzzy date matching)</description></item>
/// <item><description>In-memory caching of accounts and categories for import performance</description></item>
/// <item><description>Stream-based input for web API file upload compatibility</description></item>
/// </list>
///
/// Thread Safety: This service is not thread-safe. Import operations should run sequentially.
/// </remarks>
public partial class TransactionService(
    IDbContextFactory<DataContext> contextFactory,
    DataService dataService,
    DBService dbService)
{
    private Dictionary<string, Account> _accounts = [];
    private Dictionary<string, Category> _categories = [];

    /// <summary>
    /// Retrieves or creates an account based on the provided name.
    /// </summary>
    /// <param name="name">The account name, number, or alternative name to look up.</param>
    /// <param name="ctx">The database context to use for operations.</param>
    /// <param name="isCreateAccount">If <c>true</c>, creates a new account when not found; otherwise returns <c>null</c>.</param>
    /// <returns>
    /// The matched or newly created <see cref="Account"/>, or <c>null</c> if not found and <paramref name="isCreateAccount"/> is <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Matching strategies (in order):
    /// <list type="number">
    /// <item><description>In-memory cache lookup by exact name</description></item>
    /// <item><description>Database lookup by Name, Number (dashes removed), or AlternativeName1–5 (case-insensitive)</description></item>
    /// <item><description>Auto-creation if <paramref name="isCreateAccount"/> is <c>true</c></description></item>
    /// </list>
    /// </remarks>
    private async Task<Account?> GetAccountAsync(string? name, DataContext ctx, bool isCreateAccount = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_accounts.TryGetValue(name, out var existingAccount))
            return existingAccount;

        var accountInDB = await ctx.Accounts.FirstOrDefaultAsync(c => c.Name == name
            || c.Number == name.Replace("-", "")
            || (c.AlternativeName1 != null && c.AlternativeName1.ToUpper() == name.ToUpper())
            || (c.AlternativeName2 != null && c.AlternativeName2.ToUpper() == name.ToUpper())
            || (c.AlternativeName3 != null && c.AlternativeName3.ToUpper() == name.ToUpper())
            || (c.AlternativeName4 != null && c.AlternativeName4.ToUpper() == name.ToUpper())
            || (c.AlternativeName5 != null && c.AlternativeName5.ToUpper() == name.ToUpper()));

        if (accountInDB != null)
        {
            _accounts.Add(name, accountInDB);
            return accountInDB;
        }

        if (isCreateAccount)
        {
            var account = new Account { Name = name, ShownName = name };
            ctx.Accounts.Add(account);
            await ctx.SaveChangesAsync();
            _accounts.Add(name, account);
            return account;
        }

        return null;
    }

    /// <summary>
    /// Retrieves or creates a category based on the provided name.
    /// </summary>
    /// <param name="name">The category name to look up or create.</param>
    /// <param name="ctx">The database context to use for operations.</param>
    /// <returns>
    /// The matched or newly created <see cref="Category"/>, or <c>null</c> if <paramref name="name"/> is empty/whitespace.
    /// </returns>
    /// <remarks>
    /// Categories are always created if not found (no opt-out parameter).
    /// Newly created categories have <see cref="Category.IsNew"/> set to <c>true</c> for user review.
    /// </remarks>
    private async Task<Category?> GetCategoryAsync(string? name, DataContext ctx)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_categories.TryGetValue(name, out var existingCategory))
            return existingCategory;

        var categoryInDB = await ctx.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (categoryInDB != null)
        {
            _categories.Add(name, categoryInDB);
            return categoryInDB;
        }

        var category = new Category { Name = name, IsNew = true };
        ctx.Categories.Add(category);
        await ctx.SaveChangesAsync();
        _categories.Add(name, category);
        return category;
    }

    /// <summary>
    /// Retrieves the default "Uncategorized" category, creating it if necessary.
    /// </summary>
    /// <param name="ctx">The database context to use for operations.</param>
    /// <returns>The "Uncategorized" <see cref="Category"/>.</returns>
    private async Task<Category?> GetDefaultCategoryAsync(DataContext ctx) => await GetCategoryAsync("Uncategorized", ctx);

    /// <summary>
    /// Checks if a transaction already exists to prevent duplicates during import.
    /// </summary>
    /// <param name="date">The transaction date.</param>
    /// <param name="amount">The transaction amount.</param>
    /// <param name="isDebit">Whether the transaction is a debit.</param>
    /// <param name="originalDescription">The original description from the import file.</param>
    /// <param name="account">The account the transaction belongs to.</param>
    /// <param name="ctx">The database context to use for queries.</param>
    /// <param name="isDateFuzzy">If <c>true</c>, allows a ±5 day window for date matching.</param>
    /// <returns><c>true</c> if a matching transaction exists; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Description matching uses two strategies: exact trimmed match, or the import description containing
    /// the existing description. Fuzzy date matching is used for banks with inconsistent posting dates (e.g., CIBC).
    /// </remarks>
    private bool IsTransactionExists(DateTime date, decimal amount, bool isDebit, string? originalDescription,
        Account? account, DataContext ctx, bool isDateFuzzy = false)
    {
        if (isDateFuzzy)
        {
            var lowDate = date.AddDays(-5);
            var highDate = date.AddDays(5);
            return ctx.Transactions.Any(t => t.Date >= lowDate && t.Date <= highDate
                && t.Amount == amount && t.IsDebit == isDebit
                && t.Account.Id == account!.Id && (t.OriginalDescription.Trim() == originalDescription!.Trim() || originalDescription.Contains(t.OriginalDescription.Trim())));
        }

        return ctx.Transactions.Any(t => t.Date == date && t.Amount == amount && t.IsDebit == isDebit
            && t.Account.Id == account!.Id &&
            (t.OriginalDescription.Trim() == originalDescription!.Trim() ||
             originalDescription.Contains(t.OriginalDescription.Trim())));
    }

    /// <summary>
    /// Counts the number of data lines in a stream (total lines minus header row).
    /// </summary>
    /// <param name="stream">The stream to count lines in. Position is reset to 0 after counting.</param>
    /// <param name="hasHeader">If <c>true</c>, subtracts 1 for the header row.</param>
    /// <returns>The number of data lines.</returns>
    private static int CountStreamLines(Stream stream, bool hasHeader = true)
    {
        var count = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        while (reader.ReadLine() != null)
            count++;
        stream.Position = 0;
        return hasHeader ? count - 1 : count;
    }
}
