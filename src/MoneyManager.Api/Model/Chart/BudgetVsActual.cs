namespace MoneyManager.Api.Model.Chart;

/// <summary>
/// One budgeted (parent) category compared to its actual spend for the current
/// month. <see cref="Actual"/> is summed from <c>ReportingRow</c>s at the same
/// parent-rollup level as the budget. See CONTEXT.md ("Budget") / ADR-0007.
/// </summary>
public sealed record BudgetVsActual(int CategoryId, string Name, string? Icon, decimal Budget, decimal Actual);
