using FluentAssertions;
using MoneyManager.Api.Services;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

public class BudgetServiceTests : IDisposable
{
    private readonly ServiceBundle _svc;
    private readonly BudgetService _budgets;

    public BudgetServiceTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
        _budgets = new BudgetService(_svc.Factory);
    }

    public void Dispose() => _svc.Dispose();

    private int CategoryId(string name)
    {
        using var ctx = _svc.Factory.CreateDbContext();
        return ctx.Categories.First(c => c.Name == name).Id;
    }

    [Fact]
    public async Task SetBudget_CreatesBudget_AndGetReturnsIt()
    {
        var foodId = CategoryId("Food");

        var dto = await _budgets.SetBudgetAsync(foodId, 600m);

        dto.Should().NotBeNull();
        dto!.CategoryId.Should().Be(foodId);
        dto.CategoryName.Should().Be("Food");
        dto.Amount.Should().Be(600m);

        var all = await _budgets.GetBudgetsAsync();
        all.Should().ContainSingle(b => b.CategoryId == foodId && b.Amount == 600m);
    }

    [Fact]
    public async Task SetBudget_Twice_UpdatesInPlace_NoDuplicate()
    {
        var foodId = CategoryId("Food");

        await _budgets.SetBudgetAsync(foodId, 600m);
        await _budgets.SetBudgetAsync(foodId, 750m);

        var all = await _budgets.GetBudgetsAsync();
        all.Where(b => b.CategoryId == foodId).Should().ContainSingle()
            .Which.Amount.Should().Be(750m);
    }

    [Fact]
    public async Task DeleteBudget_RemovesIt()
    {
        var foodId = CategoryId("Food");
        await _budgets.SetBudgetAsync(foodId, 600m);

        var deleted = await _budgets.DeleteBudgetAsync(foodId);

        deleted.Should().BeTrue();
        (await _budgets.GetBudgetsAsync()).Should().NotContain(b => b.CategoryId == foodId);
    }

    [Fact]
    public async Task SetBudget_UnknownCategory_ReturnsNull()
    {
        var dto = await _budgets.SetBudgetAsync(999999, 100m);
        dto.Should().BeNull();
    }
}
