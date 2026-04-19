using System.Globalization;
using CsvHelper;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Model.Import;

namespace MoneyManager.Api.Services;

/// <content>
/// Contains methods for importing transactions from RBC (Royal Bank of Canada) CSV files.
/// </content>
public partial class TransactionService
{
    /// <summary>
    /// Validates that the RBC CSV stream has the expected column structure.
    /// </summary>
    /// <param name="stream">The CSV stream to validate. Position is reset to 0 after validation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stream is empty, missing a header row, or expected columns are absent.
    /// </exception>
    /// <remarks>
    /// Expected columns: Account Type, Account Number, Transaction Date, Cheque Number,
    /// Description 1, Description 2, CAD$, USD$.
    /// </remarks>
    private static void ValidateRbcCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var headerLine = reader.ReadLine();
        stream.Position = 0;

        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new InvalidOperationException("RBC CSV file is empty or missing header row.");
        }

        var expectedColumns = new[] { "Account Type", "Account Number", "Transaction Date", "Cheque Number", "Description 1", "Description 2", "CAD$", "USD$" };
        var actualColumns = headerLine.Split(',').Select(c => c.Trim().Replace("\"", "")).ToArray();

        foreach (var expected in expectedColumns)
        {
            if (!actualColumns.Contains(expected))
            {
                throw new InvalidOperationException($"RBC CSV file does not have the expected structure. Missing column: '{expected}'. Expected columns: {string.Join(", ", expectedColumns)}");
            }
        }
    }

    /// <summary>
    /// Imports transactions from an RBC CSV stream into the database.
    /// </summary>
    /// <param name="csvStream">A readable stream containing the RBC CSV data.</param>
    /// <param name="isCreateAccounts">If <c>true</c>, creates new accounts for unknown account numbers. If <c>false</c>, skips those transactions.</param>
    /// <returns>
    /// An <see cref="ImportResult"/> with the number of imported, skipped, and total records.
    /// </returns>
    /// <remarks>
    /// <para><b>RBC CSV columns used:</b> Account Number, Transaction Date, Description 1, Description 2, CAD$.</para>
    /// <para><b>Debit logic:</b> Negative CAD$ amount → IsDebit = true. Amount is stored as absolute value.</para>
    /// <para><b>Category:</b> All transactions default to "Uncategorized"; auto-categorization rules are applied.</para>
    /// <para><b>Description:</b> Description 1 and Description 2 are concatenated with a space.</para>
    /// <para><b>Duplicate detection:</b> Exact date matching (no fuzzy matching).</para>
    /// <para>A database backup is created before import. All transactions are batch-saved in a single operation.</para>
    ///
    /// Thread Safety: This method is not thread-safe. Run imports sequentially.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when CSV structure is invalid.</exception>
    public async Task<ImportResult> ImportRbcCsvAsync(Stream csvStream, bool isCreateAccounts = true)
    {
        ValidateRbcCsv(csvStream);

        await dbService.BackupAsync();

        _accounts = [];
        _categories = [];

        var total = CountStreamLines(csvStream, hasHeader: true);

        var context = await contextFactory.CreateDbContextAsync();
        var uCategory = await GetDefaultCategoryAsync(context);

        var transactions = new List<Transaction>();
        var skipped = 0;

        using (var reader = new StreamReader(csvStream, leaveOpen: true))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var record = new RBCCSV();
            var records = csv.EnumerateRecords(record);
            foreach (var r in records)
            {
                var account = await GetAccountAsync(r.AccountNumber, context, isCreateAccounts);

                if (account == null)
                {
                    skipped++;
                    continue;
                }
                if (r.AmountCAD == null)
                {
                    skipped++;
                    continue;
                }
                var isDebit = r.AmountCAD < 0;
                var amount = Math.Abs(r.AmountCAD.Value);
                var description = $"{r.Description1} {r.Description2}";

                var isExist = IsTransactionExists(r.Date, amount, isDebit, description, account, context);
                if (isExist)
                {
                    skipped++;
                    continue;
                }

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

                await dataService.ApplyRuleAsync(transaction, context);
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
            BankType = "RBC"
        };
    }
}
