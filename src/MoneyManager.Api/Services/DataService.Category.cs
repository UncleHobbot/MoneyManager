using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides methods for managing expense and income categories in the MoneyManager system.
/// </summary>
/// <remarks>
/// This partial class implements CRUD operations for <see cref="Category"/> entities.
/// Supports hierarchical category structures with parent-child relationships.
/// Uses <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> for thread-safe caching.
/// Excludes categories marked as new (auto-generated during import) from tree views.
/// </remarks>
public partial class DataService
{
    /// <summary>
    /// Gets all categories from the cache.
    /// </summary>
    /// <returns>A list of all <see cref="Category"/> objects in the system.</returns>
    /// <remarks>
    /// Returns data from the memory cache, loading from database if cache is empty.
    /// Returns a fresh list to prevent modification of cached collection.
    /// </remarks>
    public async Task<List<Category>> GetCategoriesAsync()
    {
        var categories = await GetCachedCategoriesAsync();
        return categories.ToList();
    }

    /// <summary>
    /// Builds and returns a hierarchical tree structure of all categories.
    /// </summary>
    /// <returns>A <see cref="HashSet{T}"/> of <see cref="CategoryTree"/> objects representing the category hierarchy.</returns>
    /// <remarks>
    /// Recursively builds tree structure from flat category list.
    /// Root categories (those without parents) are at the top level.
    /// Child categories are nested under their respective parents.
    /// Excludes categories marked as <see cref="Category.IsNew"/> (auto-generated).
    /// Parent references are set for navigation within the tree.
    /// </remarks>
    public async Task<HashSet<CategoryTree>> GetCategoriesTreeAsync()
    {
        var categories = await GetCachedCategoriesAsync();
        var result = GetChildren(null, categories);
        foreach (var parent in result)
            foreach (var child in parent.Children)
                child.Parent = parent;

        return result;
    }

    /// <summary>
    /// Recursively retrieves child categories for a given parent category.
    /// </summary>
    /// <param name="parent">The parent <see cref="Category"/> to get children for, or null for root categories.</param>
    /// <param name="categories">The full set of categories to search within.</param>
    /// <returns>A <see cref="HashSet{T}"/> of <see cref="CategoryTree"/> objects representing child categories.</returns>
    /// <remarks>
    /// Private recursive method used to build hierarchical tree structure.
    /// Filters out categories marked as <see cref="Category.IsNew"/>.
    /// Returns children sorted alphabetically by name.
    /// </remarks>
    private static HashSet<CategoryTree> GetChildren(Category? parent, HashSet<Category> categories)
    {
        var res = new HashSet<CategoryTree>();
        foreach (var c in categories.Where(c => c.Parent == parent && !c.IsNew).OrderBy(x => x.Name))
            res.Add(new CategoryTree
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon,
                Children = GetChildren(c, categories),
            });
        return res;
    }

    /// <summary>
    /// Adds a new category to the database or updates an existing category.
    /// </summary>
    /// <param name="category">
    /// The <see cref="Category"/> to add or update.
    /// If Id is 0, a new category is added; otherwise, an existing category is updated.
    /// </param>
    /// <returns>The added or updated category.</returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Refreshes the memory cache after successful database operation.
    /// </remarks>
    public async Task<Category> ChangeCategoryAsync(Category category)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (category.Id == 0)
            ctx.Categories.Add(category);
        else
            ctx.Categories.Update(category);
        await ctx.SaveChangesAsync();

        await RefreshCategoriesCacheAsync();
        return category;
    }

    /// <summary>
    /// Converts a category to a top-level parent category.
    /// </summary>
    /// <param name="category">
    /// The <see cref="CategoryDropItem"/> containing category ID and parent information.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Used when user selects "Parent" from a dropdown to make a category top-level.
    /// Clears the <see cref="Category.IsNew"/> flag.
    /// Only processes if ParentCategory is "parent" (dropdown value for making a category a parent).
    /// </remarks>
    public async Task MakeParentAsync(CategoryDropItem category)
    {
        if (category.ParentCategory == "parent")
        {
            var ctx = await contextFactory.CreateDbContextAsync();
            var cat = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
            if (cat != null)
            {
                cat.Parent = null;
                cat.IsNew = false;
                await ChangeCategoryAsync(cat);
            }
        }
    }

    /// <summary>
    /// Saves a category's parent relationship from a dropdown selection.
    /// </summary>
    /// <param name="category">
    /// The <see cref="CategoryDropItem"/> containing category ID and selected parent ID.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Used when user selects a specific parent category from dropdown.
    /// Clears the <see cref="Category.IsNew"/> flag.
    /// </remarks>
    public async Task SaveCategoryAsync(CategoryDropItem category)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var cat = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
        var catParent = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == int.Parse(category.ParentCategory));
        if (cat != null && catParent != null)
        {
            cat.Parent = catParent;
            cat.IsNew = false;
            await ChangeCategoryAsync(cat);
        }
    }

    /// <summary>
    /// Saves category name and icon changes from a tree node.
    /// </summary>
    /// <param name="category">
    /// The <see cref="CategoryTree"/> containing updated name and icon.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Used when user edits a category in the tree view.
    /// Updates only the Name and Icon properties.
    /// Clears the <see cref="Category.IsNew"/> flag automatically.
    /// </remarks>
    public async Task SaveCategoryAsync(CategoryTree category)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var cat = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
        if (cat != null)
        {
            cat.Name = category.Name;
            cat.Icon = category.Icon;
            await ChangeCategoryAsync(cat);
        }
    }

    /// <summary>
    /// Retrieves a category by its unique identifier from the cache.
    /// </summary>
    /// <param name="id">The unique identifier of the category to retrieve.</param>
    /// <returns>The <see cref="Category"/> with the specified ID, or null if not found.</returns>
    /// <remarks>
    /// Searches the memory cache for performance.
    /// Returns null if category doesn't exist in cache.
    /// </remarks>
    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        var categories = await GetCachedCategoriesAsync();
        return categories.FirstOrDefault(x => x.Id == id);
    }

    /// <summary>
    /// Retrieves a category by its name from the database.
    /// </summary>
    /// <param name="name">The name of the category to retrieve.</param>
    /// <returns>The category with the specified name, or null if not found.</returns>
    /// <remarks>
    /// Performs a database query for the category.
    /// Case-insensitive comparison using ToUpper().
    /// Useful for category lookup during import operations.
    /// </remarks>
    public async Task<Category?> GetCategoryByNameAsync(string name)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return await ctx.Categories.FirstOrDefaultAsync(x => x.Name.ToUpper() == name.ToUpper());
    }

    /// <summary>
    /// Retrieves a category by its name from the cache.
    /// </summary>
    /// <param name="name">The name of the category to retrieve.</param>
    /// <returns>The <see cref="Category"/> with the specified name, or null if not found.</returns>
    /// <remarks>
    /// Searches the memory cache for performance.
    /// Case-insensitive comparison using current culture.
    /// Preferred over <see cref="GetCategoryByNameAsync"/> when cache is populated.
    /// </remarks>
    public async Task<Category?> GetCategoryByNameFromCacheAsync(string name)
    {
        var categories = await GetCachedCategoriesAsync();
        return categories.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
    }
}
