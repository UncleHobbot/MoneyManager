using DateTime = System.DateTime;

namespace MoneyManager.Services;

public partial class DataService
{
    private async Task<List<Transaction>> ChartGetTransactions(DateTime startDate, DateTime endDate)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var categoryTransfer = await ctx.Categories.FirstOrDefaultAsync(x => x.Name == "Transfer");

        var trans = await ctx.Transactions
            .Where(x => x.Date >= startDate && x.Date < endDate)
            .Where(x => x.Category.Id != categoryTransfer.Id && x.Category.Parent.Id != categoryTransfer.Id)
            .Where(x => !x.Account.IsHideFromGraph)
            .Include(x => x.Account).Include(x => x.Category).Include(x => x.Category.Parent)
            .ToListAsync();
        return trans;
    }

    public async Task<List<Transaction>> ChartGetTransactions(string month)
    {
        var startDate = DateTime.ParseExact(month, "yyMM", Thread.CurrentThread.CurrentCulture);
        var endDate = startDate.AddMonths(1).StartOfMonth(Thread.CurrentThread.CurrentCulture);

        return await ChartGetTransactions(startDate, endDate);
    }

    public async Task<List<BalanceChart>> ChartNetIncome(string chartPeriod)
    {
        var startDate = new DateTime(DateTime.Today.Year, 1, 1);
        var endDate = DateTime.Today.AddMonths(1).StartOfMonth(Thread.CurrentThread.CurrentCulture);

        if (chartPeriod == "12")
            startDate = DateTime.Today.AddMonths(-12).StartOfMonth(Thread.CurrentThread.CurrentCulture);
        else if (chartPeriod == "1")
            startDate = new DateTime(DateTime.Today.Year, 1, 1);
        else if (chartPeriod == "2")
        {
            startDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
            endDate = new DateTime(DateTime.Today.Year, 1, 1);
        }
        else if (chartPeriod == "3")
            startDate = new DateTime(DateTime.Today.Year - 1, 1, 1);

        var catIncome = await GetCategoryByName("Income");
        var trans = await ChartGetTransactions(startDate, endDate);

        var result = trans
            .GroupBy(x => x.Date.ToString("MMM yy"))
            .Select(x => new BalanceChart
            {
                Month = x.Key,
                FirstDate = x.Min(x => x.Date),
                MonthLabel = x.Min(x => x.Date).ToString("MMMM yyyy"),
                MonthKey = x.Min(x => x.Date).ToString("yyMM"),
                Income = x.Where(x => (x.Category.Parent ?? x.Category).Id == catIncome.Id).Sum(x => (x.IsDebit ? -1 : 1) * x.Amount),
                Expenses = x.Where(x => (x.Category.Parent ?? x.Category).Id != catIncome.Id).Sum(x => (x.IsDebit ? -1 : 1) * x.Amount)
            }).OrderBy(x => x.FirstDate).ToList();

        return result;
    }
}