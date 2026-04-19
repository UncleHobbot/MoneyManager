using FluentAssertions;
using MoneyManager.Api.Controllers;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;

namespace MoneyManager.Api.Tests.Controllers;

public class TransactionsControllerTests : IDisposable
{
    private readonly ServiceBundle _svc;
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
        _controller = new TransactionsController(_svc.DataService);
    }

    public void Dispose() => _svc.Dispose();

    [Fact]
    public async Task GetTransactions_ReturnsOkWithPaginatedResult()
    {
        var result = await _controller.GetTransactions(period: "a", page: 1, pageSize: 50);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTransactions_RespectsPageSize()
    {
        var result = await _controller.GetTransactions(period: "a", page: 1, pageSize: 2);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        // Check dynamic object has items
        var itemsProp = value.GetType().GetProperty("items");
        itemsProp.Should().NotBeNull();
        var items = itemsProp!.GetValue(value) as IList<TransactionDto>;
        items.Should().NotBeNull();
        items!.Count.Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetTransaction_ReturnsOkForExistingTransaction()
    {
        // Get a transaction id from the database
        using var ctx = _svc.Factory.CreateDbContext();
        var tran = ctx.Transactions.First(t => t.Description == "Loblaws Groceries");

        var result = await _controller.GetTransaction(tran.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTransaction_ReturnsNotFoundForInvalidId()
    {
        var result = await _controller.GetTransaction(9999);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateTransaction_UpdatesDescription()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var tran = ctx.Transactions.First(t => t.Description == "Netflix");

        var request = new UpdateTransactionRequest { Description = "Netflix Premium" };
        var result = await _controller.UpdateTransaction(tran.Id, request);

        result.Result.Should().BeOfType<OkObjectResult>();
        var ok = (result.Result as OkObjectResult)!;
        var dto = ok.Value.Should().BeAssignableTo<TransactionDto>().Subject;
        dto.Description.Should().Be("Netflix Premium");
    }

    [Fact]
    public async Task UpdateTransaction_UpdatesCategory()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var tran = ctx.Transactions.First(t => t.Description == "Netflix");
        var categories = await _svc.DataService.GetCategoriesAsync();
        var food = categories.First(c => c.Name == "Food");

        var request = new UpdateTransactionRequest { CategoryId = food.Id };
        var result = await _controller.UpdateTransaction(tran.Id, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateTransaction_ReturnsNotFoundForInvalidId()
    {
        var request = new UpdateTransactionRequest { Description = "Test" };
        var result = await _controller.UpdateTransaction(9999, request);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteTransaction_ReturnsNoContent()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var tran = ctx.Transactions.First(t => t.Description == "Restaurant");

        var result = await _controller.DeleteTransaction(tran.Id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTransaction_ReturnsNotFoundForInvalidId()
    {
        var result = await _controller.DeleteTransaction(9999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteAllTransactions_ReturnsNoContent()
    {
        var result = await _controller.DeleteAllTransactions();

        result.Should().BeOfType<NoContentResult>();
    }
}
