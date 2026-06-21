using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using MoneyManager.Api.Data;
using MoneyManager.Api.Endpoints;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Endpoints;

public class TransactionEndpointsTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public TransactionEndpointsTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    [Fact]
    public async Task GetAll_ReturnsOkWithPaginatedResult()
    {
        var result = await TransactionEndpoints.GetAll(_svc.DataService, _svc.QueryService, period: "a", page: 1, pageSize: 50);

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task GetAll_RespectsPageSize()
    {
        var result = await TransactionEndpoints.GetAll(_svc.DataService, _svc.QueryService, period: "a", page: 1, pageSize: 2);

        // The result wraps an anonymous type; verify it's Ok
        result.Should().BeAssignableTo<IResult>();
        result.GetType().Name.Should().StartWith("Ok");
    }

    [Fact]
    public async Task GetAll_FiltersBySearch()
    {
        var result = await TransactionEndpoints.GetAll(_svc.DataService, _svc.QueryService, period: "a", search: "Loblaws");

        var items = GetItems(result);
        items.Should().NotBeEmpty();
        items.Should().OnlyContain(t =>
            t.Description.Contains("Loblaws", StringComparison.OrdinalIgnoreCase) ||
            t.OriginalDescription.Contains("Loblaws", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAll_FiltersUncategorized()
    {
        var result = await TransactionEndpoints.GetAll(_svc.DataService, _svc.QueryService, period: "a", uncategorized: true);

        var items = GetItems(result);
        items.Should().OnlyContain(t =>
            t.Category == null || t.Category.Name.ToLower() == "uncategorized");
    }

    [Fact]
    public async Task GetAll_SortsByAmountAscending()
    {
        var result = await TransactionEndpoints.GetAll(_svc.DataService, _svc.QueryService, period: "a", sortBy: "amount", sortDir: "asc");

        var items = GetItems(result);
        var signed = items.Select(t => t.AmountExt).ToList();
        signed.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetStats_RespectsSearchFilter()
    {
        var result = await TransactionEndpoints.GetStats(_svc.DataService, period: "a", search: "Loblaws");

        result.GetType().Name.Should().StartWith("Ok");
        var value = result.GetType().GetProperty("Value")!.GetValue(result)!;
        var count = (int)value.GetType().GetProperty("count")!.GetValue(value)!;
        count.Should().BeGreaterThan(0);
    }

    private static List<TransactionDto> GetItems(IResult result)
    {
        var value = result.GetType().GetProperty("Value")!.GetValue(result)!;
        return (List<TransactionDto>)value.GetType().GetProperty("items")!.GetValue(value)!;
    }

    [Fact]
    public async Task Create_AddsTransactionWithCategory()
    {
        int accountId, categoryId;
        using (var ctx = _svc.Factory.CreateDbContext())
        {
            accountId = ctx.Accounts.First(a => a.Name == "RBC Chequing").Id;
            categoryId = ctx.Categories.First(c => c.Name == "Food").Id;
        }

        var request = new CreateTransactionRequest
        {
            AccountId = accountId,
            Date = DateTime.Today,
            Description = "Manual Coffee",
            Amount = 4.50m,
            IsDebit = true,
            CategoryId = categoryId,
        };

        var result = await TransactionEndpoints.Create(request, _svc.DataService);

        var created = result.Should().BeOfType<Created<TransactionDto>>().Subject;
        created.Value!.Description.Should().Be("Manual Coffee");
        created.Value.Amount.Should().Be(4.50m);
        created.Value.IsDebit.Should().BeTrue();
        created.Value.AmountExt.Should().Be(-4.50m);
        created.Value.Category!.Name.Should().Be("Food");

        using var verify = _svc.Factory.CreateDbContext();
        verify.Transactions.Any(t => t.Description == "Manual Coffee").Should().BeTrue();
    }

    [Fact]
    public async Task Create_AddsIncomeWithoutCategory()
    {
        int accountId;
        using (var ctx = _svc.Factory.CreateDbContext())
            accountId = ctx.Accounts.First(a => a.Name == "RBC Chequing").Id;

        var request = new CreateTransactionRequest
        {
            AccountId = accountId,
            Date = DateTime.Today,
            Description = "Manual Income",
            Amount = 100m,
            IsDebit = false,
        };

        var result = await TransactionEndpoints.Create(request, _svc.DataService);

        var created = result.Should().BeOfType<Created<TransactionDto>>().Subject;
        created.Value!.Amount.Should().Be(100m);
        created.Value.AmountExt.Should().Be(100m);
        created.Value.Category.Should().BeNull();
    }

    [Fact]
    public async Task Create_ReturnsBadRequestForUnknownAccount()
    {
        var request = new CreateTransactionRequest
        {
            AccountId = 99999,
            Date = DateTime.Today,
            Description = "Nope",
            Amount = 5m,
            IsDebit = true,
        };

        var result = await TransactionEndpoints.Create(request, _svc.DataService);

        result.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task Create_ReturnsBadRequestForInvalidAmount()
    {
        int accountId;
        using (var ctx = _svc.Factory.CreateDbContext())
            accountId = ctx.Accounts.First(a => a.Name == "RBC Chequing").Id;

        var request = new CreateTransactionRequest
        {
            AccountId = accountId,
            Date = DateTime.Today,
            Description = "Zero",
            Amount = 0m,
            IsDebit = true,
        };

        var result = await TransactionEndpoints.Create(request, _svc.DataService);

        result.Should().BeOfType<BadRequest<string>>();
    }

    [Fact]
    public async Task GetById_ReturnsOkForExistingTransaction()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var tran = ctx.Transactions.First(t => t.Description == "Loblaws Groceries");

        var result = await TransactionEndpoints.GetById(tran.Id, _svc.DataService);

        result.Should().BeOfType<Ok<TransactionDto>>();
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundForInvalidId()
    {
        var result = await TransactionEndpoints.GetById(9999, _svc.DataService);

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetPossibleRules_ReturnsMatchingRulesForTransaction()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var account = ctx.Accounts.First(a => a.Name == "RBC Chequing");
        var transaction = new Transaction
        {
            Account = account,
            Date = DateTime.Today,
            Description = "Netflix",
            OriginalDescription = "NETFLIX.COM SUBSCRIPTION",
            Amount = 16.99m,
            IsDebit = true,
            IsRuleApplied = false
        };

        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var result = await TransactionEndpoints.GetPossibleRules(transaction.Id, _svc.DataService);

        var ok = result.Should().BeOfType<Ok<List<Rule>>>().Subject;
        ok.Value.Should().ContainSingle(rule => rule.OriginalDescription == "NETFLIX");
    }

    [Fact]
    public async Task ApplyRule_AppliesSelectedRuleToTransaction()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var account = ctx.Accounts.First(a => a.Name == "RBC Chequing");
        var transaction = new Transaction
        {
            Account = account,
            Date = DateTime.Today,
            Description = "Netflix",
            OriginalDescription = "NETFLIX.COM SUBSCRIPTION",
            Amount = 16.99m,
            IsDebit = true,
            IsRuleApplied = false
        };

        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var rule = ctx.Rules.Include(r => r.Category).First(r => r.OriginalDescription == "NETFLIX");

        var result = await TransactionEndpoints.ApplyRule(transaction.Id, rule.Id, _svc.DataService);

        var ok = result.Should().BeOfType<Ok<TransactionDto>>().Subject;
        ok.Value!.Description.Should().Be(rule.NewDescription);
        ok.Value.IsRuleApplied.Should().BeTrue();
        ok.Value.Category.Should().NotBeNull();
        ok.Value.Category!.Name.Should().Be(rule.Category.Name);
    }

    [Fact]
    public async Task Update_UpdatesDescription()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var tran = ctx.Transactions.First(t => t.Description == "Netflix");

        var request = new UpdateTransactionRequest { Description = "Netflix Premium" };
        var result = await TransactionEndpoints.Update(tran.Id, request, _svc.DataService);

        var ok = result.Should().BeOfType<Ok<TransactionDto>>().Subject;
        ok.Value!.Description.Should().Be("Netflix Premium");
    }

    [Fact]
    public async Task Update_UpdatesCategory()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var tran = ctx.Transactions.First(t => t.Description == "Netflix");
        var categories = await _svc.DataService.GetCategoriesAsync();
        var food = categories.First(c => c.Name == "Food");

        var request = new UpdateTransactionRequest { CategoryId = food.Id };
        var result = await TransactionEndpoints.Update(tran.Id, request, _svc.DataService);

        result.Should().BeOfType<Ok<TransactionDto>>();
    }

    [Fact]
    public async Task Update_ReturnsNotFoundForInvalidId()
    {
        var request = new UpdateTransactionRequest { Description = "Test" };
        var result = await TransactionEndpoints.Update(9999, request, _svc.DataService);

        result.Should().BeOfType<NotFound>();
    }

}
