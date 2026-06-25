using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

/// <summary>
/// Tests for <see cref="ReferenceDataCache"/> — the deep reference-data module.
/// The interface (Get / Invalidate / Warm) is the test surface.
/// </summary>
public class ReferenceDataCacheTests : IDisposable
{
    private readonly TestDbContextFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly ReferenceDataCache _sut;

    public ReferenceDataCacheTests()
    {
        _factory = DbContextHelper.CreateFactory();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new ReferenceDataCache(_factory, _cache);

        using var ctx = _factory.CreateDbContext();
        var groceries = new Category { Name = "Groceries", Icon = "Food" };
        ctx.Categories.Add(groceries);
        ctx.Categories.Add(new Category { Name = "Supermarket", Icon = "Food", Parent = groceries });
        ctx.Accounts.Add(new Account { Name = "Chequing", ShownName = "Chequing", Type = 0, Number = "1" });
        ctx.SaveChanges();
    }

    public void Dispose()
    {
        _factory.Dispose();
        (_cache as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task GetAccounts_OnColdCache_ReadsThroughToDatabase()
    {
        var accounts = await _sut.GetAccountsAsync();

        accounts.Should().ContainSingle(a => a.Name == "Chequing");
    }

    [Fact]
    public async Task GetCategories_PopulatesParent()
    {
        var categories = await _sut.GetCategoriesAsync();

        var child = categories.Single(c => c.Name == "Supermarket");
        child.Parent.Should().NotBeNull();
        child.Parent!.Name.Should().Be("Groceries");
    }

    [Fact]
    public async Task GetAccounts_AfterInvalidate_ReflectsNewRow()
    {
        // Prime the cache.
        (await _sut.GetAccountsAsync()).Should().HaveCount(1);

        // Write directly to the DB behind the cache's back.
        using (var ctx = _factory.CreateDbContext())
        {
            ctx.Accounts.Add(new Account { Name = "Savings", ShownName = "Savings", Type = 0, Number = "2" });
            await ctx.SaveChangesAsync();
        }

        // Stale until invalidated.
        (await _sut.GetAccountsAsync()).Should().HaveCount(1);

        _sut.InvalidateAccounts();

        (await _sut.GetAccountsAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Warm_PreloadsBothSets()
    {
        await _sut.WarmAsync();

        (await _sut.GetAccountsAsync()).Should().NotBeEmpty();
        (await _sut.GetCategoriesAsync()).Should().NotBeEmpty();
    }
}
