using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

/// <summary>
/// Tests for <see cref="MoneyManager.Api.Services.CategorizationService"/> — rule
/// matching and application. The matcher (<c>GetMatchingRulesAsync</c>) is the deep
/// core; the apply paths layer the "exactly one match" rule and persistence on top.
/// </summary>
public class CategorizationServiceTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public CategorizationServiceTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    private static Transaction Tx(string originalDescription) => new()
    {
        OriginalDescription = originalDescription,
        Description = "x",
        Amount = 10m,
        IsDebit = true,
        Date = DateTime.Today,
    };

    [Fact]
    public async Task GetMatchingRules_ContainsMatch()
    {
        var rules = await _svc.Categorization.GetMatchingRulesAsync(Tx("NETFLIX.COM SUBSCRIPTION"));

        rules.Should().ContainSingle(r => r.OriginalDescription == "NETFLIX");
    }

    [Fact]
    public async Task GetMatchingRules_StartsWithMatch()
    {
        var rules = await _svc.Categorization.GetMatchingRulesAsync(Tx("LOBLAWS SUPERMARKET #1234"));

        rules.Should().Contain(r => r.OriginalDescription == "LOBLAWS");
    }

    [Fact]
    public async Task GetMatchingRules_ExactMatch()
    {
        var rules = await _svc.Categorization.GetMatchingRulesAsync(Tx("PAYROLL DEPOSIT"));

        rules.Should().Contain(r => r.OriginalDescription == "PAYROLL DEPOSIT");
    }

    [Fact]
    public async Task GetMatchingRules_NoMatch()
    {
        var rules = await _svc.Categorization.GetMatchingRulesAsync(Tx("SOME RANDOM STORE"));

        rules.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyRule_ById_AppliesAndPersists()
    {
        int transactionId, ruleId;
        using (var ctx = _svc.Factory.CreateDbContext())
        {
            var account = await ctx.Accounts.FirstAsync(a => a.Name == "RBC Chequing");
            var transaction = new Transaction
            {
                Account = account,
                Date = DateTime.Today,
                Description = "Original",
                OriginalDescription = "SOME ORIG",
                Amount = 100m,
                IsDebit = true,
                IsRuleApplied = false,
            };
            ctx.Transactions.Add(transaction);
            await ctx.SaveChangesAsync();
            transactionId = transaction.Id;
            ruleId = (await ctx.Rules.FirstAsync(r => r.OriginalDescription == "NETFLIX")).Id;
        }

        var result = await _svc.Categorization.ApplyRuleAsync(transactionId, ruleId);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Netflix Subscription");
        result.IsRuleApplied.Should().BeTrue();
        result.Category.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyRule_ById_ReturnsNullWhenMissing()
    {
        (await _svc.Categorization.ApplyRuleAsync(999_999, 1)).Should().BeNull();
    }

    [Fact]
    public async Task RecategorizePending_CategorizesMatchingPendingTransactions()
    {
        // The seed has an uncategorized "NETFLIX.COM" transaction that matches the NETFLIX rule.
        var applied = await _svc.Categorization.RecategorizePendingAsync();

        applied.Should().BeGreaterThan(0);

        using var ctx = _svc.Factory.CreateDbContext();
        var netflix = await ctx.Transactions
            .Include(t => t.Category)
            .FirstAsync(t => t.OriginalDescription == "NETFLIX.COM");
        netflix.IsRuleApplied.Should().BeTrue();
        netflix.Category.Should().NotBeNull();
    }
}
