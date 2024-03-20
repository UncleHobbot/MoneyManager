namespace MoneyManager.Services;

public partial class DataService
{
    private static HashSet<Account> Accounts { get; set; } = [];
    public List<Account> GetAccounts() => Accounts.ToList();

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
}