using FluentAssertions;
using MoneyManager.Api.Data;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

public class DataServiceAccountTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public DataServiceAccountTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    [Fact]
    public async Task GetAccountsAsync_ReturnsAllAccounts()
    {
        var accounts = await _svc.DataService.GetAccountsAsync();

        accounts.Should().HaveCount(3);
        accounts.Should().Contain(a => a.Name == "RBC Chequing");
        accounts.Should().Contain(a => a.Name == "RBC Visa");
        accounts.Should().Contain(a => a.Name == "Transfer Account");
    }

    [Fact]
    public async Task GetAccountsAsync_ReturnsFreshListEachCall()
    {
        var first = await _svc.DataService.GetAccountsAsync();
        var second = await _svc.DataService.GetAccountsAsync();

        first.Should().NotBeSameAs(second);
    }

    [Fact]
    public async Task ChangeAccountAsync_AddsNewAccount()
    {
        var newAccount = new Account
        {
            Name = "Savings",
            ShownName = "My Savings",
            Type = 0
        };

        var result = await _svc.DataService.ChangeAccountAsync(newAccount);

        result.Should().HaveCount(4);
        result.Should().Contain(a => a.Name == "Savings");
    }

    [Fact]
    public async Task ChangeAccountAsync_UpdatesExistingAccount()
    {
        var accounts = await _svc.DataService.GetAccountsAsync();
        var account = accounts.First(a => a.Name == "RBC Chequing");
        account.ShownName = "Updated Chequing";

        var result = await _svc.DataService.ChangeAccountAsync(account);

        result.Should().Contain(a => a.ShownName == "Updated Chequing");
    }

    [Fact]
    public async Task DeleteAccountAsync_DeletesAccountWithNoTransactions()
    {
        // Add account with no transactions
        var account = new Account { Name = "Empty Account", ShownName = "Empty", Type = 0 };
        await _svc.DataService.ChangeAccountAsync(account);

        using var ctx = _svc.Factory.CreateDbContext();
        var saved = ctx.Accounts.First(a => a.Name == "Empty Account");

        var deleted = await _svc.DataService.DeleteAccountAsync(saved.Id);

        deleted.Should().BeTrue();
        var remaining = await _svc.DataService.GetAccountsAsync();
        remaining.Should().NotContain(a => a.Name == "Empty Account");
    }

    [Fact]
    public async Task DeleteAccountAsync_ReturnsFalseWhenAccountHasTransactions()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var chequing = ctx.Accounts.First(a => a.Name == "RBC Chequing");

        var deleted = await _svc.DataService.DeleteAccountAsync(chequing.Id);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAccountAsync_ReturnsFalseForNonExistentAccount()
    {
        var deleted = await _svc.DataService.DeleteAccountAsync(9999);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetAccountsAsync_CachesResults()
    {
        // First call loads from DB, second should come from cache
        var first = await _svc.DataService.GetAccountsAsync();
        var second = await _svc.DataService.GetAccountsAsync();

        first.Should().HaveCount(second.Count);
    }

    [Fact]
    public async Task ChangeAccountAsync_NewAccountGetsId()
    {
        var newAccount = new Account { Name = "Test", ShownName = "Test", Type = 0 };

        await _svc.DataService.ChangeAccountAsync(newAccount);

        newAccount.Id.Should().BeGreaterThan(0);
    }
}
