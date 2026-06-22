namespace MoneyManager.Api.Model.Chart;

/// <summary>
/// One month bucket on the spending-trend x-axis. <see cref="From"/> / <see cref="To"/>
/// is the half-open window the frontend uses to drill a clicked segment into the
/// Transactions surface (ADR-0005).
/// </summary>
public sealed record SpendingTrendMonth(string Label, DateTime From, DateTime To);

/// <summary>
/// One stacked series in the spending-trend chart: a top-level (parent-rolled-up)
/// category, or the synthetic "Other" bucket. <see cref="CategoryId"/> is null for
/// "Other" (it has no single category to drill into). <see cref="Data"/> is one
/// value per month, aligned with <see cref="SpendingTrendChart.Months"/>.
/// </summary>
public sealed record SpendingTrendSeries(int? CategoryId, string Name, string? Icon, decimal[] Data);

/// <summary>
/// Monthly spending by (parent-rolled-up) category for a period: the top categories
/// by total spend plus an "Other" bucket. Expenses only; transfers excluded.
/// </summary>
public sealed record SpendingTrendChart(
    IReadOnlyList<SpendingTrendMonth> Months,
    IReadOnlyList<SpendingTrendSeries> Series);
