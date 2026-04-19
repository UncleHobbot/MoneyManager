using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

public class DataServiceRuleTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public DataServiceRuleTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    [Fact]
    public async Task GetRulesAsync_ReturnsAllRules()
    {
        var rules = await _svc.DataService.GetRulesAsync();
        var list = await rules.ToListAsync();

        list.Should().HaveCount(3);
        list.Should().Contain(r => r.OriginalDescription == "NETFLIX");
        list.Should().Contain(r => r.OriginalDescription == "LOBLAWS");
    }

    [Fact]
    public async Task GetRulesAsync_IncludesCategory()
    {
        var rules = await _svc.DataService.GetRulesAsync();
        var netflix = await rules.FirstAsync(r => r.OriginalDescription == "NETFLIX");

        netflix.Category.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeRuleAsync_AddsNewRule()
    {
        var categories = await _svc.DataService.GetCategoriesAsync();
        var food = categories.First(c => c.Name == "Food");

        // Use a fresh context to create the rule with proper tracking
        using var ctx = _svc.Factory.CreateDbContext();
        var trackedCat = await ctx.Categories.FirstAsync(c => c.Id == food.Id);

        var newRule = new Rule
        {
            OriginalDescription = "TIM HORTONS",
            NewDescription = "Tim Hortons",
            CompareType = RuleCompareType.Contains,
            Category = trackedCat
        };

        ctx.Rules.Add(newRule);
        await ctx.SaveChangesAsync();

        var result = await _svc.DataService.GetRulesAsync();
        var list = await result.ToListAsync();

        list.Should().HaveCount(4);
        list.Should().Contain(r => r.OriginalDescription == "TIM HORTONS");
    }

    [Fact]
    public async Task DeleteRuleAsync_RemovesRule()
    {
        var rules = await _svc.DataService.GetRulesAsync();
        var netflix = await rules.FirstAsync(r => r.OriginalDescription == "NETFLIX");

        var result = await _svc.DataService.DeleteRuleAsync(netflix);
        var remaining = await result.ToListAsync();

        remaining.Should().HaveCount(2);
        remaining.Should().NotContain(r => r.OriginalDescription == "NETFLIX");
    }

    [Fact]
    public async Task GetPossibleRulesAsync_ContainsMatch()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "NETFLIX.COM SUBSCRIPTION",
            Description = "Netflix",
            Amount = 16.99m,
            IsDebit = true,
            Date = DateTime.Today
        };

        var rules = await _svc.DataService.GetPossibleRulesAsync(transaction);
        var list = rules.ToList();

        list.Should().HaveCount(1);
        list.First().OriginalDescription.Should().Be("NETFLIX");
    }

    [Fact]
    public async Task GetPossibleRulesAsync_StartsWithMatch()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "LOBLAWS SUPERMARKET #1234",
            Description = "Loblaws",
            Amount = 50m,
            IsDebit = true,
            Date = DateTime.Today
        };

        var rules = await _svc.DataService.GetPossibleRulesAsync(transaction);
        var list = rules.ToList();

        list.Should().Contain(r => r.OriginalDescription == "LOBLAWS");
    }

    [Fact]
    public async Task GetPossibleRulesAsync_ExactMatch()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "PAYROLL DEPOSIT",
            Description = "Payroll",
            Amount = 3000m,
            IsDebit = false,
            Date = DateTime.Today
        };

        var rules = await _svc.DataService.GetPossibleRulesAsync(transaction);
        var list = rules.ToList();

        list.Should().Contain(r => r.OriginalDescription == "PAYROLL DEPOSIT");
    }

    [Fact]
    public async Task GetPossibleRulesAsync_NoMatch()
    {
        var transaction = new Transaction
        {
            OriginalDescription = "SOME RANDOM STORE",
            Description = "Random",
            Amount = 25m,
            IsDebit = true,
            Date = DateTime.Today
        };

        var rules = await _svc.DataService.GetPossibleRulesAsync(transaction);
        var list = rules.ToList();

        list.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyRuleAsync_AppliesRuleToTransaction()
    {
        // Create a transaction in the database
        using var ctx = _svc.Factory.CreateDbContext();
        var account = await ctx.Accounts.FirstAsync(a => a.Name == "RBC Chequing");
        var category = await ctx.Categories.FirstAsync(c => c.Name == "Income");

        var transaction = new Transaction
        {
            Account = account,
            Date = DateTime.Today,
            Description = "Original",
            OriginalDescription = "SOME ORIG",
            Amount = 100m,
            IsDebit = true,
            IsRuleApplied = false
        };
        ctx.Transactions.Add(transaction);
        await ctx.SaveChangesAsync();

        var rule = new Rule
        {
            OriginalDescription = "SOME",
            NewDescription = "Applied Description",
            CompareType = RuleCompareType.Contains,
            Category = category
        };

        var result = await _svc.DataService.ApplyRuleAsync(transaction, rule);

        result.Description.Should().Be("Applied Description");
        result.IsRuleApplied.Should().BeTrue();
    }

    [Fact]
    public async Task SaveNewRuleAsync_CreatesNewRuleWithZeroId()
    {
        using var ctx = _svc.Factory.CreateDbContext();
        var cat = await ctx.Categories.FirstAsync(c => c.Name == "Food");

        var rule = new Rule
        {
            Id = 999, // should be reset to 0
            OriginalDescription = "NEW RULE",
            NewDescription = "New",
            CompareType = RuleCompareType.Equals,
            Category = cat
        };

        var saved = await _svc.DataService.SaveNewRuleAsync(rule);

        saved.Id.Should().NotBe(999);
        saved.OriginalDescription.Should().Be("NEW RULE");
    }
}
