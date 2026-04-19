using FluentAssertions;
using MoneyManager.Api.Data;
using MoneyManager.Api.Tests.TestHelpers;

namespace MoneyManager.Api.Tests.Services;

public class DataServiceCategoryTests : IDisposable
{
    private readonly ServiceBundle _svc;

    public DataServiceCategoryTests()
    {
        _svc = DbContextHelper.CreateServiceBundle();
    }

    public void Dispose() => _svc.Dispose();

    [Fact]
    public async Task GetCategoriesAsync_ReturnsAllCategories()
    {
        var categories = await _svc.DataService.GetCategoriesAsync();

        categories.Should().HaveCountGreaterThanOrEqualTo(6);
        categories.Should().Contain(c => c.Name == "Food");
        categories.Should().Contain(c => c.Name == "Groceries");
        categories.Should().Contain(c => c.Name == "Income");
    }

    [Fact]
    public async Task GetCategoriesTreeAsync_ReturnsHierarchicalStructure()
    {
        var tree = await _svc.DataService.GetCategoriesTreeAsync();

        // Root-level categories (no parent) excluding IsNew ones
        tree.Should().NotBeEmpty();
        var foodNode = tree.FirstOrDefault(t => t.Name == "Food");
        foodNode.Should().NotBeNull();
        foodNode!.Children.Should().Contain(c => c.Name == "Groceries");
    }

    [Fact]
    public async Task GetCategoriesTreeAsync_ExcludesIsNewCategories()
    {
        var tree = await _svc.DataService.GetCategoriesTreeAsync();

        var allNames = FlattenTree(tree);
        allNames.Should().NotContain("AutoCategory");
    }

    [Fact]
    public async Task ChangeCategoryAsync_AddsNewCategory()
    {
        var newCat = new Category { Name = "Travel", Icon = "Travel" };

        var result = await _svc.DataService.ChangeCategoryAsync(newCat);

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Travel");

        var all = await _svc.DataService.GetCategoriesAsync();
        all.Should().Contain(c => c.Name == "Travel");
    }

    [Fact]
    public async Task ChangeCategoryAsync_UpdatesExistingCategory()
    {
        var categories = await _svc.DataService.GetCategoriesAsync();
        var food = categories.First(c => c.Name == "Food");
        food.Icon = "Shopping";

        await _svc.DataService.ChangeCategoryAsync(food);

        using var ctx = _svc.Factory.CreateDbContext();
        var updated = ctx.Categories.First(c => c.Name == "Food");
        updated.Icon.Should().Be("Shopping");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsCorrectCategory()
    {
        var categories = await _svc.DataService.GetCategoriesAsync();
        var food = categories.First(c => c.Name == "Food");

        var result = await _svc.DataService.GetCategoryByIdAsync(food.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Food");
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ReturnsNullForNonExistent()
    {
        var result = await _svc.DataService.GetCategoryByIdAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryByNameAsync_IsCaseInsensitive()
    {
        var result = await _svc.DataService.GetCategoryByNameAsync("food");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Food");
    }

    [Fact]
    public async Task GetCategoryByNameFromCacheAsync_FindsCachedCategory()
    {
        // Warm cache
        await _svc.DataService.GetCategoriesAsync();

        var result = await _svc.DataService.GetCategoryByNameFromCacheAsync("Income");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Income");
    }

    [Fact]
    public async Task GetCategoriesTreeAsync_SetsParentReferences()
    {
        var tree = await _svc.DataService.GetCategoriesTreeAsync();

        var foodNode = tree.FirstOrDefault(t => t.Name == "Food");
        foodNode.Should().NotBeNull();
        var groceryChild = foodNode!.Children.FirstOrDefault(c => c.Name == "Groceries");
        groceryChild.Should().NotBeNull();
        groceryChild!.Parent.Should().NotBeNull();
        groceryChild.Parent!.Name.Should().Be("Food");
    }

    private static List<string> FlattenTree(IEnumerable<CategoryTree> nodes)
    {
        var result = new List<string>();
        foreach (var node in nodes)
        {
            result.Add(node.Name);
            result.AddRange(FlattenTree(node.Children));
        }
        return result;
    }
}
