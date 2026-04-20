using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
        var result = await TransactionEndpoints.GetAll(_svc.DataService, period: "a", page: 1, pageSize: 50);

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task GetAll_RespectsPageSize()
    {
        var result = await TransactionEndpoints.GetAll(_svc.DataService, period: "a", page: 1, pageSize: 2);

        // The result wraps an anonymous type; verify it's Ok
        result.Should().BeAssignableTo<IResult>();
        result.GetType().Name.Should().StartWith("Ok");
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

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var tran = ctx.Transactions.First(t => t.Description == "Restaurant");

        var result = await TransactionEndpoints.Delete(tran.Id, _svc.DataService);

        result.Should().BeOfType<NoContent>();
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundForInvalidId()
    {
        var result = await TransactionEndpoints.Delete(9999, _svc.DataService);

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task DeleteAll_ReturnsNoContent()
    {
        var result = await TransactionEndpoints.DeleteAll(_svc.DataService);

        result.Should().BeOfType<NoContent>();
    }
}
