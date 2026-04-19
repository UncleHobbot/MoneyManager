namespace MoneyManager.Services;

/// <summary>
/// Provides functionality for importing transactions from various sources (CSV files from different banks) into the MoneyManager database.
/// </summary>
/// <remarks>
/// This service is designed to handle CSV imports from multiple banks (Mint.com, RBC, CIBC) with format-specific parsing logic.
/// 
/// Key features:
/// - Automatic account and category resolution
/// - Duplicate transaction detection to prevent re-importing
/// - Fuzzy matching for dates during duplicate detection
/// - Automatic account creation for unknown accounts
/// - Automatic category creation with IsNew flag for unknown categories
/// - In-memory caching of accounts and categories for performance
/// 
/// The service uses partial classes to separate bank-specific import logic into separate files.
/// 
/// Thread Safety: This service is not thread-safe. Each import operation should create a new instance or run sequentially.
/// </remarks>
public partial class TransactionService(
    IDbContextFactory<DataContext> contextFactory,
    DataService dataService,
    DBService dbService)
{
    /// <summary>
    /// Gets or sets the cached dictionary of accounts keyed by name for fast lookup during import.
    /// </summary>
    /// <value>
    /// A dictionary mapping account names to Account objects.
    /// This cache is populated during import to avoid repeated database queries.
    /// </value>
    /// <remarks>
    /// This is an in-memory cache that should be cleared between import operations.
    /// The cache improves performance by avoiding repeated database lookups for the same account.
    /// </remarks>
    private Dictionary<string, Account> Accounts { get; set; } = [];

    /// <summary>
    /// Gets or sets the cached dictionary of categories keyed by name for fast lookup during import.
    /// </summary>
    /// <value>
    /// A dictionary mapping category names to Category objects.
    /// This cache is populated during import to avoid repeated database queries.
    /// </value>
    /// <remarks>
    /// This is an in-memory cache that should be cleared between import operations.
    /// The cache improves performance by avoiding repeated database lookups for the same category.
    /// </remarks>
    private Dictionary<string, Category> Categories { get; set; } = [];

    /// <summary>
    /// Retrieves or creates an account based on the provided name.
    /// </summary>
    /// <param name="name">The account name to look up or create. Can be null or whitespace.</param>
    /// <param name="ctx">The database context to use for operations.</param>
    /// <param name="isCreateAccount">If true, creates a new account if one is not found. If false, returns null when account is not found.</param>
    /// <returns>
    /// The found or created Account object, or null if name is empty/whitespace or account not found and isCreateAccount is false.
    /// </returns>
    /// <remarks>
    /// This method uses multiple matching strategies:
    /// 
    /// 1. **Cache Lookup**: First checks if account is already in the in-memory cache
    /// 2. **Exact Name Match**: Looks for account with matching Name property
    /// 3. **Account Number Match**: Looks for account with matching Number property (dashes removed from input)
    /// 4. **Alternative Name Matches**: Checks up to 5 alternative name fields with case-insensitive comparison
    /// 
    /// If account is not found and isCreateAccount is true:
    /// - Creates a new Account with Name and ShownName set to the input name
    /// - Adds the account to the database and saves changes
    /// - Adds the account to the in-memory cache for future lookups
    /// 
    /// This flexible matching allows the import to work with various account naming conventions from different banks.
    /// </remarks>
    private async Task<Account?> GetAccount(string? name, DataContext ctx, bool isCreateAccount = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (Accounts.TryGetValue(name, out var existingAccount))
            return existingAccount;

        var accountInDB = await ctx.Accounts.FirstOrDefaultAsync(c => c.Name == name
                                                                      || c.Number == name.Replace("-", "")
                                                                      || (c.AlternativeName1 != null &&
                                                                          c.AlternativeName1.ToUpper() ==
                                                                          name.ToUpper())
                                                                      || (c.AlternativeName2 != null &&
                                                                          c.AlternativeName2.ToUpper() ==
                                                                          name.ToUpper())
                                                                      || (c.AlternativeName3 != null &&
                                                                          c.AlternativeName3.ToUpper() ==
                                                                          name.ToUpper())
                                                                      || (c.AlternativeName4 != null &&
                                                                          c.AlternativeName4.ToUpper() ==
                                                                          name.ToUpper())
                                                                      || (c.AlternativeName5 != null &&
                                                                          c.AlternativeName5.ToUpper() ==
                                                                          name.ToUpper()));
        if (accountInDB != null)
        {
            Accounts.Add(name, accountInDB);
            return accountInDB;
        }

        if (isCreateAccount)
        {
            var account = new Account { Name = name, ShownName = name };
            ctx.Accounts.Add(account);
            await ctx.SaveChangesAsync();
            Accounts.Add(name, account);
            return account;
        }

        return null;
    }

    /// <summary>
    /// Retrieves or creates a category based on the provided name.
    /// </summary>
    /// <param name="name">The category name to look up or create. Can be null or whitespace.</param>
    /// <param name="ctx">The database context to use for operations.</param>
    /// <returns>
    /// The found or created Category object, or null if name is empty or whitespace.
    /// </returns>
    /// <remarks>
    /// This method follows a similar pattern to GetAccount:
    /// 
    /// 1. **Cache Lookup**: First checks if category is already in the in-memory cache
    /// 2. **Exact Name Match**: Looks for category with matching Name property
    /// 
    /// If category is not found:
    /// - Creates a new Category with Name set to the input name and IsNew set to true
    /// - Adds the category to the database and saves changes
    /// - Adds the category to the in-memory cache for future lookups
    /// 
    /// The IsNew flag indicates that this category was automatically created during import
    /// and should be reviewed by the user. These categories can be filtered in the UI.
    /// 
    /// Unlike accounts, categories are always created if not found (no isCreate parameter).
    /// This ensures all transactions have a category assignment, even if it's a generic one.
    /// </remarks>
    private async Task<Category?> GetCategory(string? name, DataContext ctx)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (Categories.TryGetValue(name, out var existingCategory))
            return existingCategory;

        var categoryInDB = await ctx.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (categoryInDB != null)
        {
            Categories.Add(name, categoryInDB);
            return categoryInDB;
        }

        var category = new Category { Name = name, IsNew = true };
        ctx.Categories.Add(category);
        await ctx.SaveChangesAsync();
        Categories.Add(name, category);
        return category;
    }

    /// <summary>
    /// Retrieves the default "Uncategorized" category from the database.
    /// </summary>
    /// <param name="ctx">The database context to use for operations.</param>
    /// <returns>
    /// The Category object named "Uncategorized", or a newly created one if it doesn't exist.
    /// </returns>
    /// <remarks>
    /// This is a convenience method that wraps GetCategory with the fixed name "Uncategorized".
    /// It's used as a fallback when a transaction doesn't have a category in the import file.
    /// </remarks>
    private async Task<Category?> GetDefaultCategory(DataContext ctx) => await GetCategory("Uncategorized", ctx);

    /// <summary>
    /// Checks if a transaction already exists in the database to prevent duplicates during import.
    /// </summary>
    /// <param name="date">The transaction date.</param>
    /// <param name="amount">The transaction amount.</param>
    /// <param name="isDebit">Whether the transaction is a debit (true) or credit (false).</param>
    /// <param name="originalDescription">The original description from the import file.</param>
    /// <param name="account">The account the transaction belongs to.</param>
    /// <param name="ctx">The database context to use for queries.</param>
    /// <param name="isDateFuzzy">If true, allows a ±5 day window for date matching. If false, requires exact date match.</param>
    /// <returns>
     /// True if a matching transaction exists in the database, false otherwise.
     /// </returns>
     /// <remarks>
     /// This method prevents duplicate transactions from being imported multiple times.
     /// 
     /// Exact Matching (isDateFuzzy = false):
     /// - Transaction must match on: exact date, exact amount, debit/credit type, account, and description
     /// - Description match is checked two ways:
     ///   1. Exact match (trimmed)
     ///   2. Original description from import contains existing description (trimmed)
     /// 
     /// Fuzzy Matching (isDateFuzzy = true):
     /// - Allows date variance of ±5 days from the provided date
     /// - Useful when bank export dates don't perfectly align (e.g., posting dates vs transaction dates)
     /// - All other criteria match exactly
    /// 
    /// The description matching uses two strategies to handle cases where:
    /// - The import file may have a longer description that contains the existing description
    /// - The existing description may have been modified after initial import
    /// 
    /// This method is called during import to skip transactions that have already been imported.
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
                                                               && t.Account.Id == account.Id && (t.OriginalDescription.Trim() == originalDescription.Trim() || originalDescription.Contains(t.OriginalDescription.Trim())));
        }

        return ctx.Transactions.Any(t => t.Date == date && t.Amount == amount && t.IsDebit == isDebit
                                         && t.Account.Id == account.Id &&
                                         (t.OriginalDescription.Trim() == originalDescription.Trim() ||
                                          originalDescription.Contains(t.OriginalDescription.Trim())));
    }
}
