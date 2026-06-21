using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Query;
using MoneyManager.Api.Services;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

public class TransactionQueryServiceTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public TransactionQueryServiceTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    private static TransactionFilters FiltersForAllSeed() =>
        new(StartDate: new DateTime(2024, 12, 1), EndDate: new DateTime(2025, 12, 1));

    // ----------------------------------------------------------------
    // GetPage — listability invariant
    // ----------------------------------------------------------------

    [Fact]
    public async Task GetPage_ExcludesHiddenAccounts()
    {
        var page = await _svc.QueryService.GetPageAsync(
            FiltersForAllSeed(), new TransactionSort(), new Paging(1, 50));

        page.Items.Should().NotContain(t => t.Description == "Internal Transfer");
        page.TotalCount.Should().Be(4);
    }

    // ----------------------------------------------------------------
    // GetPage — empty results
    // ----------------------------------------------------------------

    [Fact]
    public async Task GetPage_ReturnsEmpty_WhenNoTransactionsMatch()
    {
        var filters = new TransactionFilters(
            StartDate: new DateTime(2025, 6, 1),
            EndDate: new DateTime(2025, 7, 1));

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging());

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }

    // ----------------------------------------------------------------
    // GetPage — filters
    // ----------------------------------------------------------------

    [Fact]
    public async Task GetPage_FiltersByDateWindow()
    {
        var filters = new TransactionFilters(
            StartDate: new DateTime(2025, 2, 1),
            EndDate: new DateTime(2025, 3, 1));

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 50));

        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(t =>
            t.Description == "Netflix" || t.Description == "Restaurant");
    }

    [Fact]
    public async Task GetPage_FiltersByAccount()
    {
        int chequingId;
        using (var ctx = _svc.Factory.CreateDbContext())
            chequingId = ctx.Accounts.First(a => a.Name == "RBC Chequing").Id;

        var filters = FiltersForAllSeed() with { AccountId = chequingId };

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 50));

        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(t => t.Account.Name == "RBC Chequing");
    }

    [Fact]
    public async Task GetPage_FiltersByCategory()
    {
        int foodId;
        using (var ctx = _svc.Factory.CreateDbContext())
            foodId = ctx.Categories.First(c => c.Name == "Food").Id;

        var filters = FiltersForAllSeed() with { CategoryId = foodId };

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 50));

        page.Items.Should().ContainSingle(t => t.Description == "Restaurant");
        page.Items.Should().NotContain(t => t.Description == "Loblaws Groceries"); // Groceries is child of Food, not Food itself
    }

    [Fact]
    public async Task GetPage_FiltersBySearch_OnDescription()
    {
        var filters = FiltersForAllSeed() with { Search = "Loblaws" };

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 50));

        page.Items.Should().ContainSingle(t => t.Description == "Loblaws Groceries");
    }

    [Fact]
    public async Task GetPage_FiltersBySearch_OnOriginalDescription()
    {
        var filters = FiltersForAllSeed() with { Search = "PAYROLL" };

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 50));

        page.Items.Should().ContainSingle(t => t.Description == "Salary Deposit");
    }

    [Fact]
    public async Task GetPage_FiltersUncategorized_IncludesBothNullAndNamedCategory()
    {
        // Seed only has Netflix with Category=null. Add one with the named "Uncategorized" category
        // so we exercise both branches of the predicate (Category == null OR Name == "uncategorized").
        using (var ctx = _svc.Factory.CreateDbContext())
        {
            var uncategorized = ctx.Categories.First(c => c.Name == "Uncategorized");
            var chequing = ctx.Accounts.First(a => a.Name == "RBC Chequing");
            ctx.Transactions.Add(new Transaction
            {
                Account = chequing,
                Date = new DateTime(2025, 1, 25),
                Description = "Mystery Charge",
                OriginalDescription = "MYSTERY",
                Amount = 10m,
                IsDebit = true,
                Category = uncategorized,
            });
            await ctx.SaveChangesAsync();
        }

        var filters = FiltersForAllSeed() with { Uncategorized = true };

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 50));

        page.Items.Should().Contain(t => t.Description == "Netflix");            // null branch
        page.Items.Should().Contain(t => t.Description == "Mystery Charge");     // named branch
        page.Items.Should().NotContain(t => t.Description == "Loblaws Groceries");
        page.Items.Should().NotContain(t => t.Description == "Salary Deposit");
        page.Items.Should().NotContain(t => t.Description == "Restaurant");
    }

    // ----------------------------------------------------------------
    // GetPage — sort
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("date",        SortDirection.Ascending,  new[] { "Loblaws Groceries", "Salary Deposit", "Netflix", "Restaurant" })]
    [InlineData("date",        SortDirection.Descending, new[] { "Restaurant", "Netflix", "Salary Deposit", "Loblaws Groceries" })]
    [InlineData("amount",      SortDirection.Ascending,  new[] { "Loblaws Groceries", "Netflix", "Restaurant", "Salary Deposit" })]
    [InlineData("amount",      SortDirection.Descending, new[] { "Salary Deposit", "Restaurant", "Netflix", "Loblaws Groceries" })]
    [InlineData("description", SortDirection.Ascending,  new[] { "Loblaws Groceries", "Netflix", "Restaurant", "Salary Deposit" })]
    [InlineData("description", SortDirection.Descending, new[] { "Salary Deposit", "Restaurant", "Netflix", "Loblaws Groceries" })]
    public async Task GetPage_SortsBy(string field, SortDirection direction, string[] expectedDescriptions)
    {
        var sort = new TransactionSort(field, direction);

        var page = await _svc.QueryService.GetPageAsync(FiltersForAllSeed(), sort, new Paging(1, 50));

        page.Items.Select(t => t.Description).Should().Equal(expectedDescriptions);
    }

    [Fact]
    public async Task GetPage_SortFallsBackToDate_ForUnknownField()
    {
        var sort = new TransactionSort("unknown-field", SortDirection.Ascending);

        var page = await _svc.QueryService.GetPageAsync(FiltersForAllSeed(), sort, new Paging(1, 50));

        // Same expectation as the "date ascending" case
        page.Items.Select(t => t.Description).Should().Equal(
            "Loblaws Groceries", "Salary Deposit", "Netflix", "Restaurant");
    }

    // ----------------------------------------------------------------
    // GetPage — pagination
    // ----------------------------------------------------------------

    [Fact]
    public async Task GetPage_PaginatesCorrectly()
    {
        var sort = new TransactionSort("date", SortDirection.Ascending);

        var page1 = await _svc.QueryService.GetPageAsync(FiltersForAllSeed(), sort, new Paging(1, 2));
        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(4);
        page1.PageNumber.Should().Be(1);
        page1.PageSize.Should().Be(2);

        var page2 = await _svc.QueryService.GetPageAsync(FiltersForAllSeed(), sort, new Paging(2, 2));
        page2.Items.Should().HaveCount(2);
        page2.TotalCount.Should().Be(4);

        var page3 = await _svc.QueryService.GetPageAsync(FiltersForAllSeed(), sort, new Paging(3, 2));
        page3.Items.Should().BeEmpty();
        page3.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task GetPage_AppliesFiltersBeforePaging()
    {
        // Filter to a single transaction; ask for page 1 of size 2.
        var filters = FiltersForAllSeed() with { Search = "Loblaws" };

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 2));

        page.Items.Should().HaveCount(1);
        page.TotalCount.Should().Be(1);
    }

    // ----------------------------------------------------------------
    // GetPage — DTO projection
    // ----------------------------------------------------------------

    [Fact]
    public async Task GetPage_ReturnsDtoProjection_WithAccountAndCategoryLoaded()
    {
        var page = await _svc.QueryService.GetPageAsync(FiltersForAllSeed(), new TransactionSort(), new Paging(1, 50));

        var grocery = page.Items.Single(t => t.Description == "Loblaws Groceries");
        grocery.Account.Should().NotBeNull();
        grocery.Account.Name.Should().Be("RBC Chequing");
        grocery.Category.Should().NotBeNull();
        grocery.Category!.Name.Should().Be("Groceries");
        grocery.Amount.Should().Be(85.50m);
        grocery.AmountExt.Should().Be(-85.50m);
    }

    // ----------------------------------------------------------------
    // GetStats
    // ----------------------------------------------------------------

    [Fact]
    public async Task GetStats_AggregatesIncomeAndExpenses()
    {
        var stats = await _svc.QueryService.GetStatsAsync(FiltersForAllSeed());

        stats.Income.Should().Be(3000m);        // Salary Deposit
        stats.Expenses.Should().Be(114.99m);    // 85.50 + 16.99 + 12.50
        stats.Net.Should().Be(2885.01m);
        stats.Count.Should().Be(4);
    }

    [Fact]
    public async Task GetStats_ExcludesHiddenAccounts()
    {
        var stats = await _svc.QueryService.GetStatsAsync(FiltersForAllSeed());

        // Internal Transfer (Amount 500, debit, on hidden account) must not be in Expenses
        stats.Count.Should().Be(4);
        stats.Expenses.Should().Be(114.99m); // would be 614.99 if hidden account were included
    }

    [Fact]
    public async Task GetStats_RespectsFilters()
    {
        var filters = FiltersForAllSeed() with { Search = "PAYROLL" };

        var stats = await _svc.QueryService.GetStatsAsync(filters);

        stats.Income.Should().Be(3000m);
        stats.Expenses.Should().Be(0m);
        stats.Count.Should().Be(1);
    }
}
