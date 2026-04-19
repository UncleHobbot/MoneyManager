using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Model.Import;

namespace MoneyManager.Api.Services;

/// <content>
/// Contains methods for importing transactions from CIBC (Canadian Imperial Bank of Commerce) CSV files.
/// </content>
public partial class TransactionService
{
    /// <summary>
    /// Validates that the CIBC CSV stream has the expected structure and contains valid records.
    /// </summary>
    /// <param name="stream">The CSV stream to validate. Position is reset to 0 after validation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the stream is empty, contains no valid records, or cannot be parsed.
    /// </exception>
    /// <remarks>
    /// CIBC CSV files have no header row. Validation reads the first 5 lines and checks for at least
    /// one record with a non-default Date, non-empty Description, and non-empty AccountNumber.
    /// </remarks>
    private static void ValidateCibcCsv(Stream stream)
    {
        using var lineReader = new StreamReader(stream, leaveOpen: true);
        var lines = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var line = lineReader.ReadLine();
            if (line == null) break;
            lines.Add(line);
        }
        stream.Position = 0;

        if (lines.Count == 0)
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

    /// <summary>
    /// Imports transactions from a CIBC CSV stream into the database.
    /// </summary>
    /// <param name="csvStream">A readable stream containing the CIBC CSV data.</param>
    /// <param name="isCreateAccounts">If <c>true</c>, creates new accounts for unknown account numbers. If <c>false</c>, skips those transactions.</param>
    /// <returns>
    /// An <see cref="ImportResult"/> with the number of imported, skipped, and total records.
    /// </returns>
    /// <remarks>
    /// <para><b>CIBC CSV format:</b> No header row. Columns by index: Date (0), Description (1), AmountDebit (2), AmountCredit (3), AccountNumber (4).</para>
    /// <para><b>Debit logic:</b> If AmountDebit has a value → IsDebit = true. Otherwise AmountCredit is used.</para>
    /// <para><b>Category:</b> All transactions default to "Uncategorized"; auto-categorization rules are applied.</para>
    /// <para><b>Duplicate detection:</b> Uses fuzzy date matching (±5 days) because CIBC posting dates can vary.</para>
    /// <para>A database backup is created before import. All transactions are batch-saved in a single operation.</para>
    ///
    /// Thread Safety: This method is not thread-safe. Run imports sequentially.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when CSV structure is invalid.</exception>
    public async Task<ImportResult> ImportCibcCsvAsync(Stream csvStream, bool isCreateAccounts = true)
    {
        ValidateCibcCsv(csvStream);

        await dbService.BackupAsync();

        _accounts = [];
        _categories = [];

        var total = CountStreamLines(csvStream, hasHeader: false);

        var context = await contextFactory.CreateDbContextAsync();
        var uCategory = await GetDefaultCategoryAsync(context);

        var transactions = new List<Transaction>();
        var skipped = 0;
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };

        using (var reader = new StreamReader(csvStream, leaveOpen: true))
        using (var csv = new CsvReader(reader, config))
        {
            var record = new CIBCCSV();
            var records = csv.EnumerateRecords(record);
            foreach (var r in records)
            {
                var account = await GetAccountAsync(r.AccountNumber, context, isCreateAccounts);

                if (account == null)
                {
                    skipped++;
                    continue;
                }
                var isDebit = r.AmountDebit.HasValue;
                var amount = r.AmountDebit ?? r.AmountCredit ?? 0;

                var isExist = IsTransactionExists(r.Date, amount, isDebit, r.Description, account, context, true);
                if (isExist)
                {
                    skipped++;
                    continue;
                }

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
            BankType = "CIBC"
        };
    }
}
