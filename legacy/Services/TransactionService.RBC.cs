using System.Globalization;
using CsvHelper;
using MoneyManager.Model.Import;

namespace MoneyManager.Services;

/// <content>
/// Contains methods for importing transactions from RBC (Royal Bank of Canada) CSV files.
/// </content>
public partial class TransactionService
{
    /// <summary>
    /// Validates that the RBC CSV file has the expected structure and columns.
    /// </summary>
    /// <param name="filePath">The full path to the RBC CSV file to validate.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// - File is empty or missing header row
    /// - Expected columns are missing from the CSV
    /// </exception>
    /// <remarks>
    /// This method performs structural validation before attempting import:
    /// 
    /// **Validation Steps:**
    /// 1. Reads the first line of the CSV file (header row)
    /// 2. Checks if header row is empty or null
    /// 3. Splits header by comma and removes quotes
    /// 4. Checks for all expected columns
    /// 
    /// **Expected RBC CSV Columns:**
    /// 1. Account Type
    /// 2. Account Number
    /// 3. Transaction Date
    /// 4. Cheque Number
    /// 5. Description 1
    /// 6. Description 2
    /// 7. CAD$
    /// 8. USD$
    /// 
    /// **Error Messages:**
    /// - "RBC CSV file is empty or missing header row."
    /// - "RBC CSV file does not have the expected structure. Missing column: '{columnName}'. Expected columns: {all columns}"
    /// 
    /// This validation prevents processing malformed CSV files and provides clear error messages.
    /// 
    /// Note: This method only checks column names, not data validity.
    /// </remarks>
    private void ValidateRBCCSV(string filePath)
    {
        var headerLine = File.ReadLines(filePath).FirstOrDefault();
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
    /// Imports transactions from an RBC CSV export file into the MoneyManager database.
    /// </summary>
    /// <param name="filePath">The full path to the RBC CSV file to import.</param>
    /// <param name="isCreateAccounts">If true, creates new accounts for unknown account numbers. If false, skips transactions with unknown accounts.</param>
    /// <param name="progress">An action callback for reporting import progress (0-100%).</param>
    /// <returns>
    /// The number of transactions successfully imported from the file.
    /// </returns>
    /// <remarks>
    /// This method performs a complete RBC-specific import workflow:
    /// 
    /// **Validation:**
    /// 1. Validates CSV structure using ValidateRBCCSV
    /// 
    /// **Preparation:**
    /// 2. Creates a database backup before making changes
    /// 3. Initializes in-memory caches for accounts and categories
    /// 4. Calculates total records for progress reporting
    /// 5. Resolves default "Uncategorized" category
    /// 
    /// **CSV Processing:**
    /// 6. Opens and reads the CSV file using CsvHelper
    /// 7. Uses invariant culture for consistent number/date parsing
    /// 8. Iterates through each record:
    ///    - Reports progress at least every 1% increment
    ///    - Resolves account from AccountNumber (creates if isCreateAccounts)
    ///    - Skips if account is null and isCreateAccounts is false
    ///    - Skips if AmountCAD is null
    ///    - Determines if debit: negative amount = debit
    ///    - Uses absolute value of amount
    ///    - Combines Description1 and Description2
    ///    - Checks for duplicates using IsTransactionExists
    ///    - Creates new Transaction entity if not duplicate
    ///    - Applies rules using dataService.ApplyRule
    /// 
    /// **Database Operations:**
    /// 9. Adds all new transactions to database context
    /// 10. Saves changes in a single transaction
    /// 
    /// **File Management:**
    /// 11. Creates "Imported" subfolder in the source file's directory
    /// 12. Moves the imported file to the "Imported" folder
    /// 13. Deletes the original file
    /// 
    /// **RBC CSV Format Expected:**
    /// - Account Number: Account identifier (used for account resolution)
    /// - Transaction Date: Transaction date
    /// - Description 1: First part of description
    /// - Description 2: Second part of description
    /// - CAD$: Amount in CAD (negative = debit)
    /// - USD$: Amount in USD (currently ignored, only CAD used)
    /// 
    /// **Account Resolution:**
    /// - Account is looked up by AccountNumber
    /// - If not found and isCreateAccounts is true: account is created
    /// - If not found and isCreateAccounts is false: transaction is skipped
    /// - RBC account numbers may include dashes (handled by GetAccount)
    /// 
    /// **Debit/Credit Logic:**
    /// - Negative AmountCAD values are treated as debits (money spent)
    /// - Positive AmountCAD values are treated as credits (money received)
    /// - Amount is stored as absolute value
    /// - IsDebit flag is set based on sign
    /// 
    /// **Category Assignment:**
    /// - All imported transactions are assigned to "Uncategorized" category
    /// - This is because RBC CSV doesn't include category information
    /// - Users can then run rules to categorize transactions
    /// 
    /// **Description Handling:**
    /// - Description 1 and Description 2 are combined with space
    /// - Both are trimmed and stored in Description and OriginalDescription
    /// - Example: "Grocery Store" + "Main St" = "Grocery Store Main St"
    /// 
    /// **Duplicate Detection:**
    /// - Uses exact date matching (no fuzzy matching)
    /// - Compares: date, amount, isDebit, account, combined description
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
    /// 
    /// **Error Handling:**
    /// - Invalid CSV structure throws InvalidOperationException with clear message
    /// - File not found throws FileNotFoundException
    /// - Database errors may occur on constraint violations
    /// - File access errors may occur if file is locked
    /// 
    /// Thread Safety: This method is not thread-safe. Run imports sequentially.
    /// 
    /// Performance: All transactions are batch-added and saved in a single database transaction for efficiency.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when CSV file has invalid structure or missing columns.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="IOException">Thrown when file cannot be read or moved.</exception>
    /// <exception cref="Exception">Thrown for database operation failures.</exception>
    public async Task<int> ImportRBCCSV(string filePath, bool isCreateAccounts, Action<int> progress)
    {
        ValidateRBCCSV(filePath);
        
        await dbService.Backup();
        
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
