using System.Globalization;
using CsvHelper;
using MoneyManager.Model.AI;

namespace MoneyManager.Services;

/// <content>
/// Contains methods for preparing transaction data for AI analysis, including CSV export functionality.
/// </content>
public partial class DataService
{
    /// <summary>
    /// Retrieves transactions for AI analysis within a specified period code.
    /// </summary>
    /// <param name="period">The period code (e.g., "12", "y1", "m1", "w", "a").</param>
    /// <returns>
    /// A list of TransactionAI objects containing simplified transaction data optimized for AI processing.
    /// </returns>
    /// <remarks>
    /// This is a convenience method that:
    /// 1. Converts the period code to start/end dates using GetDates
    /// 2. Retrieves and formats transactions for AI consumption using AIGetTransactions(DateTime, DateTime)
    /// 
    /// Unlike ChartGetTransactions, this method does not filter out transfer transactions,
    /// allowing AI to see the complete picture of money movement including transfers.
    /// </remarks>
    public async Task<List<TransactionAI>> AIGetTransactions(string period)
    {
        GetDates(period, out var dateStart, out var dateEnd);
        return await AIGetTransactions(dateStart, dateEnd);
    }

    /// <summary>
    /// Retrieves transactions for AI analysis within the specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the period to retrieve transactions from (inclusive).</param>
    /// <param name="endDate">The end date of the period to retrieve transactions from (exclusive).</param>
    /// <returns>
    /// A list of TransactionAI objects containing simplified transaction data optimized for AI processing.
    /// Each TransactionAI includes:
    /// - Id: Transaction identifier
    /// - Account: Account name with "Credit card" prefix for credit card accounts
    /// - Date: Transaction date in short date string format
    /// - Amount: Transaction amount
    /// - SubCategory: Category name
    /// - Category: Parent category name
    /// - Description: Transaction description
    /// </returns>
    /// <remarks>
    /// This method:
    /// 1. Fetches all transactions in the date range
    /// 2. Excludes transactions without a category
    /// 3. Excludes transactions from accounts where IsHideFromGraph is true
    /// 4. Transforms transactions to a flat TransactionAI structure optimized for CSV export
    /// 
    /// Unlike ChartGetTransactions, transfer transactions are NOT filtered out.
    /// This allows the AI to see the complete picture of money flow.
    /// 
    /// Account naming: Credit card accounts (Type == 1) are prefixed with "Credit card " to help AI distinguish account types.
    /// 
    /// Date format: Uses the system's short date format for human-readable AI consumption.
    /// </remarks>
    public async Task<List<TransactionAI>> AIGetTransactions(DateTime startDate, DateTime endDate)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var categoryTransfer = await ctx.Categories.FirstOrDefaultAsync(x => x.Name == "Transfer");

        var trans = await ctx.Transactions
            .Where(x => x.Date >= startDate && x.Date < endDate)
            .Where(x => x.Category != null)
            //.Where(x => x.Category != null && categoryTransfer != null && x.Category.Id != categoryTransfer.Id && x.Category.Parent.Id != categoryTransfer.Id)
            .Where(x => !x.Account.IsHideFromGraph)
            .Include(x => x.Account).Include(x => x.Category).Include(x => x.Category.Parent)
            .Select(x => new TransactionAI
            {
                Id = x.Id,
                Account = (x.Account.Type == 1 ? "Credit card " : "") + x.Account.ShownName,
                Date = x.Date.ToShortDateString(),
                Amount = x.Amount,
                SubCategory = x.Category.Name,
                Category = x.Category.Parent.Name,
                Description = x.Description
            })
            .ToListAsync();
        return trans;
    }

    /// <summary>
    /// Exports transactions to CSV format for AI analysis within a specified period code.
    /// </summary>
    /// <param name="period">The period code (e.g., "12", "y1", "m1", "w", "a").</param>
    /// <returns>
    /// A CSV-formatted string containing transaction data ready for AI consumption.
    /// </returns>
    /// <remarks>
    /// This is a convenience method that:
    /// 1. Converts the period code to start/end dates using GetDates
    /// 2. Generates CSV data using AIGetTransactionsCSV(DateTime, DateTime)
    /// 
    /// The CSV format includes headers matching the TransactionAI property names,
    /// making it directly consumable by the AI service for analysis.
    /// </remarks>
    public async Task<string> AIGetTransactionsCSV(string period)
    {
        GetDates(period, out var dateStart, out var dateEnd);
        return await AIGetTransactionsCSV(dateStart, dateEnd);
    }

    /// <summary>
    /// Exports transactions to CSV format for AI analysis within the specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the period to retrieve transactions from (inclusive).</param>
    /// <param name="endDate">The end date of the period to retrieve transactions from (exclusive).</param>
    /// <returns>
    /// A CSV-formatted string containing transaction data ready for AI consumption.
    /// </returns>
    /// <remarks>
     /// This method:
     /// 1. Retrieves transactions using AIGetTransactions
     /// 2. Serializes the TransactionAI objects to CSV format using CsvHelper
     /// 3. Returns the CSV as a string for transmission to the AI API
     /// 
     /// CSV Format:
     /// - Uses invariant culture (CultureInfo.InvariantCulture) for consistent formatting
     /// - Includes header row with property names
     /// - Uses commas as field separators
     /// - Properly escapes quotes and commas in data values
    /// 
    /// The CSV is designed to be included as data in AI API requests.
    /// 
    /// Note: Transfer transactions are included in the CSV, unlike chart data exports.
    /// </remarks>
    public async Task<string> AIGetTransactionsCSV(DateTime startDate, DateTime endDate)
    {
        var trans = await AIGetTransactions(startDate, endDate);
        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture, true);
        await csv.WriteRecordsAsync(trans);
        await csv.FlushAsync();
        var newRecord = writer.ToString();
        return newRecord;
    }
}
