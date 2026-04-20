using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for managing financial transactions.
/// </summary>
public static class TransactionEndpoints
{
    /// <summary>
    /// Maps all transaction-related endpoints under <c>/api/transactions</c>.
    /// </summary>
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/transactions").WithTags("Transactions");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
        group.MapDelete("/bulk", DeleteAll);
        group.MapGet("/stats", GetStats);
        group.MapGet("/export", ExportCsv).Produces<string>(200, "text/csv");
    }

    internal static async Task<IResult> GetAll(
        DataService dataService,
        string period = "12",
        int? accountId = null,
        int? categoryId = null,
        int page = 1,
        int pageSize = 50)
    {
        dataService.GetDates(period, out var startDate, out var endDate);

        var query = await dataService.GetTransactionsAsync();
        query = query.Where(t => t.Date >= startDate && t.Date < endDate);

        if (accountId.HasValue)
            query = query.Where(t => t.Account.Id == accountId.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.Category != null && t.Category.Id == categoryId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return TypedResults.Ok(new
        {
            items = items.Select(t => t.ToDto()).ToList(),
            totalCount,
            page,
            pageSize
        });
    }

    internal static async Task<IResult> GetById(int id, DataService dataService)
    {
        var query = await dataService.GetTransactionsAsync();
        var transaction = await query.FirstOrDefaultAsync(t => t.Id == id);
        return transaction is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(transaction.ToDto());
    }

    internal static async Task<IResult> Update(
        int id,
        UpdateTransactionRequest request,
        DataService dataService)
    {
        var query = await dataService.GetTransactionsAsync();
        var transaction = await query.FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null)
            return TypedResults.NotFound();

        if (request.Description is not null)
            transaction.Description = request.Description;

        if (request.CategoryId.HasValue)
        {
            var category = await dataService.GetCategoryByIdAsync(request.CategoryId.Value);
            if (category is not null)
                transaction.Category = category;
        }

        await dataService.ChangeTransactionAsync(transaction);
        return TypedResults.Ok(transaction.ToDto());
    }

    internal static async Task<IResult> Delete(int id, DataService dataService)
    {
        var deleted = await dataService.DeleteTransactionAsync(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    internal static async Task<IResult> DeleteAll(DataService dataService)
    {
        await dataService.DeleteAllTransactionsAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<IResult> GetStats(DataService dataService, string period = "12")
    {
        dataService.GetDates(period, out var startDate, out var endDate);

        var query = await dataService.GetTransactionsAsync();
        var transactions = await query
            .Where(t => t.Date >= startDate && t.Date < endDate)
            .ToListAsync();

        var income = transactions.Where(t => !t.IsDebit).Sum(t => t.Amount);
        var expenses = transactions.Where(t => t.IsDebit).Sum(t => t.Amount);

        return TypedResults.Ok(new
        {
            income,
            expenses,
            net = income - expenses,
            count = transactions.Count
        });
    }

    internal static async Task<IResult> ExportCsv(DataService dataService, string period = "12")
    {
        var csv = await dataService.AIGetTransactionsCSVAsync(period);
        return TypedResults.Text(csv, "text/csv");
    }
}
