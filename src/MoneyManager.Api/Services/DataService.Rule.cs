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
    /// Retrieves all rules that could match the given transaction.
    /// </summary>
    /// <param name="transaction">The transaction to find matching rules for.</param>
    /// <returns>
    /// An <see cref="IQueryable{Rule}"/> containing rules whose pattern matches the
    /// transaction's <see cref="Transaction.OriginalDescription"/> according to each rule's
    /// <see cref="Rule.CompareType"/>.
    /// </returns>
    /// <remarks>
    /// Performs a two-stage filter: first a broad SQL LIKE via EF Core (case-insensitive contains),
    /// then a precise in-memory filter applying the exact <see cref="RuleCompareType"/> logic
    /// (Contains, StartsWith, EndsWith, or Equals) with case-insensitive comparison.
    /// </remarks>
    public async Task<IQueryable<Rule>> GetPossibleRulesAsync(Transaction transaction)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return (await ctx.Rules
                .Where(x => transaction.OriginalDescription.ToUpper().Contains(x.OriginalDescription.ToUpper()))
                .Include(x => x.Category)
                .ToListAsync())
            .Where(x =>
            {
                switch (x.CompareType)
                {
                    case RuleCompareType.Contains:
                        return transaction.OriginalDescription.Contains(x.OriginalDescription, StringComparison.OrdinalIgnoreCase);
                    case RuleCompareType.StartsWith:
                        return transaction.OriginalDescription.StartsWith(x.OriginalDescription, StringComparison.OrdinalIgnoreCase);
                    case RuleCompareType.EndsWith:
                        return transaction.OriginalDescription.EndsWith(x.OriginalDescription, StringComparison.OrdinalIgnoreCase);
                    case RuleCompareType.Equals:
                        return transaction.OriginalDescription.Equals(x.OriginalDescription, StringComparison.OrdinalIgnoreCase);
                    default:
                        return false;
                }
            })
            .ToList()
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

    /// <summary>
    /// Applies a specific rule to a transaction, updating its description and category.
    /// </summary>
    /// <param name="transaction">The transaction to apply the rule to.</param>
    /// <param name="rule">The rule containing the new description and category to assign.</param>
    /// <returns>
    /// The updated <see cref="Transaction"/> if found; otherwise the original transaction unchanged.
    /// </returns>
    /// <remarks>
    /// Loads the transaction from the database by ID, sets its <see cref="Transaction.Description"/>
    /// to the rule's <see cref="Rule.NewDescription"/>, assigns the rule's <see cref="Rule.Category"/>,
    /// and marks <see cref="Transaction.IsRuleApplied"/> as <c>true</c>.
    /// </remarks>
    public async Task<Transaction> ApplyRuleAsync(Transaction transaction, Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var tran = await ctx.Transactions.FirstOrDefaultAsync(x => x.Id == transaction.Id);
        if (tran == null) return transaction;
        tran.Description = rule.NewDescription;
        tran.Category = rule.Category;
        tran.IsRuleApplied = true;
        await ctx.SaveChangesAsync();
        return tran;
    }

    /// <summary>
    /// Automatically applies the first matching rule to a transaction.
    /// </summary>
    /// <param name="transaction">The transaction to auto-categorize.</param>
    /// <param name="ctx">
    /// The <see cref="DataContext"/> to use for looking up the category.
    /// This overload accepts an existing context to participate in a larger unit of work
    /// (e.g., during batch import).
    /// </param>
    /// <remarks>
    /// Finds possible matching rules via <see cref="GetPossibleRulesAsync"/>.
    /// If exactly one rule matches, it is applied: the transaction's description is updated,
    /// its category is set, and <see cref="Transaction.IsRuleApplied"/> is set to <c>true</c>.
    /// If zero or multiple rules match, no changes are made (ambiguous matches are skipped).
    /// </remarks>
    public async Task ApplyRuleAsync(Transaction transaction, DataContext ctx)
    {
        var rules = await GetPossibleRulesAsync(transaction);
        if (rules.Count() == 1)
        {
            var rule = rules.First();
            transaction.Description = rule.NewDescription;
            transaction.Category = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == rule.Category.Id);
            transaction.IsRuleApplied = true;
        }
    }
}
