namespace MoneyManager.Api.Model.Api;

/// <summary>A budget as returned to the client, with its category's display fields.</summary>
public sealed record BudgetDto(int Id, int CategoryId, string CategoryName, string? Icon, decimal Amount);

/// <summary>Request to set (create or update) the budget for a category.</summary>
public sealed record BudgetRequest(int CategoryId, decimal Amount);
