namespace MoneyManager.Services;

public partial class DataService(IDbContextFactory<DataContext> contextFactory)
{
    public async Task InitStaticStorage()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (Accounts.Count == 0)
            Accounts = (await ctx.Accounts.ToListAsync()).ToHashSet();
        if (Categories.Count == 0)
            Categories = (await ctx.Categories.ToListAsync()).ToHashSet();
    }
    
    public static string NetIncomeChartPeriod { get; set; } = "12";
}