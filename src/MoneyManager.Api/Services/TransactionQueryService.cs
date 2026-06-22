using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Query;

namespace MoneyManager.Api.Services;

/// <summary>
/// Read-side query module for listable transactions. Owns filtering, sorting,
/// paging, and aggregation behind two methods. The listability invariant
/// (transactions on hidden accounts are always excluded) is enforced here;
/// callers cannot opt out. See <c>CONTEXT.md</c> ("Listable Transaction") and
/// ADR-0002 / ADR-0003 for the design decisions.
/// </summary>
public class TransactionQueryService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;

    public TransactionQueryService(IDbContextFactory<DataContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Returns a page of transactions matching <paramref name="filters"/>,
    /// sorted by <paramref name="sort"/> and paged by <paramref name="paging"/>.
    /// </summary>
    public async Task<Page<TransactionDto>> GetPageAsync(
        TransactionFilters filters,
        TransactionSort sort,
        Paging paging,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = ApplyFilters(ListableTransactions(ctx), filters);
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ApplySort(query, sort)
            .Skip((paging.Page - 1) * paging.Size)
            .Take(paging.Size)
            .Select(t => t.ToDto())
            .ToListAsync(cancellationToken);

        return new Page<TransactionDto>(items, totalCount, paging.Page, paging.Size);
    }

    /// <summary>
    /// Returns aggregate statistics over all transactions matching
    /// <paramref name="filters"/>. Sort and paging do not apply.
    /// </summary>
    public async Task<TransactionStats> GetStatsAsync(
        TransactionFilters filters,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var transactions = await ApplyFilters(ListableTransactions(ctx), filters)
            .ToListAsync(cancellationToken);

        var income = transactions.Where(t => !t.IsDebit).Sum(t => t.Amount);
        var expenses = transactions.Where(t => t.IsDebit).Sum(t => t.Amount);

        return new TransactionStats(income, expenses, transactions.Count);
    }

    /// <summary>
    /// Returns reporting projections of all transactions matching
    /// <paramref name="filters"/>. Each row carries the signed amount,
    /// the rolled-up effective category, and pre-computed
    /// <see cref="ReportingRow.IsIncome"/> / <see cref="ReportingRow.IsTransfer"/>
    /// flags so consumers can aggregate without re-spelling the sign
    /// convention or category-name matches.
    /// </summary>
    /// <remarks>
    /// Transactions with no category produce rows with
    /// <c>EffectiveCategory: null</c> and both flags <c>false</c>. Consumers
    /// that need a category for grouping should filter; consumers that only
    /// need Income/Expense totals can ignore the field.
    /// </remarks>
    public async Task<IReadOnlyList<ReportingRow>> GetReportingRowsAsync(
        TransactionFilters filters,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var transactions = await ApplyFilters(ListableTransactions(ctx), filters)
            .ToListAsync(cancellationToken);

        return transactions
            .Select(t =>
            {
                var effective = t.Category?.Parent ?? t.Category;
                return new ReportingRow(
                    Date: t.Date,
                    SignedAmount: t.AmountExt,
                    EffectiveCategory: effective is null
                        ? null
                        : new ReportingCategory(effective.Id, effective.Name, effective.Icon),
                    IsIncome: effective?.Name == "Income",
                    IsTransfer: effective?.Name == "Transfer");
            })
            .ToList();
    }

    /// <summary>
    /// Base query for listable transactions: eager-loads the navigations the
    /// DTO needs and enforces the listability invariant. AsNoTracking because
    /// the module is read-only — entities never escape this service.
    /// </summary>
    private static IQueryable<Transaction> ListableTransactions(DataContext ctx) =>
        ctx.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Category!.Parent)
            .Where(t => !t.Account.IsHideFromGraph);

    /// <summary>
    /// Applies the shared filter set used by both <see cref="GetPageAsync"/>
    /// and <see cref="GetStatsAsync"/> so the two stay consistent.
    /// </summary>
    private static IQueryable<Transaction> ApplyFilters(IQueryable<Transaction> query, TransactionFilters filters)
    {
        query = query.Where(t => t.Date >= filters.StartDate && t.Date < filters.EndDate);

        if (filters.AccountId.HasValue)
            query = query.Where(t => t.Account.Id == filters.AccountId.Value);

        if (filters.CategoryId.HasValue)
            query = query.Where(t => t.Category != null && t.Category.Id == filters.CategoryId.Value);

        // "Uncategorized" matches both a missing category and the dedicated
        // "Uncategorized" category. See CONTEXT.md ("Uncategorized").
        if (filters.Uncategorized)
            query = query.Where(t => t.Category == null || t.Category.Name.ToLower() == "uncategorized");

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var pattern = $"%{filters.Search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Description, pattern) ||
                EF.Functions.Like(t.OriginalDescription, pattern));
        }

        return query;
    }

    /// <summary>
    /// Applies the sort specified by <paramref name="sort"/>. Unknown fields
    /// fall back to the date sort.
    /// </summary>
    /// <remarks>
    /// Amount sort uses the inline signed-amount formula
    /// (<c>IsDebit ? -Amount : Amount</c>) rather than <see cref="Transaction.AmountExt"/>
    /// because EF Core 10 cannot translate the expression-bodied property in
    /// an <c>OrderBy</c> call (it sees <c>AmountExt</c> as unmapped). The
    /// formula is duplicated here intentionally; Candidate 3 (ReportingRow
    /// with a persisted signed-amount column) is the planned consolidation.
    /// See <c>CONTEXT.md</c> ("Signed amount") and the Q9 grilling decision.
    /// </remarks>
    private static IQueryable<Transaction> ApplySort(IQueryable<Transaction> query, TransactionSort sort)
    {
        var ascending = sort.Direction == SortDirection.Ascending;

        return sort.Field?.ToLowerInvariant() switch
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
}

