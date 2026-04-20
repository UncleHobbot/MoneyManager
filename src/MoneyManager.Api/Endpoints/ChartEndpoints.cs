using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for chart data used to visualize financial trends and spending patterns.
/// </summary>
public static class ChartEndpoints
{
    private static readonly object[] Periods =
    [
        new { Code = "12", Label = "Last 12 Months" },
        new { Code = "y1", Label = "This Year" },
        new { Code = "y2", Label = "Last Year" },
        new { Code = "y3", Label = "2 Years Ago" },
        new { Code = "y12", Label = "Last + This Year" },
        new { Code = "m1", Label = "This Month" },
        new { Code = "m2", Label = "Last Month" },
        new { Code = "m1+2", Label = "Last 2 Months" },
        new { Code = "m1+3", Label = "Last 3 Months" },
        new { Code = "w", Label = "Last 7 Days" },
        new { Code = "w2", Label = "Last 14 Days" },
        new { Code = "w3", Label = "Last 31 Days" },
        new { Code = "a", Label = "All Time" }
    ];

    /// <summary>
    /// Maps all chart-related endpoints under <c>/api/charts</c>.
    /// </summary>
    public static void MapChartEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/charts").WithTags("Charts");

        group.MapGet("/net-income", GetNetIncome);
        group.MapGet("/cumulative-spending", GetCumulativeSpending);
        group.MapGet("/spending-by-category", GetSpendingByCategory);
        group.MapGet("/month/{month}", GetMonthTransactions);
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

    internal static async Task<IResult> GetSpendingByCategory(string period, DataService dataService)
    {
        var transactions = await dataService.ChartGetTransactionsPAsync(period ?? "12");

        var grouped = transactions
            .GroupBy(t => t.Category?.Parent ?? t.Category)
            .Select(g => new
            {
                Name = g.Key?.Name ?? "Unknown",
                Icon = g.Key?.Icon,
                RawAmount = g.Sum(t => (t.IsDebit ? -1 : 1) * t.Amount)
            })
            .ToList();

        var incomeItems = grouped.Where(x => x.Name == "Income").ToList();
        var expenseItems = grouped.Where(x => x.Name != "Income").ToList();

        var totalIncome = incomeItems.Sum(x => Math.Abs(x.RawAmount));
        var totalExpenses = expenseItems.Sum(x => Math.Abs(x.RawAmount));

        var income = incomeItems.Select(x => new
        {
            x.Name,
            x.Icon,
            Amount = Math.Abs(x.RawAmount),
            Percentage = totalIncome > 0 ? Math.Round((double)(Math.Abs(x.RawAmount) / totalIncome * 100), 2) : 0
        }).OrderByDescending(x => x.Amount).ToList();

        var expenses = expenseItems.Select(x => new
        {
            x.Name,
            x.Icon,
            Amount = Math.Abs(x.RawAmount),
            Percentage = totalExpenses > 0 ? Math.Round((double)(Math.Abs(x.RawAmount) / totalExpenses * 100), 2) : 0
        }).OrderByDescending(x => x.Amount).ToList();

        return TypedResults.Ok(new { income, expenses });
    }

    internal static async Task<IResult> GetMonthTransactions(string month, DataService dataService)
    {
        var transactions = await dataService.ChartGetTransactionsAsync(month);

        var dtos = transactions.Select(t => t.ToDto()).ToList();

        var categories = transactions
            .GroupBy(t => t.Category?.Parent ?? t.Category)
            .Select(g => new
            {
                Name = g.Key?.Name ?? "Unknown",
                Icon = g.Key?.Icon,
                Amount = Math.Abs(g.Sum(t => (t.IsDebit ? 1 : -1) * t.Amount)),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return TypedResults.Ok(new { transactions = dtos, categories });
    }

    internal static IResult GetPeriods()
    {
        return TypedResults.Ok(Periods);
    }
}
