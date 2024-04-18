using System.Globalization;
using CsvHelper;
using MoneyManager.Model.Import;

namespace MoneyManager.Services;

public partial class TransactionService
{
    public async Task<int> ImportRBCCSV(string filePath, bool isCreateAccounts, Action<int> progress)
    {
        Backup();
        // init global cache
        Accounts = [];
        Categories = [];
        // calculate Count
        var total = File.ReadLines(filePath).Count() - 1;

        var context = await contextFactory.CreateDbContextAsync();
        var uCategory = await GetDefaultCategory(context);

        var transactions = new List<Transaction>();
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var current = 0;
            var lastReportedProgress = 0;

            var record = new RBCCSV();
            var records = csv.EnumerateRecords(record);
            foreach (var r in records)
            {
                current++;
                var p = current * 100 / total;
                if (p > lastReportedProgress)
                {
                    lastReportedProgress = p;
                    progress(p);
                }

                //Console.WriteLine($"{current}/{total} - {p}: {r.Date}");

                var account = await GetAccount(r.AccountNumber, context, isCreateAccounts);

                if (account == null)
                    continue;
                if (r.AmountCAD == null)
                    continue;
                var isDebit = r.AmountCAD < 0;
                var amount = Math.Abs(r.AmountCAD.Value);
                var description = $"{r.Description1} {r.Description2}";

                var isExist = IsTransactionExists(r.Date, amount, isDebit, description, account, context);
                if (isExist)
                    continue;

                var transaction = new Transaction
                {
                    Account = account,
                    Date = r.Date,
                    Description = description.Trim(),
                    OriginalDescription = description.Trim(),
                    Amount = amount,
                    IsDebit = isDebit,
                    Category = uCategory,
                    IsRuleApplied = false
                };

                await dataService.ApplyRule(transaction, context);
                transactions.Add(transaction);
            }
            reader.Close();
        }
        
        if (transactions.Any())
        {
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();
        }

        var folder = Path.GetDirectoryName(filePath);
        var file = Path.GetFileName(filePath);
        var importedFolder = Path.Combine(folder, "Imported");
        if (!Directory.Exists(importedFolder))
            Directory.CreateDirectory(importedFolder);
        File.Copy(filePath, Path.Combine(importedFolder, file), true);
        File.Delete(filePath);
        return transactions.Count;
    }
}