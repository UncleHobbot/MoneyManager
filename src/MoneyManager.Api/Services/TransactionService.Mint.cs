using System.Globalization;
using CsvHelper;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Model.Import;

namespace MoneyManager.Api.Services;

/// <content>
/// Contains methods for importing transactions from Mint.com CSV files.
/// </content>
public partial class TransactionService
{
    /// <summary>
    /// Imports transactions from a Mint.com CSV stream into the database.
    /// </summary>
    /// <param name="csvStream">A readable stream containing the Mint.com CSV data.</param>
    /// <param name="isCreateAccounts">If <c>true</c>, creates new accounts for unknown account names.</param>
    /// <returns>
    /// An <see cref="ImportResult"/> with the number of imported, skipped, and total records.
    /// </returns>
    /// <remarks>
    /// <para><b>Mint.com CSV columns:</b> Date, Description, Original Description, Amount, Transaction Type, Account Name, Category.</para>
    /// <para><b>Debit logic:</b> TransactionType == "debit" → IsDebit = true.</para>
    /// <para><b>Duplicate detection:</b> Exact date matching (no fuzzy matching).</para>
    /// <para><b>Category handling:</b> Categories from the Mint CSV are resolved or auto-created.</para>
    /// <para>A database backup is created before import. All transactions are batch-saved in a single operation.</para>
    ///
    /// Thread Safety: This method is not thread-safe. Run imports sequentially.
    /// </remarks>
    public async Task<ImportResult> ImportMintCsvAsync(Stream csvStream, bool isCreateAccounts = true)
    {
        await dbService.BackupAsync();

        _accounts = [];
        _categories = [];

        var total = CountStreamLines(csvStream, hasHeader: true);

        var context = await contextFactory.CreateDbContextAsync();
        var transactions = new List<Transaction>();
        var skipped = 0;

        using (var reader = new StreamReader(csvStream, leaveOpen: true))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var record = new MintCSV();
            var records = csv.EnumerateRecords(record);
            foreach (var r in records)
            {
                var account = await GetAccountAsync(r.AccountName, context, isCreateAccounts);

                if (account == null)
                {
                    skipped++;
                    continue;
                }

                var category = await GetCategoryAsync(r.Category, context);
                var isDebit = r.TransactionType == "debit";

                var isExist = IsTransactionExists(r.Date, r.Amount, isDebit, r.OriginalDescription, account, context);
                if (isExist)
                {
                    skipped++;
                    continue;
                }

                var transaction = new Transaction
                {
                    Account = account,
                    Date = r.Date,
                    Description = r.Description ?? string.Empty,
                    OriginalDescription = r.OriginalDescription ?? string.Empty,
                    Amount = r.Amount,
                    IsDebit = isDebit,
                    Category = category,
                    IsRuleApplied = false
                };
                transactions.Add(transaction);
            }
        }

        if (transactions.Count > 0)
        {
            context.Transactions.AddRange(transactions);
            await context.SaveChangesAsync();
        }

        return new ImportResult
        {
            ImportedCount = transactions.Count,
            SkippedCount = skipped,
            TotalCount = total,
            BankType = "Mint"
        };
    }
}
