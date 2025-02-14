﻿using Microsoft.FluentUI.AspNetCore.Components.Extensions;
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

        if (chartPeriod == "12") // last 12 months
            startDate = DateTime.Today.AddMonths(-12).StartOfMonth(Thread.CurrentThread.CurrentCulture);
        else if (chartPeriod == "y1") // this year
            startDate = new DateTime(DateTime.Today.Year, 1, 1);
        else if (chartPeriod == "y2") // last year
        {
            startDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
            endDate = new DateTime(DateTime.Today.Year, 1, 1);
        }
        else if (chartPeriod == "y3") // 2 years ago
        {
            startDate = new DateTime(DateTime.Today.Year - 2, 1, 1);
            endDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
        }
        else if (chartPeriod == "y12") // last + this year
            startDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
        else if (chartPeriod == "m1") // This month
            startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        else if (chartPeriod == "m2") // Last month
        {
            startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
            endDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }
        else if (chartPeriod is "w" or "w1") // Last 7 days
            startDate = DateTime.Today.AddDays(-7);
        else if (chartPeriod == "w2") // Last 14 days
            startDate = DateTime.Today.AddDays(-14);
        else if (chartPeriod == "w3") // Last 31 days
            startDate = DateTime.Today.AddDays(-31);
        else if (chartPeriod == "a") // All
            startDate = DateTime.MinValue;
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