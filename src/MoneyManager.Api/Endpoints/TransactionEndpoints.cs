using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Model.Query;
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
        group.MapPost("/", Create);
        group.MapGet("/{id:int}", GetById);
        group.MapGet("/{id:int}/possible-rules", GetPossibleRules);
        group.MapPost("/{id:int}/apply-rule/{ruleId:int}", ApplyRule);
        group.MapPut("/{id:int}", Update);
        group.MapGet("/stats", GetStats);
        group.MapGet("/export", ExportCsv).Produces<string>(200, "text/csv");
    }

    internal static async Task<IResult> GetAll(
        DataService dataService,
        TransactionQueryService queryService,
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

        var filters = new TransactionFilters(
            StartDate: startDate,
            EndDate: endDate,
            AccountId: accountId,
            CategoryId: categoryId,
            Search: search,
            Uncategorized: uncategorized);

        var direction = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase)
            ? SortDirection.Ascending
            : SortDirection.Descending;
        var sort = new TransactionSort(sortBy ?? "date", direction);
        var paging = new Paging(page, pageSize);

        var result = await queryService.GetPageAsync(filters, sort, paging);

        return TypedResults.Ok(new
        {
            items = result.Items,
            totalCount = result.TotalCount,
            page = result.PageNumber,
            pageSize = result.PageSize
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

        // "Uncategorized" covers both a missing category and the dedicated
        // "Uncategorized" category (which is how the dashboard identifies them).
        if (uncategorized)
            query = query.Where(t => t.Category == null || t.Category.Name.ToLower() == "uncategorized");

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

    internal static async Task<IResult> Create(
        CreateTransactionRequest request,
        DataService dataService)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return TypedResults.BadRequest("Description is required.");

        if (request.Amount <= 0)
            return TypedResults.BadRequest("Amount must be greater than zero.");

        var created = await dataService.AddTransactionAsync(
            request.AccountId,
            request.Date,
            request.Description.Trim(),
            request.Amount,
            request.IsDebit,
            request.CategoryId);

        return created is null
            ? TypedResults.BadRequest("Account not found.")
            : TypedResults.Created($"/api/transactions/{created.Id}", created.ToDto());
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
        TransactionQueryService queryService,
        string period = "12",
        int? accountId = null,
        int? categoryId = null,
        string? search = null,
        bool uncategorized = false)
    {
        dataService.GetDates(period, out var startDate, out var endDate);

        var filters = new TransactionFilters(
            StartDate: startDate,
            EndDate: endDate,
            AccountId: accountId,
            CategoryId: categoryId,
            Search: search,
            Uncategorized: uncategorized);

        var stats = await queryService.GetStatsAsync(filters);
        return TypedResults.Ok(stats);
    }

    internal static async Task<IResult> ExportCsv(DataService dataService, string period = "12")
    {
        var csv = await dataService.AIGetTransactionsCSVAsync(period);
        return TypedResults.Text(csv, "text/csv");
    }
}
