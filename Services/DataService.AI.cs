using System.Globalization;
using CsvHelper;
using MoneyManager.Model.AI;

namespace MoneyManager.Services;

public partial class DataService
{
    public async Task<List<TransactionAI>> AIGetTransactions(string period)
    {
        GetDates(period, out var dateStart, out var dateEnd);
        return await AIGetTransactions(dateStart, dateEnd);
    }

    public async Task<List<TransactionAI>> AIGetTransactions(DateTime startDate, DateTime endDate)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var categoryTransfer = await ctx.Categories.FirstOrDefaultAsync(x => x.Name == "Transfer");

        var trans = await ctx.Transactions
            .Where(x => x.Date >= startDate && x.Date < endDate)
            .Where(x => x.Category != null)
            //.Where(x => x.Category != null && categoryTransfer != null && x.Category.Id != categoryTransfer.Id && x.Category.Parent.Id != categoryTransfer.Id)
            .Where(x => !x.Account.IsHideFromGraph)
            .Include(x => x.Account).Include(x => x.Category).Include(x => x.Category.Parent)
            .Select(x => new TransactionAI
            {
                Id = x.Id,
                Account = (x.Account.Type == 1 ? "Credit card " : "") + x.Account.ShownName,
                Date = x.Date.ToShortDateString(),
                Amount = x.Amount,
                SubCategory = x.Category.Name,
                Category = x.Category.Parent.Name,
                Description = x.Description
            })
            .ToListAsync();
        return trans;
    }

    public async Task<string> AIGetTransactionsCSV(string period)
    {
        GetDates(period, out var dateStart, out var dateEnd);
        return await AIGetTransactionsCSV(dateStart, dateEnd);
    }

    public async Task<string> AIGetTransactionsCSV(DateTime startDate, DateTime endDate)
    {
        var trans = await AIGetTransactions(startDate, endDate);
        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture, true);
        await csv.WriteRecordsAsync(trans);
        await csv.FlushAsync();
        var newRecord = writer.ToString();
        return newRecord;
    }
}