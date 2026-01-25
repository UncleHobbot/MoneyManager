using Microsoft.FluentUI.AspNetCore.Components.Extensions;
using DateTime = System.DateTime;

namespace MoneyManager.Services;

public partial class DataService
{
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

    public async Task<List<Transaction>> ChartGetTransactions(string month)
    {
        var startDate = DateTime.ParseExact(month, "yyMM", Thread.CurrentThread.CurrentCulture);
        var endDate = startDate.AddMonths(1).StartOfMonth(Thread.CurrentThread.CurrentCulture);

        return await ChartGetTransactions(startDate, endDate);
    }

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

    public async Task<List<Transaction>> ChartGetTransactionsP(string chartPeriod)
    {
        GetDates(chartPeriod, out var startDate, out var endDate);
        return await ChartGetTransactions(startDate, endDate);
    }

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