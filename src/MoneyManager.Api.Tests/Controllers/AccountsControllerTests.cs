using FluentAssertions;
using MoneyManager.Api.Controllers;
using MoneyManager.Api.Data;
using MoneyManager.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;

namespace MoneyManager.Api.Tests.Controllers;

public class AccountsControllerTests : IDisposable
{
    private readonly ServiceBundle _svc;
    private readonly AccountsController _controller;

    public AccountsControllerTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
        _controller = new AccountsController(_svc.DataService);
    }

    public void Dispose() => _svc.Dispose();

    [Fact]
    public async Task GetAccounts_ReturnsOkWithAllAccounts()
    {
        var result = await _controller.GetAccounts();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var accounts = ok.Value.Should().BeAssignableTo<List<Account>>().Subject;
        accounts.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAccount_ReturnsOkForExistingId()
    {
        var all = await _svc.DataService.GetAccountsAsync();
        var id = all.First().Id;

        var result = await _controller.GetAccount(id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAccount_ReturnsNotFoundForInvalidId()
    {
        var result = await _controller.GetAccount(9999);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateAccount_ReturnsCreatedAtAction()
    {
        var account = new Account { Name = "New Savings", ShownName = "Savings", Type = 0 };

        var result = await _controller.CreateAccount(account);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var created = (result.Result as CreatedAtActionResult)!;
        var value = created.Value.Should().BeAssignableTo<Account>().Subject;
        value.Name.Should().Be("New Savings");
    }

    [Fact]
    public async Task CreateAccount_SetsIdToZero()
    {
        var account = new Account { Id = 999, Name = "Forced", ShownName = "Forced", Type = 0 };

        var result = await _controller.CreateAccount(account);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task UpdateAccount_ReturnsOkWithUpdatedList()
    {
        var all = await _svc.DataService.GetAccountsAsync();
        var account = all.First();
        account.ShownName = "Updated Name";

        var result = await _controller.UpdateAccount(account.Id, account);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var accounts = ok.Value.Should().BeAssignableTo<List<Account>>().Subject;
        accounts.Should().Contain(a => a.ShownName == "Updated Name");
    }

    [Fact]
    public async Task DeleteAccount_ReturnsNoContentForAccountWithoutTransactions()
    {
        // Create a fresh account with no transactions
        var account = new Account { Name = "Deletable", ShownName = "Deletable", Type = 0 };
        await _svc.DataService.ChangeAccountAsync(account);
        var all = await _svc.DataService.GetAccountsAsync();
        var deletable = all.First(a => a.Name == "Deletable");

        var result = await _controller.DeleteAccount(deletable.Id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteAccount_ReturnsConflictForAccountWithTransactions()
    {
        var all = await _svc.DataService.GetAccountsAsync();
        var chequing = all.First(a => a.Name == "RBC Chequing");

        var result = await _controller.DeleteAccount(chequing.Id);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task DeleteAccount_ReturnsConflictForNonExistentId()
    {
        var result = await _controller.DeleteAccount(9999);

        result.Should().BeOfType<ConflictObjectResult>();
    }
}
