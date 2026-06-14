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
        group.MapGet("/{id:int}/possible-rules", GetPossibleRules);
        group.MapPost("/{id:int}/apply-rule/{ruleId:int}", ApplyRule);
        group.MapPut("/{id:int}", Update);
        group.MapGet("/stats", GetStats);
        group.MapGet("/export", ExportCsv).Produces<string>(200, "text/csv");
    }

    internal static async Task<IResult> GetAll(
        DataService dataService,
        string period = "12",
        int? accountId = null,
        int? categoryId = null,
        string? search = null,
        bool uncategorized = false,
        string sortBy = "date",
        string sortDir = "desc",
        int page = 1,
        int pageSize = 50)
    {
        dataService.GetDates(period, out var startDate, out var endDate);

        var query = await dataService.GetTransactionsAsync();
        query = ApplyFilters(query, startDate, endDate, accountId, categoryId, search, uncategorized);

        var totalCount = await query.CountAsync();

        var items = await ApplySort(query, sortBy, sortDir)
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

    /// <summary>
    /// Applies the shared date/account/category/search/uncategorized filters used by
    /// both the listing and the stats endpoints so the two stay consistent.
    /// </summary>
    private static IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query,
        DateTime startDate,
        DateTime endDate,
        int? accountId,
        int? categoryId,
        string? search,
        bool uncategorized)
    {
        query = query.Where(t => t.Date >= startDate && t.Date < endDate);

        if (accountId.HasValue)
            query = query.Where(t => t.Account.Id == accountId.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.Category != null && t.Category.Id == categoryId.Value);

        if (uncategorized)
            query = query.Where(t => t.Category == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Description, pattern) ||
                EF.Functions.Like(t.OriginalDescription, pattern));
        }

        return query;
    }

    private static IQueryable<Transaction> ApplySort(
        IQueryable<Transaction> query,
        string sortBy,
        string sortDir)
    {
        var ascending = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLowerInvariant() switch
        {
            "amount" => ascending
                ? query.OrderBy(t => t.IsDebit ? -t.Amount : t.Amount)
                : query.OrderByDescending(t => t.IsDebit ? -t.Amount : t.Amount),
            "description" => ascending
                ? query.OrderBy(t => t.Description)
                : query.OrderByDescending(t => t.Description),
            _ => ascending
                ? query.OrderBy(t => t.Date).ThenBy(t => t.Id)
                : query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id),
        };
    }

    internal static async Task<IResult> GetById(int id, DataService dataService)
    {
        var query = await dataService.GetTransactionsAsync();
        var transaction = await query.FirstOrDefaultAsync(t => t.Id == id);
        return transaction is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(transaction.ToDto());
    }

    internal static async Task<IResult> GetPossibleRules(int id, DataService dataService)
    {
        var query = await dataService.GetTransactionsAsync();
        var transaction = await query.FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null)
            return TypedResults.NotFound();

        var rules = await dataService.GetPossibleRulesAsync(transaction);
        return TypedResults.Ok(rules.ToList());
    }

    internal static async Task<IResult> ApplyRule(int id, int ruleId, DataService dataService)
    {
        var transactions = await dataService.GetTransactionsAsync();
        var transaction = await transactions.FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null)
            return TypedResults.NotFound();

        var rules = await dataService.GetRulesAsync();
        var rule = await rules.FirstOrDefaultAsync(r => r.Id == ruleId);

        if (rule is null)
            return TypedResults.NotFound();

        await dataService.ApplyRuleAsync(transaction, rule);

        var refreshedTransactions = await dataService.GetTransactionsAsync();
        var updated = await refreshedTransactions.FirstOrDefaultAsync(t => t.Id == id);
        return updated is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(updated.ToDto());
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

    internal static async Task<IResult> GetStats(
        DataService dataService,
        string period = "12",
        int? accountId = null,
        int? categoryId = null,
        string? search = null,
        bool uncategorized = false)
    {
        dataService.GetDates(period, out var startDate, out var endDate);

        var query = await dataService.GetTransactionsAsync();
        var transactions = await ApplyFilters(query, startDate, endDate, accountId, categoryId, search, uncategorized)
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
