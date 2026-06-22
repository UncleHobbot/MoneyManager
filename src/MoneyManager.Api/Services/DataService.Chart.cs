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
        var (startDate, endDate) = (ChartPeriod.Find(chartPeriod) ?? ChartPeriod.Default).GetDateRange(DateTime.Today);

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
