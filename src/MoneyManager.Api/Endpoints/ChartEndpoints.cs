using MoneyManager.Api.Model.Query;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for chart data used to visualize financial trends and spending patterns.
/// </summary>
public static class ChartEndpoints
{
    /// <summary>
    /// Maps all chart-related endpoints under <c>/api/charts</c>.
    /// </summary>
    public static void MapChartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/charts").WithTags("Charts");

        group.MapGet("/net-income", GetNetIncome);
        group.MapGet("/cumulative-spending", GetCumulativeSpending);
        group.MapGet("/spending-by-category", GetSpendingByCategory);
        group.MapGet("/spending-trend", GetSpendingTrend);
        group.MapGet("/top-merchants", GetTopMerchants);
        group.MapGet("/cash-flow", GetCashFlow);
        group.MapGet("/budget-vs-actual", GetBudgetVsActual);
        group.MapGet("/periods", GetPeriods);
    }

    internal static async Task<IResult> GetNetIncome(string period, DataService dataService)
    {
        var result = await dataService.ChartNetIncomeAsync(period ?? "12");
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetCumulativeSpending(DataService dataService)
    {
        var result = await dataService.ChartCumulativeSpendingAsync();
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetSpendingTrend(string period, DataService dataService)
    {
        var result = await dataService.ChartSpendingTrendAsync(period ?? "12");
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetTopMerchants(string period, DataService dataService, int limit = 15)
    {
        var result = await dataService.ChartTopMerchantsAsync(period ?? "12", limit);
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetCashFlow(string period, DataService dataService)
    {
        var result = await dataService.ChartCashFlowAsync(period ?? "12");
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetBudgetVsActual(DataService dataService)
    {
        var result = await dataService.ChartBudgetVsActualAsync();
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetSpendingByCategory(
        string period,
        TransactionQueryService queryService)
    {
        var (startDate, endDate) = (ChartPeriod.Find(period ?? "12") ?? ChartPeriod.Default).GetDateRange(DateTime.Today);

        // The immediately-preceding window of the same length, for a per-category
        // delta. Skipped for unbounded "a" (StartDate == MinValue), where there is
        // no earlier window to compare against.
        var hasPrevious = startDate > DateTime.MinValue.AddYears(1);
        var prevStart = hasPrevious ? startDate - (endDate - startDate) : startDate;

        var rows = await queryService.GetReportingRowsAsync(
            new TransactionFilters(StartDate: prevStart, EndDate: endDate));

        // Previous-window spend per category id (absolute).
        var previousByCategory = rows
            .Where(r => r.Date < startDate && !r.IsTransfer && r.EffectiveCategory != null)
            .GroupBy(r => r.EffectiveCategory!.Id)
            .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(r => r.SignedAmount)));

        // Current-window groups (one per rolled-up category), excluding transfers
        // and uncategorized.
        var grouped = rows
            .Where(r => r.Date >= startDate && !r.IsTransfer && r.EffectiveCategory != null)
            .GroupBy(r => r.EffectiveCategory!)
            .Select(g => new
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Icon = g.Key.Icon,
                RawSignedAmount = g.Sum(r => r.SignedAmount),
                IsIncome = g.First().IsIncome,
            })
            .ToList();

        var incomeItems = grouped.Where(x => x.IsIncome).ToList();
        var expenseItems = grouped.Where(x => !x.IsIncome).ToList();

        var totalIncome = incomeItems.Sum(x => Math.Abs(x.RawSignedAmount));
        var totalExpenses = expenseItems.Sum(x => Math.Abs(x.RawSignedAmount));

        var income = incomeItems.Select(x => new
        {
            x.Name,
            x.Icon,
            Amount = Math.Abs(x.RawSignedAmount),
            PreviousAmount = previousByCategory.GetValueOrDefault(x.Id, 0m),
            Percentage = totalIncome > 0 ? Math.Round((double)(Math.Abs(x.RawSignedAmount) / totalIncome * 100), 2) : 0
        }).OrderByDescending(x => x.Amount).ToList();

        var expenses = expenseItems.Select(x => new
        {
            x.Name,
            x.Icon,
            Amount = Math.Abs(x.RawSignedAmount),
            PreviousAmount = previousByCategory.GetValueOrDefault(x.Id, 0m),
            Percentage = totalExpenses > 0 ? Math.Round((double)(Math.Abs(x.RawSignedAmount) / totalExpenses * 100), 2) : 0
        }).OrderByDescending(x => x.Amount).ToList();

        return TypedResults.Ok(new { income, expenses });
    }

    internal static IResult GetPeriods()
    {
        // Single source of truth: ChartPeriod.All. The endpoint projects
        // away the GetDateRange function (not serializable, not relevant
        // to clients) and returns only { Code, Label }.
        return TypedResults.Ok(ChartPeriod.All.Select(p => new { p.Code, p.Label }));
    }
}
