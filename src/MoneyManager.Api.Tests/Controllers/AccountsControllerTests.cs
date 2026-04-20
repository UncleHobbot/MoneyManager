using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MoneyManager.Api.Data;
using MoneyManager.Api.Endpoints;
using MoneyManager.Api.Tests.TestHelpers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MoneyManager.Api.Tests.Endpoints;

public class AccountEndpointsTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public AccountEndpointsTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    [Fact]
    public async Task GetAll_ReturnsOkWithAllAccounts()
    {
        var result = await AccountEndpoints.GetAll(_svc.DataService);

        var ok = result.Should().BeOfType<Ok<List<Account>>>().Subject;
        ok.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetById_ReturnsOkForExistingId()
    {
        var all = await _svc.DataService.GetAccountsAsync();
        var id = all.First().Id;

        var result = await AccountEndpoints.GetById(id, _svc.DataService);

        result.Should().BeOfType<Ok<Account>>();
    }

    [Fact]
    public async Task GetById_ReturnsNotFoundForInvalidId()
    {
        var result = await AccountEndpoints.GetById(9999, _svc.DataService);

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithAccount()
    {
        var account = new Account { Name = "New Savings", ShownName = "Savings", Type = 0 };

        var result = await AccountEndpoints.Create(account, _svc.DataService);

        var created = result.Should().BeOfType<Created<Account>>().Subject;
        created.Value!.Name.Should().Be("New Savings");
    }

    [Fact]
    public async Task Create_SetsIdToZero()
    {
        var account = new Account { Id = 999, Name = "Forced", ShownName = "Forced", Type = 0 };

        var result = await AccountEndpoints.Create(account, _svc.DataService);

        result.Should().BeOfType<Created<Account>>();
    }

    [Fact]
    public async Task Update_ReturnsOkWithUpdatedList()
    {
        var all = await _svc.DataService.GetAccountsAsync();
        var account = all.First();
        account.ShownName = "Updated Name";

        var result = await AccountEndpoints.Update(account.Id, account, _svc.DataService);

        var ok = result.Should().BeOfType<Ok<List<Account>>>().Subject;
        ok.Value.Should().Contain(a => a.ShownName == "Updated Name");
    }

    [Fact]
    public async Task Delete_ReturnsNoContentForAccountWithoutTransactions()
    {
        var account = new Account { Name = "Deletable", ShownName = "Deletable", Type = 0 };
        await _svc.DataService.ChangeAccountAsync(account);
        var all = await _svc.DataService.GetAccountsAsync();
        var deletable = all.First(a => a.Name == "Deletable");

        var result = await AccountEndpoints.Delete(deletable.Id, _svc.DataService);

        result.Should().BeOfType<NoContent>();
    }

    [Fact]
    public async Task Delete_ReturnsConflictForAccountWithTransactions()
    {
        var all = await _svc.DataService.GetAccountsAsync();
        var chequing = all.First(a => a.Name == "RBC Chequing");

        var result = await AccountEndpoints.Delete(chequing.Id, _svc.DataService);

        result.Should().BeAssignableTo<IResult>();
        result.GetType().Name.Should().Contain("Conflict");
    }

    [Fact]
    public async Task Delete_ReturnsConflictForNonExistentId()
    {
        var result = await AccountEndpoints.Delete(9999, _svc.DataService);

        result.GetType().Name.Should().Contain("Conflict");
    }
}
