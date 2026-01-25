using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MoneyManager.Model.Import;

namespace MoneyManager.Services;

public partial class TransactionService
{
    private void ValidateCIBCCSV(string filePath)
    {
        var lines = File.ReadLines(filePath).Take(5).ToList();
        if (!lines.Any())
        {
            throw new InvalidOperationException("CIBC CSV file is empty.");
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
        try
        {
            using var reader = new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines))));
            using var csv = new CsvReader(reader, config);

            var record = new CIBCCSV();
            var records = csv.EnumerateRecords(record);
            var hasValidRecord = false;

            foreach (var r in records)
            {
                if (r.Date != default && !string.IsNullOrWhiteSpace(r.Description) && !string.IsNullOrWhiteSpace(r.AccountNumber))
                {
                    hasValidRecord = true;
                    break;
                }
            }

            if (!hasValidRecord)
            {
                throw new InvalidOperationException("CIBC CSV file does not have the expected structure. Unable to find valid records with Date, Description, and Account Number.");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("CIBC CSV file does not have the expected structure. Expected format: Date (index 0), Description (index 1), AmountDebit (index 2), AmountCredit (index 3), AccountNumber (index 4).", ex);
        }
    }

    public async Task<int> ImportCIBCCSV(string filePath, bool isCreateAccounts, Action<int> progress)
    {
        ValidateCIBCCSV(filePath);
        
        await dbService.Backup();
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
                var p = total == 0 ? 0 : current * 100 / total;
                if (p > lastReportedProgress)
                {
                    lastReportedProgress = p;
                    progress(p);
                }

                //Console.WriteLine($"{current}/{total} - {p}: {r.Date}");

                if (r.Date < dateLimit)
                    continue;

                var account = await GetAccount(r.AccountNumber, context, isCreateAccounts);

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

            reader.Close();
        }

        if (transactions.Any())
        {
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();
        }

        var folder = Path.GetDirectoryName(filePath);
        var file = Path.GetFileName(filePath);
        if (folder != null)
        {
            var importedFolder = Path.Combine(folder, "Imported");
            if (!Directory.Exists(importedFolder))
                Directory.CreateDirectory(importedFolder);
            File.Copy(filePath, Path.Combine(importedFolder, file), true);
        }

        File.Delete(filePath);
        return transactions.Count;
    }
}