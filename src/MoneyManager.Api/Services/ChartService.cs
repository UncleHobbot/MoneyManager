using MoneyManager.Api.Helpers;
using MoneyManager.Api.Model.Chart;
using MoneyManager.Api.Model.Query;

namespace MoneyManager.Api.Services;

/// <summary>
/// The single home for chart aggregation: turns <see cref="ReportingRow"/>
/// projections into chart DTOs. Consumes (never produces) reporting rows via
/// <see cref="TransactionQueryService"/> and reuses <see cref="BudgetService"/>
/// for budgets, so it touches no database directly. See CONTEXT.md
/// ("Chart aggregation") and ADR-0004 (ReportingRow).
/// </summary>
public class ChartService(TransactionQueryService queryService, BudgetService budgetService)
{
    /// <summary>
    /// Aggregates transactions by month into income vs expenses. Transfers are
    /// excluded (chart convention); Income/Expense split and sign convention come
    /// precomputed on <see cref="ReportingRow"/>. Ordered chronologically.
    /// </summary>
    public async Task<List<BalanceChart>> ChartNetIncomeAsync(string chartPeriod)
    {
        var (startDate, endDate) = (ChartPeriod.Find(chartPeriod) ?? ChartPeriod.Default).GetDateRange(DateTime.Today);

        var filters = new TransactionFilters(StartDate: startDate, EndDate: endDate);
        var rows = await queryService.GetReportingRowsAsync(filters);

        return rows
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
    }

    /// <summary>
    /// Builds the monthly spending-by-category trend for a period: the top 7 parent
    /// categories by total spend plus an "Other" bucket, one value per month.
    /// Expenses only; transfers excluded.
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
    /// "Merchant / Payee"). Expenses only; uncategorized spend still counts.
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
    /// Breaks a period's spend into income and expense categories with a
    /// per-category delta against the immediately-preceding window of the same
    /// length, plus each category's share of its side. Transfers and uncategorized
    /// rows are excluded; categories are parent-rolled-up via <see cref="ReportingRow"/>.
    /// </summary>
    public async Task<SpendingByCategoryChart> ChartSpendingByCategoryAsync(string chartPeriod)
    {
        var (startDate, endDate) = (ChartPeriod.Find(chartPeriod) ?? ChartPeriod.Default).GetDateRange(DateTime.Today);

        // The immediately-preceding window of the same length, for a per-category
        // delta. Align the previous window to the same calendar span: for
        // month-aligned periods step back by whole months so "previous" is the
        // prior calendar period, not a day-count window straddling month
        // boundaries. Day-based periods (weeks) fall back to an equal-length
        // window. Skipped for unbounded "a" (StartDate == MinValue).
        var hasPrevious = startDate > DateTime.MinValue.AddYears(1);
        DateTime prevStart;
        if (!hasPrevious)
            prevStart = startDate;
        else if (startDate.Day == 1 && endDate.Day == 1)
            prevStart = startDate.AddMonths(-((endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month)));
        else
            prevStart = startDate - (endDate - startDate);

        var rows = await queryService.GetReportingRowsAsync(
            new TransactionFilters(StartDate: prevStart, EndDate: endDate));

        // Previous-window spend per category id (absolute).
        var previousByCategory = rows
            .Where(r => r.Date < startDate && !r.IsTransfer && r.EffectiveCategory != null)
            .GroupBy(r => r.EffectiveCategory!.Id)
            .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(r => r.SignedAmount)));

        // Current-window groups (one per rolled-up category), excluding transfers
        // and uncategorized.
        var grouped = rows
            .Where(r => r.Date >= startDate && !r.IsTransfer && r.EffectiveCategory != null)
            .GroupBy(r => r.EffectiveCategory!)
            .Select(g => (
                g.Key.Id,
                g.Key.Name,
                g.Key.Icon,
                Raw: g.Sum(r => r.SignedAmount),
                IsIncome: g.First().IsIncome))
            .ToList();

        var incomeItems = grouped.Where(x => x.IsIncome).ToList();
        var expenseItems = grouped.Where(x => !x.IsIncome).ToList();

        var totalIncome = incomeItems.Sum(x => Math.Abs(x.Raw));
        var totalExpenses = expenseItems.Sum(x => Math.Abs(x.Raw));

        List<CategoryChart> Project(
            List<(int Id, string Name, string? Icon, decimal Raw, bool IsIncome)> items,
            decimal total) => items
            .Select(x => new CategoryChart(
                x.Name,
                x.Icon,
                Math.Abs(x.Raw),
                previousByCategory.GetValueOrDefault(x.Id, 0m),
                total > 0 ? Math.Round((double)(Math.Abs(x.Raw) / total * 100), 2) : 0))
            .OrderByDescending(c => c.Amount)
            .ToList();

        return new SpendingByCategoryChart(Project(incomeItems, totalIncome), Project(expenseItems, totalExpenses));
    }

    /// <summary>
    /// Builds the cash-flow Sankey for a period: income category nodes flow into a
    /// single "Total Income" hub, which flows out to expense category nodes (top
    /// <paramref name="topExpenses"/> + "Other") plus a "Savings" node. A surplus
    /// produces the Savings flow; a deficit adds a "Deficit" source feeding the hub.
    /// </summary>
    public async Task<CashFlowChart> ChartCashFlowAsync(string chartPeriod, int topExpenses = 8)
    {
        const string Hub = "Total Income";

        var (startDate, endDate) = (ChartPeriod.Find(chartPeriod) ?? ChartPeriod.Default).GetDateRange(DateTime.Today);
        var rows = await queryService.GetReportingRowsAsync(
            new TransactionFilters(StartDate: startDate, EndDate: endDate));

        var nonTransfer = rows.Where(r => !r.IsTransfer).ToList();

        // Income side: income rows grouped by (rolled-up) category.
        var incomeGroups = nonTransfer
            .Where(r => r.IsIncome && r.EffectiveCategory is not null)
            .GroupBy(r => r.EffectiveCategory!)
            .Select(g => (Id: (int?)g.Key.Id, g.Key.Name, Amount: g.Sum(r => r.SignedAmount)))
            .Where(x => x.Amount > 0)
            .ToList();

        // Expense side: everything non-income. Null category groups as "Uncategorized".
        var expenseGroups = nonTransfer
            .Where(r => !r.IsIncome)
            .GroupBy(r => r.EffectiveCategory)
            .Select(g => (
                Id: g.Key?.Id,
                Name: g.Key?.Name ?? "Uncategorized",
                Kind: g.Key is null ? "uncategorized" : "expense",
                Amount: g.Sum(r => -r.SignedAmount)))
            .Where(x => x.Amount > 0)
            .OrderByDescending(x => x.Amount)
            .ToList();

        var totalIncome = incomeGroups.Sum(x => x.Amount);
        var totalExpense = expenseGroups.Sum(x => x.Amount);

        var nodes = new List<SankeyNode>();
        var links = new List<SankeyLink>();

        // Sankey links reference nodes by name, so names must be unique. A user
        // category sharing a synthetic name ("Other"/"Savings"/"Deficit"/the hub)
        // would otherwise collide; suffix duplicates to keep them distinct.
        var usedNames = new HashSet<string>();
        string Uniq(string name)
        {
            var candidate = name;
            while (!usedNames.Add(candidate))
                candidate += " ";
            return candidate;
        }

        var hubName = Uniq(Hub);

        foreach (var inc in incomeGroups)
        {
            var name = Uniq(inc.Name);
            nodes.Add(new SankeyNode(name, inc.Id, "income"));
            links.Add(new SankeyLink(name, hubName, inc.Amount));
        }

        nodes.Add(new SankeyNode(hubName, null, "hub"));

        foreach (var ex in expenseGroups.Take(topExpenses))
        {
            var name = Uniq(ex.Name);
            nodes.Add(new SankeyNode(name, ex.Id, ex.Kind));
            links.Add(new SankeyLink(hubName, name, ex.Amount));
        }

        var rest = expenseGroups.Skip(topExpenses).ToList();
        if (rest.Count > 0)
        {
            var name = Uniq("Other");
            nodes.Add(new SankeyNode(name, null, "other"));
            links.Add(new SankeyLink(hubName, name, rest.Sum(x => x.Amount)));
        }

        if (totalIncome >= totalExpense)
        {
            var surplus = totalIncome - totalExpense;
            if (surplus > 0)
            {
                var name = Uniq("Savings");
                nodes.Add(new SankeyNode(name, null, "savings"));
                links.Add(new SankeyLink(hubName, name, surplus));
            }
        }
        else
        {
            var name = Uniq("Deficit");
            nodes.Add(new SankeyNode(name, null, "deficit"));
            links.Add(new SankeyLink(name, hubName, totalExpense - totalIncome));
        }

        return new CashFlowChart(nodes, links);
    }

    /// <summary>
    /// Compares each budgeted (parent) category's monthly limit to its actual spend
    /// for the current calendar month. Actual is summed from <see cref="ReportingRow"/>
    /// at the same parent-rollup level as the budget; budgets come from
    /// <see cref="BudgetService"/>. See CONTEXT.md ("Budget") / ADR-0007.
    /// </summary>
    public async Task<List<BudgetVsActual>> ChartBudgetVsActualAsync()
    {
        var budgets = await budgetService.GetBudgetsAsync();
        if (budgets.Count == 0)
            return [];

        var now = DateTime.Today;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1);

        var rows = await queryService.GetReportingRowsAsync(
            new TransactionFilters(StartDate: start, EndDate: end));

        var actualByCategory = rows
            .Where(r => !r.IsIncome && !r.IsTransfer && r.EffectiveCategory is not null)
            .GroupBy(r => r.EffectiveCategory!.Id)
            .ToDictionary(g => g.Key, g => g.Sum(r => -r.SignedAmount));

        return budgets
            .Select(b => new BudgetVsActual(
                b.CategoryId,
                b.CategoryName,
                b.Icon,
                b.Amount,
                actualByCategory.GetValueOrDefault(b.CategoryId, 0m)))
            .OrderByDescending(x => x.Actual)
            .ToList();
    }

    /// <summary>
    /// Cumulative expenses by day of month for the current month vs the previous
    /// month (31 entries). Days beyond a month's length, or future days this month,
    /// carry <c>null</c> for that month. Used to read spending velocity.
    /// </summary>
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
