using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Controllers;

/// <summary>
/// Provides API endpoints for chart data used to visualize financial trends and spending patterns.
/// </summary>
/// <remarks>
/// Endpoints include:
/// <list type="bullet">
/// <item><description>Net income by month over a configurable period</description></item>
/// <item><description>Cumulative spending comparison (this month vs last month)</description></item>
/// <item><description>Spending breakdown by category with income/expense separation</description></item>
/// <item><description>Transaction details and category breakdown for a specific month</description></item>
/// <item><description>Available chart period codes</description></item>
/// </list>
/// All chart endpoints exclude transfer transactions and hidden accounts.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class ChartsController(DataService dataService) : ControllerBase
{
    /// <summary>
    /// The list of supported chart period codes with their display labels.
    /// </summary>
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
    /// Gets net income (income vs expenses) aggregated by month for the specified period.
    /// </summary>
    /// <param name="period">
    /// The chart period code (e.g., "12" for last 12 months, "y1" for this year, "m1" for this month).
    /// Defaults to "12".
    /// </param>
    /// <returns>
    /// A list of <see cref="Model.Chart.BalanceChart"/> objects, one per month, ordered chronologically.
    /// Each entry contains income, expenses, and computed balance for that month.
    /// </returns>
    [HttpGet("net-income")]
    public async Task<IActionResult> GetNetIncome([FromQuery] string period = "12")
    {
        var result = await dataService.ChartNetIncomeAsync(period);
        return Ok(result);
    }

    /// <summary>
    /// Gets cumulative spending comparison between the current month and the previous month.
    /// </summary>
    /// <returns>
    /// A list of <see cref="Model.Chart.CumulativeSpendingChart"/> objects (up to 31 entries),
    /// each containing the cumulative spending for that day of the month in both the current and previous month.
    /// </returns>
    [HttpGet("cumulative-spending")]
    public async Task<IActionResult> GetCumulativeSpending()
    {
        var result = await dataService.ChartCumulativeSpendingAsync();
        return Ok(result);
    }

    /// <summary>
    /// Gets spending breakdown by top-level category, separated into income and expense groups.
    /// </summary>
    /// <param name="period">
    /// The chart period code (e.g., "12" for last 12 months, "y1" for this year).
    /// Defaults to "12".
    /// </param>
    /// <returns>
    /// An object with two arrays: <c>income</c> and <c>expenses</c>.
    /// Each array contains category entries with name, icon, absolute amount, and percentage of total.
    /// Categories are grouped by their parent (or self if no parent).
    /// </returns>
    /// <remarks>
    /// Transactions whose top-level category is "Income" are placed in the income group;
    /// all others are placed in the expenses group. Each entry's percentage is relative
    /// to its own group total. Results are ordered by amount descending.
    /// </remarks>
    [HttpGet("spending-by-category")]
    public async Task<IActionResult> GetSpendingByCategory([FromQuery] string period = "12")
    {
        var transactions = await dataService.ChartGetTransactionsPAsync(period);

        // Group by top-level category (parent if exists, otherwise the category itself)
        var grouped = transactions
            .GroupBy(t => t.Category?.Parent ?? t.Category)
            .Select(g => new
            {
                Name = g.Key?.Name ?? "Unknown",
                Icon = g.Key?.Icon,
                // Sign: credits positive, debits negative
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

        return Ok(new { income, expenses });
    }

    /// <summary>
    /// Gets transactions and category breakdown for a specific month.
    /// </summary>
    /// <param name="month">The month in "yyMM" format (e.g., "2501" for January 2025).</param>
    /// <returns>
    /// An object containing:
    /// <list type="bullet">
    /// <item><description><c>transactions</c>: array of transaction DTOs for the month</description></item>
    /// <item><description><c>categories</c>: array of category breakdowns with name, icon, amount, and transaction count</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="FormatException">Thrown when month is not in valid "yyMM" format.</exception>
    [HttpGet("month/{month}")]
    public async Task<IActionResult> GetMonthTransactions(string month)
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

        return Ok(new { transactions = dtos, categories });
    }

    /// <summary>
    /// Gets the list of available chart period codes with their display labels.
    /// </summary>
    /// <returns>
    /// An array of objects, each containing a <c>code</c> string and a <c>label</c> string.
    /// </returns>
    [HttpGet("periods")]
    public IActionResult GetPeriods()
    {
        return Ok(Periods);
    }
}
