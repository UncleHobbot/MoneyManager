namespace MoneyManager.Services;

public partial class TransactionService(IDbContextFactory<DataContext> contextFactory, DataService dataService)
{
    private Dictionary<string, Account> Accounts { get; set; } = [];
    private Dictionary<string, Category> Categories { get; set; } = [];

    private async Task<Account> GetAccount(string name, DataContext ctx, bool isCreateAccount = true)
    {
        if (Accounts.TryGetValue(name, out var existingAccount))
            return existingAccount;

        var accountInDB = await ctx.Accounts.FirstOrDefaultAsync(c => c.Name == name || c.Number == name.Replace("-", "")
                                                                                     || c.AlternativeName1.ToUpper() == name.ToUpper() || c.AlternativeName2.ToUpper() == name.ToUpper());
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

    private async Task<Category> GetCategory(string name, DataContext ctx)
    {
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

    private async Task<Category> GetDefaultCategory(DataContext ctx) => await GetCategory("Uncategorized", ctx);

    private bool IsTransactionExists(DateTime date, decimal amount, bool isDebit, string originalDescription, Account account, DataContext ctx, bool isDateFuzzy = false)
    {
        if (isDateFuzzy)
        {
            var lowDate = date.AddDays(-5);
            var highDate = date.AddDays(5);
            return ctx.Transactions.Any(t => (t.Date >= lowDate && t.Date <= highDate)
                                             && t.Amount == amount && t.IsDebit == isDebit
                                             && t.Account.Id == account.Id && (t.OriginalDescription.Trim() == originalDescription.Trim() || originalDescription.Contains(t.OriginalDescription.Trim())));
        }

        return ctx.Transactions.Any(t => t.Date == date && t.Amount == amount && t.IsDebit == isDebit
                                         && t.Account.Id == account.Id && (t.OriginalDescription.Trim() == originalDescription.Trim() || originalDescription.Contains(t.OriginalDescription.Trim())));
    }
}