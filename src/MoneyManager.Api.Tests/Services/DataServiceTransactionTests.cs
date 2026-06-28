using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

public class DataServiceTransactionTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public DataServiceTransactionTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    [Fact]
    public async Task GetTransactionsAsync_ExcludesHiddenAccounts()
    {
        var query = await _svc.DataService.GetTransactionsAsync();
        var transactions = await query.ToListAsync();

        // Hidden account transaction ("Internal Transfer") should be excluded
        transactions.Should().NotContain(t => t.Description == "Internal Transfer");
        transactions.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetTransactionsAsync_IncludesAccountAndCategory()
    {
        var query = await _svc.DataService.GetTransactionsAsync();
        var transactions = await query.ToListAsync();

        var grocery = transactions.First(t => t.Description == "Loblaws Groceries");
        grocery.Account.Should().NotBeNull();
        grocery.Account.Name.Should().Be("RBC Chequing");
        grocery.Category.Should().NotBeNull();
        grocery.Category!.Name.Should().Be("Groceries");
    }

    [Fact]
    public async Task ChangeTransactionAsync_UpdatesDescription()
    {
        var query = await _svc.DataService.GetTransactionsAsync();
        var transaction = await query.FirstAsync(t => t.Description == "Netflix");
        transaction.Description = "Netflix Updated";

        await _svc.DataService.ChangeTransactionAsync(transaction);

        using var ctx = _svc.Factory.CreateDbContext();
        var updated = await ctx.Transactions.FirstAsync(t => t.Id == transaction.Id);
        updated.Description.Should().Be("Netflix Updated");
    }

    [Fact]
    public async Task ChangeTransactionAsync_IgnoresNewTransaction()
    {
        var newTran = new Transaction
        {
            Id = 0,
            Description = "Should Not Save",
            OriginalDescription = "Test",
            Amount = 10m,
            IsDebit = true,
            Account = new Account { Name = "Temp", ShownName = "Temp" },
            Date = DateTime.Today
        };

        // Should not throw and should return all transactions
        var result = await _svc.DataService.ChangeTransactionAsync(newTran);
        var transactions = await result.ToListAsync();
        transactions.Should().NotContain(t => t.Description == "Should Not Save");
    }

    [Fact]
    public async Task UpdateTransactionAsync_NotFound_ReturnsNull()
    {
        (await _svc.DataService.UpdateTransactionAsync(9999, "x", null)).Should().BeNull();
    }

    [Fact]
    public async Task UpdateTransactionAsync_PatchesDescriptionOnly_LeavesCategory()
    {
        var query = await _svc.DataService.GetTransactionsAsync();
        var grocery = await query.FirstAsync(t => t.Description == "Loblaws Groceries");
        var categoryIdBefore = grocery.Category!.Id;

        var updated = await _svc.DataService.UpdateTransactionAsync(grocery.Id, "Renamed", null);

        updated.Should().NotBeNull();
        updated!.Description.Should().Be("Renamed");
        updated.Category!.Id.Should().Be(categoryIdBefore); // category untouched

        using var ctx = _svc.Factory.CreateDbContext();
        var persisted = await ctx.Transactions.FirstAsync(t => t.Id == grocery.Id);
        persisted.Description.Should().Be("Renamed");
    }

    [Fact]
    public async Task UpdateTransactionAsync_PatchesCategoryOnly_LeavesDescription()
    {
        int netflixId, foodId;
        using (var ctx = _svc.Factory.CreateDbContext())
        {
            netflixId = ctx.Transactions.First(t => t.Description == "Netflix").Id;
            foodId = ctx.Categories.First(c => c.Name == "Food").Id;
        }

        var updated = await _svc.DataService.UpdateTransactionAsync(netflixId, null, foodId);

        updated.Should().NotBeNull();
        updated!.Description.Should().Be("Netflix"); // description untouched
        updated.Category!.Id.Should().Be(foodId);
    }

    [Fact]
    public async Task GetTransactionsAsync_TransactionToDtoWorks()
    {
        var query = await _svc.DataService.GetTransactionsAsync();
        var transaction = await query.FirstAsync(t => t.Description == "Loblaws Groceries");
        var dto = transaction.ToDto();

        dto.Id.Should().Be(transaction.Id);
        dto.Amount.Should().Be(85.50m);
        dto.IsDebit.Should().BeTrue();
        dto.AmountExt.Should().Be(-85.50m);
        dto.Description.Should().Be("Loblaws Groceries");
    }

    [Fact]
    public async Task GetTransactionsAsync_CreditTransactionHasPositiveAmountExt()
    {
        var query = await _svc.DataService.GetTransactionsAsync();
        var salary = await query.FirstAsync(t => t.Description == "Salary Deposit");

        salary.AmountExt.Should().Be(3000m);
        salary.IsDebit.Should().BeFalse();
    }
}
