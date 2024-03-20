namespace MoneyManager.Services;

public partial class DataService
{
    public async Task<IQueryable<Transaction>> GetTransactions()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return ctx.Transactions.Include(x => x.Account).Include(x => x.Category).Include(x => x.Category.Parent)
            .Where(x => !x.Account.IsHideFromGraph).Select(x => x);
    }
}