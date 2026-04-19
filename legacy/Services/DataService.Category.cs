namespace MoneyManager.Services;

/// <summary>
/// Provides methods for managing expense and income categories in the MoneyManager system.
/// </summary>
/// <remarks>
/// This partial class implements CRUD operations for <see cref="Data.Category"/> entities.
/// Supports hierarchical category structures with parent-child relationships.
/// Uses static caching via <see cref="Categories"/> property for performance.
/// Excludes categories marked as new (auto-generated during import) from tree views.
/// </remarks>
public partial class DataService
{
    /// <summary>
    /// Gets or sets the in-memory cache of all categories.
    /// </summary>
    /// <value>
    /// A <see cref="HashSet{T}"/> containing all categories from database.
    /// Initialized empty and populated by <see cref="InitStaticStorage"/>.
    /// </value>
    /// <remarks>
    /// Static cache provides O(1) lookups for category operations.
    /// Updated whenever categories are added, modified, or deleted.
    /// </remarks>
    private static HashSet<Category> Categories { get; set; } = [];

    /// <summary>
    /// Gets all categories from the in-memory cache.
    /// </summary>
    /// <returns>
    /// A list of all <see cref="Data.Category"/> objects in the system.
    /// </returns>
    /// <remarks>
    /// Returns data from static cache, not from database.
    /// Returns a fresh list to prevent modification of cached collection.
    /// Use this for displaying category lists in UI.
    /// </remarks>
    public List<Category> GetCategories() => Categories.ToList();

    /// <summary>
    /// Builds and returns a hierarchical tree structure of all categories.
    /// </summary>
    /// <returns>
    /// A <see cref="HashSet{T}"/> of <see cref="CategoryTree"/> objects representing the category hierarchy.
    /// </returns>
    /// <remarks>
    /// Recursively builds tree structure from flat category list.
    /// Root categories (those without parents) are at the top level.
    /// Child categories are nested under their respective parents.
    /// Excludes categories marked as <see cref="Data.Category.IsNew"/> (auto-generated).
    /// Parent references are set for navigation within the tree.
    /// </remarks>
    public HashSet<CategoryTree> GetCategoriesTree()
    {
        var result = GetChildren(null);
        foreach (var parent in result)
            foreach (var child in parent.Children)
                child.Parent = parent;

        return result;
    }

    /// <summary>
    /// Recursively retrieves child categories for a given parent category.
    /// </summary>
    /// <param name="parent">
    /// The parent <see cref="Data.Category"/> to get children for, or null for root categories.
    /// </param>
    /// <returns>
    /// A <see cref="HashSet{T}"/> of <see cref="CategoryTree"/> objects representing child categories.
    /// </returns>
    /// <remarks>
    /// Private recursive method used to build hierarchical tree structure.
    /// Filters out categories marked as <see cref="Data.Category.IsNew"/>.
    /// Returns children sorted alphabetically by name.
    /// Each child recursively builds its own children via this method.
    /// </remarks>
    private HashSet<CategoryTree> GetChildren(Category? parent)
    {
        var res = new HashSet<CategoryTree>();
        foreach (var c in Categories.Where(c => c.Parent == parent && !c.IsNew).OrderBy(x => x.Name))
            res.Add(new CategoryTree
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon,
                Children = GetChildren(c),
            });
        return res;
    }

    /// <summary>
    /// Adds a new category to the database or updates an existing category.
    /// </summary>
    /// <param name="category">
    /// The <see cref="Data.Category"/> to add or update.
    /// If Id is 0, a new category is added; otherwise, an existing category is updated.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is the added/updated category.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Updates static cache after successful database operation.
    /// Returns the category object for further use in UI.
    /// </remarks>
    public async Task<Category> ChangeCategory(Category category)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (category.Id == 0)
            ctx.Categories.Add(category);
        else
            ctx.Categories.Update(category);
        await ctx.SaveChangesAsync();

        Categories = (await ctx.Categories.ToListAsync()).ToHashSet();
        return category;
    }

    /// <summary>
    /// Converts a dropdown category item to a parent-level category in the database.
    /// </summary>
    /// <param name="category">
    /// The <see cref="Data.CategoryDropItem"/> containing category ID and parent information.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// Used when user selects "Parent" from a dropdown to make a category top-level.
    /// Clears the <see cref="Data.Category.IsNew"/> flag.
    /// Only processes if ParentCategory is "parent" (dropdown value for making a category a parent).
    /// </remarks>
    public async Task MakeParent(CategoryDropItem category)
    {
        if (category.ParentCategory == "parent")
        {
            var ctx = await contextFactory.CreateDbContextAsync();
            var cat = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
            if (cat != null)
            {
                cat.Parent = null;
                cat.IsNew = false;
                await ChangeCategory(cat);
            }
        }
    }

    /// <summary>
    /// Saves a category's parent relationship from a dropdown selection.
    /// </summary>
    /// <param name="category">
    /// The <see cref="Data.CategoryDropItem"/> containing category ID and selected parent ID.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// Used when user selects a specific parent category from dropdown.
    /// Clears the <see cref="Data.Category.IsNew"/> flag.
    /// ParentCategoryId is parsed from string to integer.
    /// </remarks>
    public async Task SaveCategory(CategoryDropItem category)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var cat = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
        var catParent = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == int.Parse(category.ParentCategory));
        if (cat != null && catParent != null)
        {
            cat.Parent = catParent;
            cat.IsNew = false;
            await ChangeCategory(cat);
        }
    }

    /// <summary>
    /// Saves category name and icon changes from a tree node.
    /// </summary>
    /// <param name="category">
    /// The <see cref="Data.CategoryTree"/> containing updated name and icon.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// Used when user edits a category in the tree view.
    /// Updates only the Name and Icon properties.
    /// Clears the <see cref="Data.Category.IsNew"/> flag automatically.
    /// </remarks>
    public async Task SaveCategory(CategoryTree category)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var cat = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == category.Id);
        if (cat != null)
        {
            cat.Name = category.Name;
            cat.Icon = category.Icon;
            await ChangeCategory(cat);
        }
    }

    /// <summary>
    /// Retrieves a category by its unique identifier from the in-memory cache.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the category to retrieve.
    /// </param>
    /// <returns>
    /// The <see cref="Data.Category"/> with the specified ID, or null if not found.
    /// </returns>
    /// <remarks>
    /// Searches the static cache for performance.
    /// Returns null if category doesn't exist in cache.
    /// Case-sensitive match on ID.
    /// </remarks>
    public Category? GetCategoryById(int id) => Categories.FirstOrDefault(x => x.Id == id);

    /// <summary>
    /// Retrieves a category by its name from the database.
    /// </summary>
    /// <param name="name">
    /// The name of the category to retrieve.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is the category with the specified name, or null if not found.
    /// </returns>
    /// <remarks>
    /// Performs a database query for the category.
    /// Case-insensitive comparison using ToUpper().
    /// Useful for category lookup during import operations.
    /// </remarks>
    public async Task<Category?> GetCategoryByName(string name)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return await ctx.Categories.FirstOrDefaultAsync(x => x.Name.ToUpper() == name.ToUpper());
    }

    /// <summary>
    /// Retrieves a category by its name from the in-memory cache.
    /// </summary>
    /// <param name="name">
    /// The name of the category to retrieve.
    /// </param>
    /// <returns>
    /// The <see cref="Data.Category"/> with the specified name, or null if not found.
    /// </returns>
    /// <remarks>
    /// Searches the static cache for performance.
    /// Case-insensitive comparison using current culture.
    /// Returns null if category doesn't exist in cache.
    /// Preferred over <see cref="GetCategoryByName"/> when cache is populated.
    /// </remarks>
    public Category? GetCategoryByNameFromCache(string name) => Categories.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
}
