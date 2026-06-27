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

        // Subtree semantics: filtering by the Food parent includes Food itself and
        // its children (Groceries), so a chart drill reconciles with the rolled-up
        // slice total. See ADR-0005.
        page.Items.Should().Contain(t => t.Description == "Restaurant"); // Food itself
        page.Items.Should().Contain(t => t.Description == "Loblaws Groceries"); // Groceries is a child of Food
    }

    [Fact]
    public async Task GetPage_FiltersByCategory_LeafReturnsOnlyItsOwn()
    {
        int groceriesId;
        using (var ctx = _svc.Factory.CreateDbContext())
            groceriesId = ctx.Categories.First(c => c.Name == "Groceries").Id;

        var filters = FiltersForAllSeed() with { CategoryId = groceriesId };

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 50));

        // A leaf category has no children, so the subtree is just itself.
        page.Items.Should().Contain(t => t.Description == "Loblaws Groceries");
        page.Items.Should().NotContain(t => t.Description == "Restaurant");
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

    [Fact]
    public async Task GetPage_FiltersUncategorized_IncludesSubcategoriesOfUncategorized()
    {
        using (var ctx = _svc.Factory.CreateDbContext())
        {
            var uncategorized = ctx.Categories.First(c => c.Name == "Uncategorized");
            var withdrawal = new Category { Name = "Withdrawal", Parent = uncategorized };
            ctx.Categories.Add(withdrawal);
            var chequing = ctx.Accounts.First(a => a.Name == "RBC Chequing");
            ctx.Transactions.Add(new Transaction
            {
                Account = chequing,
                Date = new DateTime(2025, 1, 26),
                Description = "ATM Withdrawal",
                OriginalDescription = "ATM",
                Amount = 100m,
                IsDebit = true,
                Category = withdrawal,
            });
            await ctx.SaveChangesAsync();
        }

        var filters = FiltersForAllSeed() with { Uncategorized = true };

        var page = await _svc.QueryService.GetPageAsync(filters, new TransactionSort(), new Paging(1, 50));

        page.Items.Should().Contain(t => t.Description == "ATM Withdrawal");
        page.Items.Should().NotContain(t => t.Description == "Loblaws Groceries");
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

    // ----------------------------------------------------------------
    // GetReportingRows
    // ----------------------------------------------------------------

    [Fact]
    public async Task GetReportingRows_ReturnsAllListableTransactions()
    {
        var rows = await _svc.QueryService.GetReportingRowsAsync(FiltersForAllSeed());

        rows.Should().HaveCount(4);
        rows.Select(r => r.Date).Should().NotBeNull();
    }

    [Fact]
    public async Task GetReportingRows_ExcludesHiddenAccounts()
    {
        var rows = await _svc.QueryService.GetReportingRowsAsync(FiltersForAllSeed());

        // Internal Transfer is on the hidden "Transfer Account" and must be excluded
        // by the listability invariant, even though its category is Transfer.
        rows.Should().NotContain(r => r.IsTransfer && r.SignedAmount == -500m);
        rows.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetReportingRows_SignedAmount_PositiveForCredits_NegativeForDebits()
    {
        var rows = await _svc.QueryService.GetReportingRowsAsync(FiltersForAllSeed());

        var salary = rows.Single(r => r.Date == new DateTime(2025, 1, 20));
        salary.SignedAmount.Should().Be(3000m);    // credit (income direction)

        var loblaws = rows.Single(r => r.Date == new DateTime(2025, 1, 15));
        loblaws.SignedAmount.Should().Be(-85.50m); // debit (expense direction)
    }

    [Fact]
    public async Task GetReportingRows_EffectiveCategory_RollsUpToParent()
    {
        var rows = await _svc.QueryService.GetReportingRowsAsync(FiltersForAllSeed());

        // Loblaws is categorized as "Groceries" (child of "Food").
        // EffectiveCategory should be the parent "Food", not "Groceries".
        var loblaws = rows.Single(r => r.Date == new DateTime(2025, 1, 15));
        loblaws.EffectiveCategory.Should().NotBeNull();
        loblaws.EffectiveCategory!.Name.Should().Be("Food");
    }

    [Fact]
    public async Task GetReportingRows_EffectiveCategory_IsSelf_WhenNoParent()
    {
        var rows = await _svc.QueryService.GetReportingRowsAsync(FiltersForAllSeed());

        // Salary is categorized as top-level "Income" (no parent).
        // EffectiveCategory should be "Income" itself.
        var salary = rows.Single(r => r.Date == new DateTime(2025, 1, 20));
        salary.EffectiveCategory.Should().NotBeNull();
        salary.EffectiveCategory!.Name.Should().Be("Income");
    }

    [Fact]
    public async Task GetReportingRows_EffectiveCategory_IsNull_WhenTransactionHasNoCategory()
    {
        var rows = await _svc.QueryService.GetReportingRowsAsync(FiltersForAllSeed());

        // Netflix has Category = null. EffectiveCategory should be null,
        // and both flags should be false (no category to check against).
        var netflix = rows.Single(r => r.Date == new DateTime(2025, 2, 1));
        netflix.EffectiveCategory.Should().BeNull();
        netflix.IsIncome.Should().BeFalse();
        netflix.IsTransfer.Should().BeFalse();
    }

    [Fact]
    public async Task GetReportingRows_IsIncome_TrueForIncomeCategory()
    {
        var rows = await _svc.QueryService.GetReportingRowsAsync(FiltersForAllSeed());

        var salary = rows.Single(r => r.Date == new DateTime(2025, 1, 20));
        salary.IsIncome.Should().BeTrue();

        // All other listable rows should have IsIncome == false
        rows.Where(r => r.Date != new DateTime(2025, 1, 20))
            .Should().OnlyContain(r => !r.IsIncome);
    }

    [Fact]
    public async Task GetReportingRows_IsTransfer_TrueForTransferCategory_OnVisibleAccount()
    {
        // Seed's only Transfer-categorized transaction is on a hidden account
        // (excluded by listability). Add a Transfer row on a visible account
        // so we can exercise the IsTransfer flag.
        using (var ctx = _svc.Factory.CreateDbContext())
        {
            var transfer = ctx.Categories.First(c => c.Name == "Transfer");
            var chequing = ctx.Accounts.First(a => a.Name == "RBC Chequing");
            ctx.Transactions.Add(new Transaction
            {
                Account = chequing,
                Date = new DateTime(2025, 3, 1),
                Description = "Visa Payment",
                OriginalDescription = "VISA PAYMENT",
                Amount = 200m,
                IsDebit = true,
                Category = transfer,
            });
            await ctx.SaveChangesAsync();
        }

        var rows = await _svc.QueryService.GetReportingRowsAsync(FiltersForAllSeed());

        var visaPayment = rows.Single(r => r.Date == new DateTime(2025, 3, 1));
        visaPayment.IsTransfer.Should().BeTrue();
        visaPayment.IsIncome.Should().BeFalse();
    }

    [Fact]
    public async Task GetReportingRows_RespectsDateFilter()
    {
        var filters = new TransactionFilters(
            StartDate: new DateTime(2025, 2, 1),
            EndDate: new DateTime(2025, 3, 1));

        var rows = await _svc.QueryService.GetReportingRowsAsync(filters);

        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(r => r.Date >= new DateTime(2025, 2, 1) && r.Date < new DateTime(2025, 3, 1));
    }
}
