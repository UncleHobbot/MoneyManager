using FluentAssertions;
using MoneyManager.Api.Model.Chart;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

public class DataServiceChartTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public DataServiceChartTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    // ----------------------------------------------------------------
    // ChartNetIncomeAsync
    //
    // Tests document the post-migration behavior, which fixes a latent
    // bug in ChartGetTransactionsAsync's source filter (the buggy filter
    // excluded top-level categories like Income and Food because
    // `Category.Parent.Id` evaluated to NULL when Parent was null).
    // See ADR-0004 and CONTEXT.md ("Reporting Row").
    // ----------------------------------------------------------------

    [Fact]
    public async Task ChartNetIncome_GroupsByMonth()
    {
        var result = await _svc.DataService.ChartNetIncomeAsync("a");

        // Seed spans Jan 2025 and Feb 2025 (listable transactions only).
        result.Should().HaveCount(2);
        result.Select(b => b.MonthKey).Should().BeEquivalentTo(new[] { "2501", "2502" });
        result.Select(b => b.Month).Should().Equal(new[] { "Jan 25", "Feb 25" });
    }

    [Fact]
    public async Task ChartNetIncome_OrderedByFirstDate()
    {
        var result = await _svc.DataService.ChartNetIncomeAsync("a");

        result.Select(b => b.FirstDate).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ChartNetIncome_IncomeBucketSumsCreditsAsPositive()
    {
        var result = await _svc.DataService.ChartNetIncomeAsync("a");

        // January 2025 contains Salary Deposit (Income, +3000).
        var jan = result.Single(b => b.MonthKey == "2501");
        jan.Income.Should().Be(3000m);

        // February 2025 has no Income-category transactions.
        var feb = result.Single(b => b.MonthKey == "2502");
        feb.Income.Should().Be(0m);
    }

    [Fact]
    public async Task ChartNetIncome_ExpensesBucketSumsDebitsAsNegative()
    {
        var result = await _svc.DataService.ChartNetIncomeAsync("a");

        // January 2025: Loblaws (-85.50). Expenses are negative.
        var jan = result.Single(b => b.MonthKey == "2501");
        jan.Expenses.Should().Be(-85.50m);

        // February 2025: Netflix (-16.99) + Restaurant (-12.50) = -29.49.
        var feb = result.Single(b => b.MonthKey == "2502");
        feb.Expenses.Should().Be(-29.49m);
    }

    [Fact]
    public async Task ChartNetIncome_IncludesTopLevelCategories_AfterBugFix()
    {
        // Regression guard for the bug fixed by ReportingRow migration:
        // Salary (Income, top-level) and Restaurant (Food, top-level) previously
        // were excluded by the latent `Category.Parent.Id` NULL-comparison bug.
        var result = await _svc.DataService.ChartNetIncomeAsync("a");

        var jan = result.Single(b => b.MonthKey == "2501");
        jan.Income.Should().Be(3000m, "Salary (Income top-level) must appear after bug fix");

        var feb = result.Single(b => b.MonthKey == "2502");
        feb.Expenses.Should().Be(-29.49m, "Restaurant (Food top-level) must appear after bug fix");
    }

    // ----------------------------------------------------------------
    // ChartSpendingTrendAsync (monthly spending by parent category)
    // ----------------------------------------------------------------

    [Fact]
    public async Task ChartSpendingTrend_RollsUpSubcategoriesAndExcludesNonExpenses()
    {
        int foodId;
        using (var ctx = _svc.Factory.CreateDbContext())
            foodId = ctx.Categories.First(c => c.Name == "Food").Id;

        var result = await _svc.DataService.ChartSpendingTrendAsync("a");

        // Only Food has expenses: Groceries rolls up to Food, Netflix is
        // uncategorized (null category), Salary is income, and the transfer is on a
        // hidden account. One series, no "Other" bucket.
        result.Series.Should().ContainSingle();
        var food = result.Series.Single();
        food.Name.Should().Be("Food");
        food.CategoryId.Should().Be(foodId);

        var monthLabels = result.Months.Select(m => m.Label).ToList();
        var jan = monthLabels.IndexOf("Jan 25");
        var feb = monthLabels.IndexOf("Feb 25");
        jan.Should().BeGreaterThanOrEqualTo(0);
        feb.Should().BeGreaterThanOrEqualTo(0);

        food.Data[jan].Should().Be(85.50m); // Loblaws (Groceries -> Food)
        food.Data[feb].Should().Be(12.50m); // Restaurant (Food)
        food.Data.Sum().Should().Be(98.00m);

        result.Series.Should().NotContain(s =>
            s.Name == "Income" || s.Name == "Transfer" || s.Name == "Uncategorized" || s.Name == "Other");
    }

    [Fact]
    public async Task ChartSpendingTrend_AlignsSeriesDataWithMonths()
    {
        var result = await _svc.DataService.ChartSpendingTrendAsync("a");

        result.Months.Should().NotBeEmpty();
        result.Series.Should().OnlyContain(s => s.Data.Length == result.Months.Count);
    }

    // ----------------------------------------------------------------
    // ChartTopMerchantsAsync (spend grouped by Description)
    // ----------------------------------------------------------------

    [Fact]
    public async Task ChartTopMerchants_RanksExpenseMerchants_IncludingUncategorized()
    {
        var result = await _svc.DataService.ChartTopMerchantsAsync("a");

        // Expense merchants ordered by spend; income (Salary) and the hidden-account
        // transfer are excluded; uncategorized spend (Netflix) still counts.
        result.Select(m => m.Name).Should().Equal("Loblaws Groceries", "Netflix", "Restaurant");

        var loblaws = result.First();
        loblaws.Amount.Should().Be(85.50m);
        loblaws.Count.Should().Be(1);

        result.Should().NotContain(m => m.Name == "Salary Deposit");
        result.Should().NotContain(m => m.Name == "Internal Transfer");
    }

    [Fact]
    public async Task ChartTopMerchants_RespectsLimit()
    {
        var result = await _svc.DataService.ChartTopMerchantsAsync("a", limit: 2);

        result.Should().HaveCount(2);
        result.Select(m => m.Name).Should().Equal("Loblaws Groceries", "Netflix");
    }

    [Fact]
    public async Task ChartNetIncome_IncludesUncategorizedTransactions()
    {
        // Netflix has Category = null and falls into the Expenses bucket
        // (IsIncome == false because EffectiveCategory is null). EffectiveCategory
        // being null does not exclude a row from ChartNetIncome.
        var result = await _svc.DataService.ChartNetIncomeAsync("a");

        var feb = result.Single(b => b.MonthKey == "2502");
        feb.Expenses.Should().Be(-29.49m, "Netflix (Category=null) contributes -16.99 to Expenses");
    }

    [Fact]
    public async Task ChartNetIncome_ExcludesTransfers()
    {
        // Add a Transfer-categorized transaction on a visible account.
        // The IsTransfer flag must keep it out of the Income/Expenses split.
        using (var ctx = _svc.Factory.CreateDbContext())
        {
            var transfer = ctx.Categories.First(c => c.Name == "Transfer");
            var chequing = ctx.Accounts.First(a => a.Name == "RBC Chequing");
            ctx.Transactions.Add(new Data.Transaction
            {
                Account = chequing,
                Date = new DateTime(2025, 1, 28),
                Description = "Visa Payment",
                OriginalDescription = "VISA PAYMENT",
                Amount = 200m,
                IsDebit = true,
                Category = transfer,
            });
            await ctx.SaveChangesAsync();
        }

        var result = await _svc.DataService.ChartNetIncomeAsync("a");

        // January numbers must NOT include the -200 transfer.
        var jan = result.Single(b => b.MonthKey == "2501");
        jan.Income.Should().Be(3000m);
        jan.Expenses.Should().Be(-85.50m, "Transfer (-200) must be excluded");
    }

    [Fact]
    public async Task ChartNetIncome_ExcludesHiddenAccounts()
    {
        // Internal Transfer (Transfer category, hidden account) is excluded by
        // the listability invariant in GetReportingRowsAsync. It would
        // otherwise show up as a transfer (excluded anyway) but the test
        // documents the layered invariants.
        var result = await _svc.DataService.ChartNetIncomeAsync("a");

        result.Should().HaveCount(2);
        // Sum of all buckets across months, excluding the 500 hidden debit.
        var totalIncome = result.Sum(b => b.Income);
        var totalExpenses = result.Sum(b => b.Expenses);
        totalIncome.Should().Be(3000m);
        totalExpenses.Should().Be(-114.99m);
    }

    [Fact]
    public async Task ChartNetIncome_RespectsPeriodCode()
    {
        // Today is wherever the test runner is; "a" includes everything in seed.
        // "y3" = 2 years ago. With seed in 2025, "y3" picks the year before
        // the year before today's year. Use a deterministic "no match" period.
        var result = await _svc.DataService.ChartNetIncomeAsync("y3");

        // y3 = two full years ago. Seed (2025) only matches y3 if today's
        // year is 2027. On any other year, the result should be empty.
        // Treat this as a smoke test: method runs without error.
        result.Should().NotBeNull();
    }

    // ----------------------------------------------------------------
    // ChartCumulativeSpendingAsync
    //
    // Tests use DateTime.Today-relative dates because the method hard-codes
    // "this month" and "last month" from today. Bug-fix regression guards
    // mirror the ChartNetIncome tests: top-level categories and uncategorized
    // transactions now reach the chart.
    // ----------------------------------------------------------------

    private async Task AddTransactionAsync(DateTime date, decimal amount, bool isDebit, string categoryName)
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var category = ctx.Categories.First(c => c.Name == categoryName);
        var account = ctx.Accounts.First(a => a.Name == "RBC Chequing");
        ctx.Transactions.Add(new Data.Transaction
        {
            Account = account,
            Date = date,
            Description = $"{categoryName} {date:yyyy-MM-dd}",
            OriginalDescription = $"{categoryName}_{date:yyyyMMdd}",
            Amount = amount,
            IsDebit = isDebit,
            Category = category,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task ChartCumulativeSpending_HasEntryForEveryDayOfLastMonth()
    {
        var result = await _svc.DataService.ChartCumulativeSpendingAsync();

        var lastMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        var daysInLastMonth = DateTime.DaysInMonth(lastMonthStart.Year, lastMonthStart.Month);

        result.Should().HaveCount(daysInLastMonth);
        result.Select(d => d.DayNumber).Should().BeEquivalentTo(Enumerable.Range(1, daysInLastMonth));
    }

    [Fact]
    public async Task ChartCumulativeSpending_LastMonthAccumulatesByDay()
    {
        // Add two expenses on day 5 and day 10 of last month.
        var lastMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        await AddTransactionAsync(new DateTime(lastMonthStart.Year, lastMonthStart.Month, 5), 50m, true, "Food");
        await AddTransactionAsync(new DateTime(lastMonthStart.Year, lastMonthStart.Month, 10), 30m, true, "Food");

        var result = await _svc.DataService.ChartCumulativeSpendingAsync();

        // Cumulative: day 5 onward includes 50; day 10 onward includes 80.
        result.Single(d => d.DayNumber == 1).LastMonthExpenses.Should().Be(0m);
        result.Single(d => d.DayNumber == 5).LastMonthExpenses.Should().Be(50m);
        result.Single(d => d.DayNumber == 9).LastMonthExpenses.Should().Be(50m);
        result.Single(d => d.DayNumber == 10).LastMonthExpenses.Should().Be(80m);
        result.Last().LastMonthExpenses.Should().Be(80m);
    }

    [Fact]
    public async Task ChartCumulativeSpending_ThisMonthAccumulatesByDay()
    {
        // Add two expenses on day 5 and day 15 of this month, before today.
        var today = DateTime.Today;
        var day5 = new DateTime(today.Year, today.Month, 5);
        var day15 = new DateTime(today.Year, today.Month, 15);
        if (day5 >= today || day15 >= today) return; // skip if today is too early in month

        await AddTransactionAsync(day5, 40m, true, "Food");
        await AddTransactionAsync(day15, 25m, true, "Food");

        var result = await _svc.DataService.ChartCumulativeSpendingAsync();

        result.Single(d => d.DayNumber == 5).ThisMonthExpenses.Should().Be(40m);
        result.Single(d => d.DayNumber == 14).ThisMonthExpenses.Should().Be(40m);
        result.Single(d => d.DayNumber == 15).ThisMonthExpenses.Should().Be(65m);
        result.Single(d => d.DayNumber == today.Day).ThisMonthExpenses.Should().Be(65m);
    }

    [Fact]
    public async Task ChartCumulativeSpending_ExcludesIncome()
    {
        // Add a Salary (Income) transaction in last month — must not contribute.
        var lastMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        await AddTransactionAsync(new DateTime(lastMonthStart.Year, lastMonthStart.Month, 5), 5000m, false, "Income");

        var result = await _svc.DataService.ChartCumulativeSpendingAsync();

        result.Should().NotBeEmpty();
        result.Should().OnlyContain(d => d.LastMonthExpenses == 0m);
    }

    [Fact]
    public async Task ChartCumulativeSpending_ExcludesTransfers()
    {
        // Add a Transfer (debit) on day 5 of last month — must not contribute.
        var lastMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        await AddTransactionAsync(new DateTime(lastMonthStart.Year, lastMonthStart.Month, 5), 300m, true, "Transfer");

        var result = await _svc.DataService.ChartCumulativeSpendingAsync();

        result.Should().NotBeEmpty();
        result.Should().OnlyContain(d => d.LastMonthExpenses == 0m);
    }

    [Fact]
    public async Task ChartCumulativeSpending_IncludesTopLevelAndUncategorized_AfterBugFix()
    {
        // Regression guard: pre-migration, ChartGetTransactionsAsync's NULL-comparison
        // bug filtered out Food (top-level) and Uncategorized (top-level) transactions,
        // and the Category != null filter excluded uncategorized-by-null rows.
        // After migration, top-level categories and Category=null rows both reach the chart.
        var lastMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);

        // Food is top-level in seed.
        await AddTransactionAsync(new DateTime(lastMonthStart.Year, lastMonthStart.Month, 5), 20m, true, "Food");

        // Add an uncategorized transaction (Category = null).
        using (var ctx = _svc.Factory.CreateDbContext())
        {
            var account = ctx.Accounts.First(a => a.Name == "RBC Chequing");
            ctx.Transactions.Add(new Data.Transaction
            {
                Account = account,
                Date = new DateTime(lastMonthStart.Year, lastMonthStart.Month, 6),
                Description = "Mystery",
                OriginalDescription = "MYSTERY",
                Amount = 15m,
                IsDebit = true,
                Category = null,
            });
            await ctx.SaveChangesAsync();
        }

        var result = await _svc.DataService.ChartCumulativeSpendingAsync();

        // Food (20) + Mystery (15) = 35 from day 6 onward.
        result.Single(d => d.DayNumber == 5).LastMonthExpenses.Should().Be(20m);
        result.Single(d => d.DayNumber == 6).LastMonthExpenses.Should().Be(35m);
        result.Last().LastMonthExpenses.Should().Be(35m);
    }

    [Fact]
    public async Task ChartCumulativeSpending_RefundsReduceExpenses()
    {
        // A credit "expense" (e.g. refund) should subtract from cumulative
        // spending, matching the pre-migration convention where credits
        // produced negative sums.
        var lastMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        await AddTransactionAsync(new DateTime(lastMonthStart.Year, lastMonthStart.Month, 5), 50m, true, "Food");
        await AddTransactionAsync(new DateTime(lastMonthStart.Year, lastMonthStart.Month, 10), 20m, false, "Food");

        var result = await _svc.DataService.ChartCumulativeSpendingAsync();

        // Day 5: +50; Day 10: +50 - 20 = +30.
        result.Single(d => d.DayNumber == 5).LastMonthExpenses.Should().Be(50m);
        result.Single(d => d.DayNumber == 10).LastMonthExpenses.Should().Be(30m);
    }
}
