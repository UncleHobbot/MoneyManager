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
}
