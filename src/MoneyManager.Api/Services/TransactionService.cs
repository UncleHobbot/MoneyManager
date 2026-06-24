using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Services.Import;

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
/// Thread Safety: <see cref="ImportAsync(Stream, IBankImporter, bool)"/> is thread-safe -
/// each call uses method-local caches for accounts and categories.
/// </remarks>
public partial class TransactionService(
    IDbContextFactory<DataContext> contextFactory,
    DataService dataService)
{

    /// <summary>
    /// Imports transactions from a CSV stream using the supplied
    /// <paramref name="importer"/> for bank-specific parsing. This is the
    /// post-migration pipeline that replaces the per-bank
    /// <c>Import{Mint,Rbc,Cibc}CsvAsync</c> methods.
    /// </summary>
    /// <remarks>
    /// The pipeline owns: validation (via adapter), line counting,
    /// account/category resolution (with per-call caches), dedup,
    /// optional rule application, batch save. The adapter owns: CSV parsing,
    /// sign convention, description composition, category source. Backup is
    /// no longer the pipeline's concern — the caller takes one backup before an
    /// import batch (ADR-0008).
    /// Per-call state (the <paramref name="accounts"/>/<paramref name="categories"/>
    /// caches) lives in method-local variables, so concurrent imports do not
    /// corrupt each other.
    /// </remarks>
    public async Task<ImportResult> ImportAsync(
        Stream csvStream,
        IBankImporter importer,
        bool isCreateAccounts = true)
    {
        importer.Validate(csvStream);

        var total = CountStreamLines(csvStream, hasHeader: importer.HasHeaderRecord);

        var accounts = new Dictionary<string, Account>();
        var categories = new Dictionary<string, Category>();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var defaultCategory = await GetDefaultCategoryAsync(ctx, categories);

        var transactions = new List<Transaction>();

        foreach (var row in importer.ReadRows(csvStream))
        {
            var account = await GetAccountAsync(row.AccountName, ctx, accounts, isCreateAccounts);
            if (account is null)
                continue;

            var category = string.IsNullOrEmpty(row.CategoryHint)
                ? defaultCategory
                : await GetCategoryAsync(row.CategoryHint, ctx, categories);

            if (IsTransactionExists(row.Date, row.Amount, row.IsDebit, row.OriginalDescription, account, ctx, importer.UseFuzzyDateMatch))
                continue;

            var transaction = new Transaction
            {
                Account = account,
                Date = row.Date,
                Description = row.Description,
                OriginalDescription = row.OriginalDescription,
                Amount = row.Amount,
                IsDebit = row.IsDebit,
                Category = category,
                IsRuleApplied = false,
            };

            if (importer.ApplyRules)
                await dataService.ApplyRuleAsync(transaction, ctx);

            transactions.Add(transaction);
        }

        if (transactions.Count > 0)
        {
            ctx.Transactions.AddRange(transactions);
            await ctx.SaveChangesAsync();
        }

        return new ImportResult
        {
            ImportedCount = transactions.Count,
            SkippedCount = total - transactions.Count,
            TotalCount = total,
            BankType = importer.BankType,
        };
    }

    /// <summary>
    /// Retrieves or creates an account based on the provided name, using
    /// <paramref name="accountCache"/> for in-call memoization.
    /// </summary>
    private async Task<Account?> GetAccountAsync(
        string? name,
        DataContext ctx,
        Dictionary<string, Account> accountCache,
        bool isCreateAccount = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (accountCache.TryGetValue(name, out var existingAccount))
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
            accountCache.Add(name, accountInDB);
            return accountInDB;
        }

        if (isCreateAccount)
        {
            var account = new Account { Name = name, ShownName = name };
            ctx.Accounts.Add(account);
            await ctx.SaveChangesAsync();
            accountCache.Add(name, account);
            return account;
        }

        return null;
    }

    /// <summary>
    /// Retrieves or creates a category based on the provided name, using
    /// <paramref name="categoryCache"/> for in-call memoization.
    /// </summary>
    private async Task<Category?> GetCategoryAsync(
        string? name,
        DataContext ctx,
        Dictionary<string, Category> categoryCache)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (categoryCache.TryGetValue(name, out var existingCategory))
            return existingCategory;

        var categoryInDB = await ctx.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (categoryInDB != null)
        {
            categoryCache.Add(name, categoryInDB);
            return categoryInDB;
        }

        var category = new Category { Name = name, IsNew = true };
        ctx.Categories.Add(category);
        await ctx.SaveChangesAsync();
        categoryCache.Add(name, category);
        return category;
    }

    /// <summary>
    /// Retrieves the default "Uncategorized" category, creating it if necessary.
    /// </summary>
    private async Task<Category?> GetDefaultCategoryAsync(DataContext ctx, Dictionary<string, Category> categoryCache)
        => await GetCategoryAsync("Uncategorized", ctx, categoryCache);

    /// <summary>
    /// Checks if a transaction already exists to prevent duplicates during import.
    /// </summary>
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

