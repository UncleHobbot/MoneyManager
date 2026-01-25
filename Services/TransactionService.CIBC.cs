using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MoneyManager.Model.Import;

namespace MoneyManager.Services;

/// <content>
/// Contains methods for importing transactions from CIBC (Canadian Imperial Bank of Commerce) CSV files.
/// </content>
public partial class TransactionService
{
    /// <summary>
    /// Validates that the CIBC CSV file has the expected structure and contains valid records.
    /// </summary>
    /// <param name="filePath">The full path to the CIBC CSV file to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// - File is empty
    /// - File does not have valid records with Date, Description, and Account Number
    /// - CSV parsing fails
    /// </exception>
    /// <remarks>
    /// This method performs structural validation before attempting import:
    /// 
    /// **Validation Steps:**
    /// 1. Reads the first 5 lines of the CSV file
    /// 2. Checks if file is empty (no lines)
    /// 3. Configures CsvHelper with invariant culture and no headers
    /// 4. Converts lines to memory stream for parsing
    /// 5. Parses records using CIBCCSV mapping
    /// 6. Checks for at least one valid record
    /// 
    /// **Valid Record Criteria:**
    /// A record is valid if it has:
    /// - Non-default Date value
    /// - Non-empty Description
    /// - Non-empty Account Number
    /// 
    /// **CIBC CSV Format Expected:**
    /// - Column 0 (index 0): Date
    /// - Column 1 (index 1): Description
    /// - Column 2 (index 2): Amount Debit
    /// - Column 3 (index 3): Amount Credit
    /// - Column 4 (index 4): Account Number
    /// 
    /// **Note:** CIBC CSV files do NOT have a header row.
    /// The first line contains data, not column names.
    /// 
    /// **Error Messages:**
    /// - "CIBC CSV file is empty."
    /// - "CIBC CSV file does not have the expected structure. Unable to find valid records with Date, Description, and Account Number."
    /// - "CIBC CSV file does not have the expected structure. Expected format: Date (index 0), Description (index 1), AmountDebit (index 2), AmountCredit (index 3), AccountNumber (index 4)."
    /// 
    /// This validation prevents processing malformed CSV files and provides clear error messages.
    /// It only checks the first 5 lines for efficiency.
    /// 
    /// Note: This method only checks data presence, not data validity.
    /// Invalid dates or amounts will be caught during import.
    /// </remarks>
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

    /// <summary>
    /// Imports transactions from a CIBC CSV export file into the MoneyManager database.
    /// </summary>
    /// <param name="filePath">The full path to the CIBC CSV file to import.</param>
    /// <param name="isCreateAccounts">If true, creates new accounts for unknown account numbers. If false, skips transactions with unknown accounts.</param>
    /// <param name="progress">An action callback for reporting import progress (0-100%).</param>
    /// <returns>
    /// The number of transactions successfully imported from the file.
    /// </returns>
    /// <remarks>
    /// This method performs a complete CIBC-specific import workflow:
    /// 
    /// **Validation:**
    /// 1. Validates CSV structure using ValidateCIBCCSV
    /// 
    /// **Preparation:**
    /// 2. Creates a database backup before making changes
    /// 3. Initializes in-memory caches for accounts and categories
    /// 4. Calculates total records for progress reporting
    /// 5. Resolves default "Uncategorized" category
    /// 6. Configures CsvHelper with no headers (CIBC format)
    /// 
    /// **CSV Processing:**
    /// 7. Opens and reads the CSV file using CsvHelper
    /// 8. Uses invariant culture for consistent number/date parsing
    /// 9. Iterates through each record:
    ///    - Reports progress at least every 1% increment
    ///    - Skips records dated before January 1, 2024
    ///    - Resolves account from AccountNumber (creates if isCreateAccounts)
    ///    - Skips if account is null and isCreateAccounts is false
    ///    - Determines if debit: AmountDebit has value
    ///    - Uses AmountDebit if present, otherwise AmountCredit
    ///    - Checks for duplicates using IsTransactionExists with fuzzy date matching
    ///    - Creates new Transaction entity if not duplicate
    ///    - Applies rules using dataService.ApplyRule
    /// 
    /// **Database Operations:**
    /// 10. Adds all new transactions to database context
    /// 11. Saves changes in a single transaction
    /// 
    /// **File Management:**
    /// 12. Creates "Imported" subfolder in the source file's directory
    /// 13. Moves the imported file to the "Imported" folder
    /// 14. Deletes the original file
    /// 
    /// **CIBC CSV Format Expected:**
    /// - Column 0: Date (transaction date)
    /// - Column 1: Description (transaction description)
    /// - Column 2: Amount Debit (optional, if present = debit)
    /// - Column 3: Amount Credit (optional, if Debit is null = credit)
    /// - Column 4: Account Number (account identifier)
    /// 
    /// **Important:** CIBC CSV files do NOT have a header row.
    /// The first line contains data.
    /// 
    /// **Date Limit:**
    /// - Only processes transactions on or after January 1, 2024
    /// - Older transactions are skipped silently
    /// - This prevents importing very old historical data
    /// 
    /// **Account Resolution:**
    /// - Account is looked up by AccountNumber
    /// - If not found and isCreateAccounts is true: account is created
    /// - If not found and isCreateAccounts is false: transaction is skipped
    /// 
    /// **Debit/Credit Logic:**
    /// - If AmountDebit has a value: transaction is a debit (money spent)
    /// - If AmountDebit is null and AmountCredit has value: transaction is a credit (money received)
    /// - If both are null: amount is 0 (shouldn't happen)
    /// - IsDebit flag is set based on which column has value
    /// 
    /// **Category Assignment:**
    /// - All imported transactions are assigned to "Uncategorized" category
    /// - This is because CIBC CSV doesn't include category information
    /// - Users can then run rules to categorize transactions
    /// 
    /// **Description Handling:**
    /// - Description column is used for both Description and OriginalDescription
    /// - Not trimmed separately (already from CSV)
    /// 
    /// **Duplicate Detection:**
    /// - Uses fuzzy date matching (±5 days)
    /// - Compares: date (with fuzzy window), amount, isDebit, account, description
    /// - Fuzzy matching is used because CIBC may have different transaction posting dates
    /// 
    /// **Rule Application:**
    /// - Each transaction is passed through ApplyRule after creation
    /// - Rules can auto-assign categories based on patterns
    /// - This happens before the transaction is saved
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
    /// - Only moves file if directory path is not null (should always be valid)
    /// 
    /// **Error Handling:**
    /// - Invalid CSV structure throws InvalidOperationException with clear message
    /// - File not found throws FileNotFoundException
    /// - Database errors may occur on constraint violations
    /// - File access errors may occur if file is locked
    /// - Null reference possible if GetDirectoryName returns null (unlikely)
    /// 
    /// Thread Safety: This method is not thread-safe. Run imports sequentially.
    /// 
    /// Performance: All transactions are batch-added and saved in a single database transaction for efficiency.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when CSV file has invalid structure or missing valid records.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="IOException">Thrown when file cannot be read or moved.</exception>
    /// <exception cref="Exception">Thrown for database operation failures.</exception>
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
            // Limit to date to January 1st 2024 and older
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
