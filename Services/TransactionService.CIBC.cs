﻿using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MoneyManager.Model.Import;

namespace MoneyManager.Services;

public partial class TransactionService
{
    public async Task ImportCIBCCSV(string filePath, Action<int> progress)
    {
        // init global cache
        Accounts = [];
        Categories = [];
        // calculate Count
        var total = File.ReadLines(filePath).Count() - 1;

        var context = await contextFactory.CreateDbContextAsync();
        var uCategory = await GetDefaultCategory(context);

        var transactions = new List<Transaction>();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, config))
        {
            var current = 0;
            var lastReportedProgress = 0;
            // Limit the date to January 1st 2024 and older
            var dateLimit = new DateTime(2024, 1, 1);

            var record = new CIBCCSV();
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

                if (r.Date < dateLimit)
                    continue;

                var account = await GetAccount(r.AccountNumber, context, false);

                if (account == null)
                    continue;
                var isDebit = r.AmountDebit.HasValue;
                var amount = r.AmountDebit ?? r.AmountCredit ?? 0;

                var isExist = IsTransactionExists(r.Date, amount, isDebit, r.Description, account, context, true);
                if (isExist)
                    continue;

                var transaction = new Transaction
                {
                    Account = account,
                    Date = r.Date,
                    Description = r.Description,
                    OriginalDescription = r.Description,
                    Amount = amount,
                    IsDebit = isDebit,
                    Category = uCategory,
                    IsRuleApplied = false
                };

                await dataService.ApplyRule(transaction, context);
                transactions.Add(transaction);
            }
        }

        if (transactions.Any())
        {
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();
        }
    }
}