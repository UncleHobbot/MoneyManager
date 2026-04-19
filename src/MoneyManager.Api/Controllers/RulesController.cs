using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Controllers;

/// <summary>
/// API controller for managing auto-categorization rules.
/// </summary>
/// <remarks>
/// Provides CRUD endpoints for <see cref="Rule"/> entities and a bulk-apply
/// endpoint that runs all rules against uncategorized transactions.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class RulesController(DataService dataService, IDbContextFactory<DataContext> contextFactory) : ControllerBase
{
    /// <summary>
    /// Retrieves all auto-categorization rules.
    /// </summary>
    /// <returns>A list of all <see cref="Rule"/> entities with their associated categories.</returns>
    /// <response code="200">Returns the list of rules.</response>
    [HttpGet]
    public async Task<ActionResult<List<Rule>>> GetAll()
    {
        var rules = await dataService.GetRulesAsync();
        return Ok(await rules.ToListAsync());
    }

    /// <summary>
    /// Retrieves a single rule by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the rule.</param>
    /// <returns>The <see cref="Rule"/> with the specified <paramref name="id"/>.</returns>
    /// <response code="200">Returns the requested rule.</response>
    /// <response code="404">No rule with the given identifier was found.</response>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Rule>> GetById(int id)
    {
        var rules = await dataService.GetRulesAsync();
        var rule = await rules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null)
            return NotFound();

        return Ok(rule);
    }

    /// <summary>
    /// Creates a new auto-categorization rule.
    /// </summary>
    /// <param name="rule">The rule to create. <see cref="Rule.Id"/> is ignored and set to 0.</param>
    /// <returns>The list of all rules after the new rule has been added.</returns>
    /// <response code="201">The rule was created successfully.</response>
    [HttpPost]
    public async Task<ActionResult<List<Rule>>> Create([FromBody] Rule rule)
    {
        rule.Id = 0;
        var rules = await dataService.ChangeRuleAsync(rule);
        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, await rules.ToListAsync());
    }

    /// <summary>
    /// Updates an existing auto-categorization rule.
    /// </summary>
    /// <param name="id">The unique identifier of the rule to update.</param>
    /// <param name="rule">The updated rule data.</param>
    /// <returns>The list of all rules after the update.</returns>
    /// <response code="200">The rule was updated successfully.</response>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<List<Rule>>> Update(int id, [FromBody] Rule rule)
    {
        rule.Id = id;
        var rules = await dataService.ChangeRuleAsync(rule);
        return Ok(await rules.ToListAsync());
    }

    /// <summary>
    /// Deletes an auto-categorization rule.
    /// </summary>
    /// <param name="id">The unique identifier of the rule to delete.</param>
    /// <returns>The list of remaining rules after deletion.</returns>
    /// <response code="200">The rule was deleted successfully.</response>
    /// <response code="404">No rule with the given identifier was found.</response>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<List<Rule>>> Delete(int id)
    {
        var rules = await dataService.GetRulesAsync();
        var rule = await rules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null)
            return NotFound();

        var remaining = await dataService.DeleteRuleAsync(rule);
        return Ok(await remaining.ToListAsync());
    }

    /// <summary>
    /// Applies all matching rules to uncategorized transactions.
    /// </summary>
    /// <returns>An object containing the count of transactions that had rules applied.</returns>
    /// <remarks>
    /// Iterates over every transaction where <see cref="Transaction.Category"/> is <c>null</c>
    /// or <see cref="Transaction.IsRuleApplied"/> is <c>false</c>, and attempts to match each
    /// against the existing rules. Only unambiguous single-rule matches are applied.
    /// Changes are saved in a single batch at the end.
    /// </remarks>
    /// <response code="200">Returns the count of transactions that had rules applied.</response>
    [HttpPost("apply-all")]
    public async Task<ActionResult> ApplyAll()
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();

        var transactions = await ctx.Transactions
            .Include(t => t.Category)
            .Where(t => t.Category == null || !t.IsRuleApplied)
            .ToListAsync();

        var appliedCount = 0;
        foreach (var transaction in transactions)
        {
            var wasApplied = transaction.IsRuleApplied;
            await dataService.ApplyRuleAsync(transaction, ctx);
            if (transaction.IsRuleApplied && !wasApplied)
                appliedCount++;
        }

        await ctx.SaveChangesAsync();
        return Ok(new { applied = appliedCount });
    }
}
