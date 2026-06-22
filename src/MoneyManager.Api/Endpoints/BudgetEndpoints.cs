using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for per-category monthly budgets (CONTEXT.md "Budget").
/// </summary>
public static class BudgetEndpoints
{
    /// <summary>Maps all budget endpoints under <c>/api/budgets</c>.</summary>
    public static void MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/budgets").WithTags("Budgets");

        group.MapGet("/", GetBudgets);
        group.MapPut("/", SetBudget);
        group.MapDelete("/{categoryId:int}", DeleteBudget);
    }

    internal static async Task<IResult> GetBudgets(BudgetService budgetService)
        => TypedResults.Ok(await budgetService.GetBudgetsAsync());

    internal static async Task<IResult> SetBudget(BudgetRequest request, BudgetService budgetService)
    {
        if (request.Amount <= 0)
            return TypedResults.BadRequest("Amount must be greater than zero.");

        var dto = await budgetService.SetBudgetAsync(request.CategoryId, request.Amount);
        return dto is null
            ? TypedResults.NotFound("Category not found.")
            : TypedResults.Ok(dto);
    }

    internal static async Task<IResult> DeleteBudget(int categoryId, BudgetService budgetService)
    {
        var deleted = await budgetService.DeleteBudgetAsync(categoryId);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
