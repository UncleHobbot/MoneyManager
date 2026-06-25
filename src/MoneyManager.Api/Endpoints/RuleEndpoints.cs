using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for managing auto-categorization rules.
/// </summary>
public static class RuleEndpoints
{
    /// <summary>
    /// Maps all rule-related endpoints under <c>/api/rules</c>.
    /// </summary>
    public static void MapRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/rules").WithTags("Rules");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
        group.MapPost("/apply-all", ApplyAll);
    }

    internal static async Task<IResult> GetAll(DataService dataService)
    {
        var rules = await dataService.GetRulesAsync();
        return TypedResults.Ok(await rules.ToListAsync());
    }

    internal static async Task<IResult> GetById(int id, DataService dataService)
    {
        var rules = await dataService.GetRulesAsync();
        var rule = await rules.FirstOrDefaultAsync(r => r.Id == id);
        return rule is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(rule);
    }

    internal static async Task<IResult> Create(Rule rule, DataService dataService)
    {
        rule.Id = 0;
        var rules = await dataService.ChangeRuleAsync(rule);
        return TypedResults.Created($"/api/rules/{rule.Id}", await rules.ToListAsync());
    }

    internal static async Task<IResult> Update(int id, Rule rule, DataService dataService)
    {
        rule.Id = id;
        var rules = await dataService.ChangeRuleAsync(rule);
        return TypedResults.Ok(await rules.ToListAsync());
    }

    internal static async Task<IResult> Delete(int id, DataService dataService)
    {
        var rules = await dataService.GetRulesAsync();
        var rule = await rules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null)
            return TypedResults.NotFound();

        var remaining = await dataService.DeleteRuleAsync(rule);
        return TypedResults.Ok(await remaining.ToListAsync());
    }

    internal static async Task<IResult> ApplyAll(CategorizationService categorization)
    {
        var applied = await categorization.RecategorizePendingAsync();
        return TypedResults.Ok(new { applied });
    }
}
