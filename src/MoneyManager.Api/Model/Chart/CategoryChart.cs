namespace MoneyManager.Api.Model.Chart;

/// <summary>
/// One (parent-rolled-up) category's slice of a spending breakdown: its display
/// fields, the period's spend, the spend in the immediately-preceding window of
/// the same length (for a delta), and the share of its side (income or expenses).
/// </summary>
public sealed record CategoryChart(
    string Name,
    string? Icon,
    decimal Amount,
    decimal PreviousAmount,
    double Percentage);

/// <summary>
/// The spending-by-category response: categories split into income and expense
/// sides, each ordered largest-first. Wire shape: <c>{ income: [...], expenses: [...] }</c>.
/// </summary>
public sealed record SpendingByCategoryChart(
    IReadOnlyList<CategoryChart> Income,
    IReadOnlyList<CategoryChart> Expenses);
