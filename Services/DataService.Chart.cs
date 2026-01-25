using Microsoft.FluentUI.AspNetCore.Components.Extensions;
using DateTime = System.DateTime;

namespace MoneyManager.Services;

/// <content>
/// Contains methods for retrieving and processing transaction data for chart visualization and analysis.
/// </content>
public partial class DataService
{
    /// <summary>
    /// Retrieves transactions within the specified date range for chart purposes, excluding transfers and hidden accounts.
    /// </summary>
    /// <param name="startDate">The start date of the period to retrieve transactions from (inclusive).</param>
    /// <param name="endDate">The end date of the period to retrieve transactions from (exclusive).</param>
    /// <returns>
    /// A list of Transaction objects that fall within the specified date range, excluding transfers and accounts marked as hidden from graphs.
    /// </returns>
    /// <remarks>
    /// This method filters out:
    /// - Transactions belonging to the "Transfer" category (either directly or as a subcategory)
    /// - Transactions from accounts where IsHideFromGraph is true
    /// - Transactions without a category assignment
    /// 
    /// The method eagerly loads Account, Category, and Parent Category relationships for efficient access.
    /// This is used by all chart data aggregation methods to ensure consistent filtering.
    /// </remarks>
    public async Task<List<Transaction>> ChartGetTransactions(DateTime startDate, DateTime endDate)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var categoryTransfer = await ctx.Categories.FirstOrDefaultAsync(x => x.Name == "Transfer");

        var trans = await ctx.Transactions
            .Where(x => x.Date >= startDate && x.Date < endDate)
            .Where(x => x.Category != null && categoryTransfer != null && x.Category.Id != categoryTransfer.Id && x.Category.Parent.Id != categoryTransfer.Id)
            .Where(x => !x.Account.IsHideFromGraph)
            .Include(x => x.Account).Include(x => x.Category).Include(x => x.Category.Parent)
            .ToListAsync();
        return trans;
    }

    /// <summary>
    /// Retrieves transactions for a specific month (format: yyMM) for chart purposes.
    /// </summary>
    /// <param name="month">The month in "yyMM" format (e.g., "2501" for January 2025).</param>
    /// <returns>
    /// A list of Transaction objects for the specified month, excluding transfers and hidden accounts.
    /// </returns>
    /// <remarks>
    /// This is a convenience method that converts the month string to date boundaries and calls ChartGetTransactions(DateTime, DateTime).
    /// 
    /// The method parses the month string and calculates:
    /// - Start date: First day of the specified month at midnight
    /// - End date: First day of the next month (exclusive boundary)
    /// </remarks>
    /// <exception cref="FormatException">Thrown when month string is not in valid "yyMM" format.</exception>
    public async Task<List<Transaction>> ChartGetTransactions(string month)
    {
        var startDate = DateTime.ParseExact(month, "yyMM", Thread.CurrentThread.CurrentCulture);
        var endDate = startDate.AddMonths(1).StartOfMonth(Thread.CurrentThread.CurrentCulture);

        return await ChartGetTransactions(startDate, endDate);
    }

    /// <summary>
    /// Converts a chart period code to corresponding start and end dates.
    /// </summary>
    /// <param name="chartPeriod">The period code (e.g., "12", "y1", "m1", "w", "a").</param>
    /// <param name="startDate">Output parameter for the calculated start date.</param>
    /// <param name="endDate">Output parameter for the calculated end date.</param>
    /// <remarks>
    /// Supported period codes:
    /// 
    /// **Year-based periods:**
    /// - "12" - Last 12 months from today
    /// - "y1" - Current year (January 1 to now)
    /// - "y2" - Last year (full previous calendar year)
    /// - "y3" - Two years ago (full calendar year)
    /// - "y12" - Last year + current year (from Jan 1 of last year to now)
    /// 
    /// **Month-based periods:**
    /// - "m1" - Current month (first day to now)
    /// - "m2" - Last month (full previous month)
    /// - "m1+2" - Current + last 2 months (last 3 months)
    /// - "m1+3" - Current + last 3 months (last 4 months)
    /// 
    /// **Week-based periods:**
    /// - "w" or "w1" - Last 7 days
    /// - "w2" - Last 14 days
    /// - "w3" - Last 31 days
    /// 
    /// **Other periods:**
    /// - "a" - All data (from DateTime.MinValue)
    /// 
    /// If the period code is not recognized, defaults to current year to next month start.
    /// </remarks>
    public void GetDates(string chartPeriod, out DateTime startDate, out DateTime endDate)
    {
        startDate = new DateTime(DateTime.Today.Year, 1, 1);
        endDate = DateTime.Today.AddMonths(1).StartOfMonth(Thread.CurrentThread.CurrentCulture);

        switch (chartPeriod)
        {
            // last 12 months
            case "12":
                startDate = DateTime.Today.AddMonths(-12).StartOfMonth(Thread.CurrentThread.CurrentCulture);
                break;
            // this year
            case "y1":
                startDate = new DateTime(DateTime.Today.Year, 1, 1);
                break;
            // last year
            case "y2":
                startDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
                endDate = new DateTime(DateTime.Today.Year, 1, 1);
                break;
            // 2 years ago
            case "y3":
                startDate = new DateTime(DateTime.Today.Year - 2, 1, 1);
                endDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
                break;
            // last + this year
            case "y12":
                startDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
                break;
            // This month
            case "m1":
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                break;
            // Last month
            case "m2":
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                endDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                break;
            // This +last months
            case "m1+2":
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                break;
            // This + 2 last months
            case "m1+3":
                startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-2);
                break;
            // Last 7 days
            case "w" or "w1":
                startDate = DateTime.Today.AddDays(-7);
                break;
            // Last 14 days
            case "w2":
                startDate = DateTime.Today.AddDays(-14);
                break;
            // Last 31 days
            case "w3":
                startDate = DateTime.Today.AddDays(-31);
                break;
            // All
            case "a":
                startDate = DateTime.MinValue;
                break;
        }
    }

    /// <summary>
    /// Retrieves transactions for a specified chart period using period code.
    /// </summary>
    /// <param name="chartPeriod">The period code (e.g., "12", "y1", "m1", "w", "a").</param>
    /// <returns>
    /// A list of Transaction objects for the specified period, excluding transfers and hidden accounts.
    /// </returns>
    /// <remarks>
    /// This is a convenience method that:
    /// 1. Converts the period code to start/end dates using GetDates
    /// 2. Retrieves transactions for that date range using ChartGetTransactions
    /// 
    /// Commonly used to fetch transaction data for chart components without manually calculating dates.
    /// </remarks>
    public async Task<List<Transaction>> ChartGetTransactionsP(string chartPeriod)
    {
        GetDates(chartPeriod, out var startDate, out var endDate);
        return await ChartGetTransactions(startDate, endDate);
    }

    /// <summary>
    /// Aggregates transaction data by month to calculate net income (income vs expenses).
    /// </summary>
    /// <param name="chartPeriod">The period code (e.g., "12", "y1", "m1", "w", "a").</param>
    /// <returns>
    /// A list of BalanceChart objects grouped by month, each containing:
    /// - Month label and key for display and sorting
    /// - Total Income: Sum of transactions in the "Income" category (or subcategories)
    /// - Total Expenses: Sum of all non-income transactions
    /// </returns>
    /// <remarks>
    /// This method:
    /// 1. Retrieves transactions for the specified period
    /// 2. Groups transactions by month (format: "MMM yy")
    /// 3. Separates income (transactions in "Income" category tree) from expenses
    /// 4. Calculates totals applying debit/credit sign correction
    /// 
    /// Income calculation: Sum of transactions where (Category or Parent) == "Income"
    /// Expense calculation: Sum of all other transactions
    /// 
    /// Sign correction: For both income and expenses, multiplies by -1 if IsDebit is true.
    /// This ensures positive values represent net flow:
    /// - Positive income = money received
    /// - Positive expenses = money spent
    /// 
    /// The result is ordered chronologically by first date in each month.
    /// </remarks>
    public async Task<List<BalanceChart>> ChartNetIncome(string chartPeriod)
    {
        GetDates(chartPeriod, out var startDate, out var endDate);

        var catIncome = await GetCategoryByName("Income");
        var trans = await ChartGetTransactions(startDate, endDate);

        var result = trans
            .GroupBy(x => x.Date.ToString("MMM yy"))
            .Select(x => new BalanceChart
            {
                Month = x.Key,
                FirstDate = x.Min(t => t.Date),
                MonthLabel = x.Min(t => t.Date).ToString("MMMM yyyy"),
                MonthKey = x.Min(t => t.Date).ToString("yyMM"),
                Income = x.Where(t => t.Category != null && catIncome != null && (t.Category.Parent ?? t.Category).Id == catIncome.Id).Sum(t => (t.IsDebit ? -1 : 1) * t.Amount),
                Expenses = x.Where(t => t.Category != null && catIncome != null && (t.Category.Parent ?? t.Category).Id != catIncome.Id).Sum(t => (t.IsDebit ? -1 : 1) * t.Amount)
            }).OrderBy(x => x.FirstDate).ToList();

        return result;
    }

    /// <summary>
    /// Calculates cumulative spending by day of month for the current month and previous month.
    /// </summary>
    /// <returns>
    /// A list of CumulativeSpendingChart objects (31 entries, one for each day of month) containing:
    /// - DayNumber: The day number (1-31)
    /// - LastMonthExpenses: Cumulative expenses on that day last month
    /// - ThisMonthExpenses: Cumulative expenses on that day this month (only up to today)
    /// </returns>
    /// <remarks>
    /// This method creates a day-by-day comparison of spending patterns between two months:
    /// 
    /// 1. Retrieves transactions from the first day of last month to today
    /// 2. Filters out income transactions (only expenses are included)
    /// 3. For each day (1-31):
    ///    - Calculates cumulative expenses on that day last month
    ///    - Calculates cumulative expenses on that day this month (if day has passed)
    /// 
     /// Cumulative calculation: Sum of all expenses from the first of the month up to and including that day.
     /// 
     /// Sign convention: Debit transactions are positive (money spent), credit transactions are negative (money back).
     /// This differs from ChartNetIncome where expenses are shown as positive values for display.
    /// 
    /// Days beyond the actual month length will have NaN for that month's expenses.
    /// Future days in the current month will have NaN for this month's expenses.
    /// 
    /// This is used to visualize spending velocity and identify whether the current month is ahead or behind the previous month's pace.
    /// </remarks>
    public async Task<List<CumulativeSpendingChart>> ChartCumulativeSpending()
    {
        var result = new List<CumulativeSpendingChart>();

        var thisMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var lastMonthStart = thisMonthStart.AddDays(-1);
        lastMonthStart = new DateTime(lastMonthStart.Year, lastMonthStart.Month, 1);

        var catIncome = await GetCategoryByName("Income");
        var trans = (await ChartGetTransactions(lastMonthStart, DateTime.Today))
            .Where(x => (x.Category.Parent ?? x.Category).Id != catIncome.Id).ToList();

        for (var day = 1; day <= 31; day++)
        {
            var dayValue = new CumulativeSpendingChart { DayNumber = day };

            try
            {
                var lastMonthDate = new DateTime(lastMonthStart.Year, lastMonthStart.Month, day).AddDays(1);
                var lastMonth = trans.Where(x => x.Date >= lastMonthStart && x.Date < lastMonthDate).Sum(x => (x.IsDebit ? 1 : -1) * x.Amount);
                dayValue.LastMonthExpenses = lastMonth;
                result.Add(dayValue);
            }
            catch (ArgumentOutOfRangeException ex)
            {
            }

            try
            {
                var thisMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, day);
                if (thisMonthDate <= DateTime.Today)
                {
                    thisMonthDate = thisMonthDate.AddDays(1);
                    var thisMonth = trans.Where(x => x.Date >= thisMonthStart && x.Date < thisMonthDate).Sum(x => (x.IsDebit ? 1 : -1) * x.Amount);
                    dayValue.ThisMonthExpenses = thisMonth;
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
            }
        }

        return result;
    }
}
