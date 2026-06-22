using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Helpers;
using MoneyManager.Api.Model.Chart;
using MoneyManager.Api.Model.Query;

namespace MoneyManager.Api.Services;

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
    public async Task<List<Transaction>> ChartGetTransactionsAsync(DateTime startDate, DateTime endDate)
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
    /// This is a convenience method that converts the month string to date boundaries and calls ChartGetTransactionsAsync(DateTime, DateTime).
    /// 
    /// The method parses the month string and calculates:
    /// - Start date: First day of the specified month at midnight
    /// - End date: First day of the next month (exclusive boundary)
    /// </remarks>
    /// <exception cref="FormatException">Thrown when month string is not in valid "yyMM" format.</exception>
    public async Task<List<Transaction>> ChartGetTransactionsAsync(string month)
    {
        var startDate = DateTime.ParseExact(month, "yyMM", Thread.CurrentThread.CurrentCulture);
        var endDate = startDate.AddMonths(1).StartOfMonth();

        return await ChartGetTransactionsAsync(startDate, endDate);
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
        endDate = DateTime.Today.AddMonths(1).StartOfMonth();

        switch (chartPeriod)
        {
            // last 12 months
            case "12":
                startDate = DateTime.Today.AddMonths(-12).StartOfMonth();
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
    /// 2. Retrieves transactions for that date range using ChartGetTransactionsAsync
    /// 
    /// Commonly used to fetch transaction data for chart components without manually calculating dates.
    /// </remarks>
    public async Task<List<Transaction>> ChartGetTransactionsPAsync(string chartPeriod)
    {
        GetDates(chartPeriod, out var startDate, out var endDate);
        return await ChartGetTransactionsAsync(startDate, endDate);
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
    /// 1. Retrieves <see cref="ReportingRow">reporting rows</see> for the period
    ///    (excludes hidden accounts via the listability invariant; category
    ///    name-match for Income is precomputed as <see cref="ReportingRow.IsIncome"/>;
    ///    sign convention is precomputed as <see cref="ReportingRow.SignedAmount"/>).
    /// 2. Filters out transfers (<c>IsTransfer</c> flag) — chart convention.
    /// 3. Groups the remaining rows by month (format: "MMM yy").
    /// 4. Sums signed amounts into Income (<c>IsIncome</c>) and Expenses buckets.
    ///
    /// Income is positive (credits); Expenses is negative (debits) — both follow
    /// the canonical <see cref="ReportingRow.SignedAmount"/> convention from
    /// <c>CONTEXT.md</c> ("Signed amount").
    ///
    /// The result is ordered chronologically by first date in each month.
    /// </remarks>
    public async Task<List<BalanceChart>> ChartNetIncomeAsync(string chartPeriod)
    {
        GetDates(chartPeriod, out var startDate, out var endDate);

        var filters = new TransactionFilters(StartDate: startDate, EndDate: endDate);
        var rows = await queryService.GetReportingRowsAsync(filters);

        var result = rows
            .Where(r => !r.IsTransfer)
            .GroupBy(r => r.Date.ToString("MMM yy"))
            .Select(g => new BalanceChart
            {
                Month = g.Key,
                FirstDate = g.Min(r => r.Date),
                MonthLabel = g.Min(r => r.Date).ToString("MMMM yyyy"),
                MonthKey = g.Min(r => r.Date).ToString("yyMM"),
                Income = g.Where(r => r.IsIncome).Sum(r => r.SignedAmount),
                Expenses = g.Where(r => !r.IsIncome).Sum(r => r.SignedAmount),
            })
            .OrderBy(x => x.FirstDate)
            .ToList();

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
    /// 1. Retrieves <see cref="ReportingRow">reporting rows</see> from the first
    ///    day of last month to today (today's transactions excluded by the
    ///    exclusive end of the date window, matching pre-migration behavior).
    /// 2. Filters to expenses only (<c>!IsIncome &amp;&amp; !IsTransfer</c>).
    /// 3. For each day (1-31), sums <c>-SignedAmount</c> over rows whose date
    ///    falls in [monthStart, dayBoundary). The negation flips the canonical
    ///    "debits negative" convention to the "expenses positive" convention
    ///    this chart has always used; see <c>CONTEXT.md</c> ("Signed amount").
    ///
    /// Days beyond the actual month length will have <c>null</c> for that month's
    /// expenses. Future days in the current month will have <c>null</c> for this
    /// month's expenses.
    ///
    /// This is used to visualize spending velocity and identify whether the
    /// current month is ahead or behind the previous month's pace.
    /// </remarks>
    public async Task<List<CumulativeSpendingChart>> ChartCumulativeSpendingAsync()
    {
        var result = new List<CumulativeSpendingChart>();

        var thisMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var lastMonthStart = thisMonthStart.AddDays(-1);
        lastMonthStart = new DateTime(lastMonthStart.Year, lastMonthStart.Month, 1);

        var filters = new TransactionFilters(StartDate: lastMonthStart, EndDate: DateTime.Today);
        var rows = await queryService.GetReportingRowsAsync(filters);

        var expenses = rows
            .Where(r => !r.IsIncome && !r.IsTransfer)
            .ToList();

        for (var day = 1; day <= 31; day++)
        {
            var dayValue = new CumulativeSpendingChart { DayNumber = day };

            try
            {
                var lastMonthDate = new DateTime(lastMonthStart.Year, lastMonthStart.Month, day).AddDays(1);
                var lastMonth = expenses
                    .Where(x => x.Date >= lastMonthStart && x.Date < lastMonthDate)
                    .Sum(x => -x.SignedAmount);
                dayValue.LastMonthExpenses = lastMonth;
                result.Add(dayValue);
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                var thisMonthDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, day);
                if (thisMonthDate <= DateTime.Today)
                {
                    thisMonthDate = thisMonthDate.AddDays(1);
                    var thisMonth = expenses
                        .Where(x => x.Date >= thisMonthStart && x.Date < thisMonthDate)
                        .Sum(x => -x.SignedAmount);
                    dayValue.ThisMonthExpenses = thisMonth;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        return result;
    }
}
