using System.Globalization;
using CsvHelper;
using MoneyManager.Model.Import;

namespace MoneyManager.Services;

/// <content>
/// Contains methods for importing transactions from Mint.com CSV files.
/// </content>
public partial class TransactionService
{
    /// <summary>
    /// Imports transactions from a Mint.com CSV export file into the MoneyManager database.
    /// </summary>
    /// <param name="filePath">The full path to the Mint.com CSV file to import.</param>
    /// <param name="progress">An action callback for reporting import progress (0-100%).</param>
    /// <returns>
    /// The number of transactions successfully imported from the file.
    /// </returns>
    /// <remarks>
    /// This method performs a complete import workflow:
    /// 
    /// **Preparation:**
    /// 1. Creates a database backup before making changes
    /// 2. Initializes in-memory caches for accounts and categories
    /// 3. Calculates total records for progress reporting
    /// 
    /// **CSV Processing:**
    /// 4. Opens and reads the CSV file using CsvHelper
    /// 5. Uses invariant culture for consistent number/date parsing
    /// 6. Iterates through each record:
    ///    - Reports progress at least every 1% increment
    ///    - Logs record information to console for debugging
    ///    - Skips records dated before January 1, 2023 (historical cutoff)
    /// 
    /// **Transaction Processing:**
    /// 7. Resolves account from AccountName (creates if needed)
    /// 8. Resolves category from Category (creates with IsNew if needed)
    /// 9. Determines transaction type: "debit" = true, anything else = false
    /// 10. Checks for duplicates using IsTransactionExists
    /// 11. Creates new Transaction entity if not duplicate
    /// 
    /// **Database Operations:**
    /// 12. Adds all new transactions to database context
    /// 13. Saves changes in a single transaction
    /// 
    /// **File Management:**
    /// 14. Creates "Imported" subfolder in the source file's directory
    /// 15. Moves the imported file to the "Imported" folder
    /// 16. Deletes the original file
    /// 
    /// **Mint.com CSV Format Expected:**
    /// - Date column (parsed to DateTime)
    /// - Description column (display description)
    /// - Original Description column (original description from bank)
    /// - Amount column (decimal value)
    /// - Transaction Type column ("debit" or other)
    /// - Account Name column (account identifier)
    /// - Category column (category name)
    /// 
    /// **Date Limit:**
    /// - Only processes transactions on or after January 1, 2023
    /// - Older transactions are skipped silently
    /// - This prevents importing very old historical data
    /// 
    /// **Duplicate Detection:**
    /// - Uses exact date matching (no fuzzy matching)
    /// - Compares: date, amount, isDebit, account, description
    /// 
    /// **Progress Reporting:**
    /// - Calls progress callback with percentage (0-100)
    /// - Updates callback at least every 1% increment
    /// - Allows UI to show progress bar or indicator
    /// 
    /// **File Operations:**
    /// - Creates "Imported" folder if it doesn't exist
    /// - Overwrites existing files in "Imported" folder with same name
    /// - Deletes original file after successful import
    /// 
    /// **Error Handling:**
    /// - Invalid CSV format will throw CsvHelper exceptions
    /// - Invalid date format will cause parsing errors
    /// - File access errors may occur if file is locked
    /// - Database errors may occur on constraint violations
    /// 
    /// Thread Safety: This method is not thread-safe. Run imports sequentially.
    /// 
    /// Performance: All transactions are batch-added and saved in a single database transaction for efficiency.
    /// </remarks>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="IOException">Thrown when file cannot be read or moved.</exception>
    /// <exception cref="Exception">Thrown for database operation failures.</exception>
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
