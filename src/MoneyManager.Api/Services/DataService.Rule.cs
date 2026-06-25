using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides methods for managing auto-categorization rules.
/// </summary>
public partial class DataService
{
    /// <summary>
    /// Retrieves all rules from the database.
    /// </summary>
    /// <returns>
    /// An <see cref="IQueryable{Rule}"/> containing all rules with their associated
    /// <see cref="Rule.Category"/> and parent category loaded.
    /// </returns>
    public async Task<IQueryable<Rule>> GetRulesAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return ctx.Rules
            .Include(x => x.Category)
            .Include(x => x.Category.Parent)
            .AsQueryable();
    }

    /// <summary>
    /// Saves a new rule to the database.
    /// </summary>
    /// <param name="rule">The rule entity to save. Its <see cref="Rule.Id"/> will be reset to 0 to ensure a new record is created.</param>
    /// <returns>The saved <see cref="Rule"/> entity with its database-generated ID populated.</returns>
    public async Task<Rule> SaveNewRuleAsync(Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        rule.Id = 0;
        ctx.Rules.Update(rule);
        await ctx.SaveChangesAsync();
        return rule;
    }

    /// <summary>
    /// Adds a new rule or updates an existing rule.
    /// </summary>
    /// <param name="rule">
    /// The rule to save. If <see cref="Rule.Id"/> is 0, a new rule is added;
    /// otherwise the existing rule is updated.
    /// </param>
    /// <returns>
    /// An <see cref="IQueryable{Rule}"/> containing all rules after the change.
    /// </returns>
    public async Task<IQueryable<Rule>> ChangeRuleAsync(Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (rule.Id == 0)
            ctx.Rules.Add(rule);
        else
            ctx.Rules.Update(rule);
        await ctx.SaveChangesAsync();
        return await GetRulesAsync();
    }

    /// <summary>
    /// Deletes a rule from the database.
    /// </summary>
    /// <param name="rule">The rule entity to delete.</param>
    /// <returns>
    /// An <see cref="IQueryable{Rule}"/> containing all remaining rules after deletion.
    /// </returns>
    public async Task<IQueryable<Rule>> DeleteRuleAsync(Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        ctx.Rules.Remove(rule);
        await ctx.SaveChangesAsync();
        return await GetRulesAsync();
    }

}
