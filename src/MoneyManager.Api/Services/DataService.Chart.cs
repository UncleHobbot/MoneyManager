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
    /// Builds the monthly spending-by-category trend for a period: the top 7 parent
    /// categories by total spend plus an "Other" bucket, one value per month.
    /// Expenses only; transfers excluded. Reuses <see cref="ReportingRow"/> (the
    /// listability invariant, parent rollup, and sign convention) — see CONTEXT.md.
    /// </summary>
    public async Task<SpendingTrendChart> ChartSpendingTrendAsync(string chartPeriod)
    {
        const int TopCategoryCount = 7;

        var (startDate, endDate) = (ChartPeriod.Find(chartPeriod) ?? ChartPeriod.Default).GetDateRange(DateTime.Today);

        var rows = await queryService.GetReportingRowsAsync(
            new TransactionFilters(StartDate: startDate, EndDate: endDate));

        // Expenses only: drop income, transfers, and uncategorized rows.
        var expenseRows = rows
            .Where(r => !r.IsIncome && !r.IsTransfer && r.EffectiveCategory is not null)
            .ToList();

        if (expenseRows.Count == 0)
            return new SpendingTrendChart([], []);

        // Month buckets span [firstMonth, endDate). For unbounded periods ("a",
        // StartDate == MinValue) anchor on the earliest expense month so we don't
        // generate thousands of empty buckets.
        var earliestMonth = expenseRows.Min(r => r.Date).StartOfMonth();
        var firstMonth = startDate.StartOfMonth() > earliestMonth ? startDate.StartOfMonth() : earliestMonth;

        var months = new List<SpendingTrendMonth>();
        var monthIndex = new Dictionary<DateTime, int>();
        for (var cursor = firstMonth; cursor < endDate; cursor = cursor.AddMonths(1))
        {
            monthIndex[cursor] = months.Count;
            months.Add(new SpendingTrendMonth(cursor.ToString("MMM yy"), cursor, cursor.AddMonths(1)));
        }

        // Accumulate spend per category per month. Spend is the magnitude (-SignedAmount).
        var agg = new Dictionary<int, (string Name, string? Icon, decimal[] PerMonth, decimal Total)>();
        foreach (var row in expenseRows)
        {
            if (!monthIndex.TryGetValue(row.Date.StartOfMonth(), out var mi))
                continue;

            var cat = row.EffectiveCategory!;
            if (!agg.TryGetValue(cat.Id, out var entry))
                entry = (cat.Name, cat.Icon, new decimal[months.Count], 0m);

            var spend = -row.SignedAmount;
            entry.PerMonth[mi] += spend;
            entry.Total += spend;
            agg[cat.Id] = entry;
        }

        var ranked = agg.OrderByDescending(kv => kv.Value.Total).ToList();

        var series = ranked
            .Take(TopCategoryCount)
            .Select(kv => new SpendingTrendSeries(kv.Key, kv.Value.Name, kv.Value.Icon, kv.Value.PerMonth))
            .ToList();

        var rest = ranked.Skip(TopCategoryCount).ToList();
        if (rest.Count > 0)
        {
            var otherPerMonth = new decimal[months.Count];
            foreach (var kv in rest)
                for (var i = 0; i < months.Count; i++)
                    otherPerMonth[i] += kv.Value.PerMonth[i];
            series.Add(new SpendingTrendSeries(null, "Other", null, otherPerMonth));
        }

        return new SpendingTrendChart(months, series);
    }

    /// <summary>
    /// Returns the top spending merchants over a period, grouped by transaction
    /// <c>Description</c> (the Rules-normalized merchant label — see CONTEXT.md
    /// "Merchant / Payee"). Expenses only (income and transfers excluded);
    /// uncategorized spend still counts. Reuses <see cref="ReportingRow"/>.
    /// </summary>
    public async Task<List<MerchantSpend>> ChartTopMerchantsAsync(string chartPeriod, int limit = 15)
    {
        var (startDate, endDate) = (ChartPeriod.Find(chartPeriod) ?? ChartPeriod.Default).GetDateRange(DateTime.Today);

        var rows = await queryService.GetReportingRowsAsync(
            new TransactionFilters(StartDate: startDate, EndDate: endDate));

        return rows
            .Where(r => !r.IsIncome && !r.IsTransfer)
            .GroupBy(r => r.Description)
            .Select(g => new MerchantSpend(g.Key, g.Sum(r => -r.SignedAmount), g.Count()))
            .Where(m => m.Amount > 0) // drop net-credit merchants (e.g. pure refunds)
            .OrderByDescending(m => m.Amount)
            .Take(limit)
            .ToList();
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
