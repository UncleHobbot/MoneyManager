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

    internal static async Task<IResult> GetNetIncome(string period, ChartService chartService)
    {
        var result = await chartService.ChartNetIncomeAsync(period ?? "12");
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetCumulativeSpending(ChartService chartService)
    {
        var result = await chartService.ChartCumulativeSpendingAsync();
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetSpendingTrend(string period, ChartService chartService)
    {
        var result = await chartService.ChartSpendingTrendAsync(period ?? "12");
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetTopMerchants(string period, ChartService chartService, int limit = 15)
    {
        var result = await chartService.ChartTopMerchantsAsync(period ?? "12", limit);
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetCashFlow(string period, ChartService chartService)
    {
        var result = await chartService.ChartCashFlowAsync(period ?? "12");
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetBudgetVsActual(ChartService chartService)
    {
        var result = await chartService.ChartBudgetVsActualAsync();
        return TypedResults.Ok(result);
    }

    internal static async Task<IResult> GetSpendingByCategory(string period, ChartService chartService)
    {
        var result = await chartService.ChartSpendingByCategoryAsync(period ?? "12");
        return TypedResults.Ok(result);
    }

    internal static IResult GetPeriods()
    {
        // Single source of truth: ChartPeriod.All. The endpoint projects
        // away the GetDateRange function (not serializable, not relevant
        // to clients) and returns only { Code, Label }.
        return TypedResults.Ok(ChartPeriod.All.Select(p => new { p.Code, p.Label }));
    }
}
