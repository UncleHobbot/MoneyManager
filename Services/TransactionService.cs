using System.Globalization;
using CsvHelper;
using MoneyManager.Model.Import;

namespace MoneyManager.Services;

public class TransactionService(IDbContextFactory<DataContext> contextFactory)
{
    public async Task ImportMintCSV(string filePath, Action<int> progress)
    {
        // init global cache
        Accounts = [];
        Categories = [];
        // calculate Count
        var total = File.ReadLines(filePath).Count() - 1;

        var context = contextFactory.CreateDbContext();
        var transactions = new List<Transaction>();
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var current = 0;
            var lastReportedProgress = 0;
            var dateLimit = new DateTime(2023, 1, 1);

            var record = new MintCSV();
            var records = csv.EnumerateRecords(record);
            foreach (var r in records)
            {
                current++;
                var p= current * 100 / total;
                if (p > lastReportedProgress)
                {
                    lastReportedProgress = p;
                    progress(p);
                }

                Console.WriteLine($"{current}/{total} - {p}: {r.Date}");

                if (r.Date < dateLimit)
                    continue;

                var account = await GetAccount(r.AccountName, context);
                var category = await GetCategory(r.Category, context);
                var isDebit = r.TransactionType == "debit";

                var isExist = IsTransactionExists(r.Date, r.Amount, isDebit, r.OriginalDescription, account, context);
                if (isExist)
                    continue;

                var transaction = new Transaction
                {
                    Account = account,
                    Date = r.Date,
                    Description = r.Description,
                    OriginalDescription = r.OriginalDescription,
                    Amount = r.Amount,
                    IsDebit = isDebit,
                    Category = category,
                    IsRuleApplied = false
                };
                transactions.Add(transaction);
            }
        }

        if (transactions.Any())
        {
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();
        }
    }

    private Dictionary<string, Account> Accounts { get; set; } = [];
    private Dictionary<string, Category> Categories { get; set; } = [];

    private async Task<Account> GetAccount(string name, DataContext ctx)
    {
        if (Accounts.TryGetValue(name, out var existingAccount))
            return existingAccount;

        var accountInDB = await ctx.Accounts.FirstOrDefaultAsync(c => c.Name == name);
        if (accountInDB != null)
        {
            Accounts.Add(name, accountInDB);
            return accountInDB;
        }

        var account = new Account { Name = name, ShownName = name };
        ctx.Accounts.Add(account);
        await ctx.SaveChangesAsync();
        Accounts.Add(name, account);
        return account;
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

    private bool IsTransactionExists(DateTime date, decimal amount, bool isDebit, string originalDescription, Account account, DataContext ctx)
        => ctx.Transactions.Any(t => t.Date == date && t.Amount == amount && t.IsDebit == isDebit
                                     && t.Account.Id == account.Id && t.OriginalDescription == originalDescription);
}