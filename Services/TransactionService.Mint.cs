﻿using System.Globalization;
using CsvHelper;
using MoneyManager.Model.Import;

namespace MoneyManager.Services;

public partial class TransactionService
{
    public async Task<int> ImportMintCSV(string filePath, Action<int> progress)
    {
        await dbService.Backup();

        // init global cache
        Accounts = [];
        Categories = [];
        // calculate Count
        var total = File.ReadLines(filePath).Count() - 1;

        var context = await contextFactory.CreateDbContextAsync();
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
                var p = current * 100 / total;
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