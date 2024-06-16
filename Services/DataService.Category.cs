namespace MoneyManager.Services;

public partial class DataService
{
    private static HashSet<Category> Categories { get; set; } = [];

    public List<Category> GetCategories() => Categories.ToList();

    public HashSet<CategoryTree> GetCategoriesTree()
    {
        var result = GetChildren(null);
        foreach (var parent in result)
        foreach (var child in parent.Children)
            child.Parent = parent;

        return result;
    }

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

    public Category? GetCategoryById(int id) => Categories.FirstOrDefault(x => x.Id == id);

    public async Task<Category?> GetCategoryByName(string name)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return await ctx.Categories.FirstOrDefaultAsync(x => x.Name.ToUpper() == name.ToUpper());
    }

    public Category? GetCategoryByNameFromCache(string name) => Categories.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
}