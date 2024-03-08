namespace MoneyManager.Services;

public class DataService(IDbContextFactory<DataContext> contextFactory)
{
    private static HashSet<Account> Accounts { get; set; } = [];
    private static HashSet<Category> Categories { get; set; } = [];

    public async Task InitStaticStorage()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (Accounts.Count == 0)
            Accounts = (await ctx.Accounts.ToListAsync()).ToHashSet();
        if (Categories.Count == 0)
            Categories = (await ctx.Categories.ToListAsync()).ToHashSet();
    }

    public List<Account> GetAccounts() => Accounts.ToList();
    public List<Category> GetCategories() => Categories.ToList();

    public HashSet<CategoryTree> GetCategoriesTree() => GetChildren(null);

    private HashSet<CategoryTree> GetChildren(Category parent)
    {
        var res = new HashSet<CategoryTree>();
        foreach (var c in Categories.Where(c => c.Parent == parent && !c.IsNew).OrderBy(x => x.Name))
            res.Add(new CategoryTree
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon,
                Children = GetChildren(c)
            });
        return res;
    }

    public async Task<List<Account>> ChangeAccount(Account account)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (account.Id == 0)
            ctx.Accounts.Add(account);
        else
            ctx.Accounts.Update(account);
        await ctx.SaveChangesAsync();

        Accounts = (await ctx.Accounts.ToListAsync()).ToHashSet();
        return Accounts.ToList();
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
            cat.Parent = null;
            cat.IsNew = false;
            await ChangeCategory(cat);
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
}