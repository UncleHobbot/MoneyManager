namespace MoneyManager.Services;

/// <summary>
/// Provides methods for managing auto-categorization rules in MoneyManager system.
/// </summary>
/// <remarks>
/// This partial class implements CRUD operations for <see cref="Data.Rule"/> entities.
/// Supports rule application to transactions for automatic categorization.
/// All queries eagerly load Category and Category.Parent navigation properties.
/// Rules can be tested against transactions before applying them.
/// </remarks>
public partial class DataService
{
    /// <summary>
    /// Retrieves all rules from the database.
    /// </summary>
    /// <returns>
    /// A task representing to asynchronous operation. The task result is a queryable collection of all rules.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Eagerly loads Category and Category.Parent navigation properties.
    /// Returns a queryable for further filtering and sorting in UI.
    /// </remarks>
    public async Task<IQueryable<Rule>> GetRules()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return ctx.Rules.Include(x => x.Category).Include(x => x.Category.Parent).AsQueryable();
    }

    /// <summary>
    /// Retrieves all rules that could potentially match the given transaction.
    /// </summary>
    /// <param name="transaction">
    /// The <see cref="Data.Transaction"/> to test against rules.
    /// </param>
    /// <returns>
    /// A task representing to asynchronous operation. The task result is a queryable collection of matching rules.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Performs case-insensitive pattern matching based on <see cref="Data.RuleCompareType"/>.
    /// Returns rules where transaction's original description contains rule's pattern.
    /// Further filters by comparing actual match type to ensure rule's logic is satisfied.
    /// Matching is performed on both rule pattern and transaction's original description.
    /// </remarks>
    public async Task<IQueryable<Rule>> GetPossibleRules(Transaction transaction)
    {
        var ctx = await contextFactory.CreateDbContextAsync();

        /*
        var r1 = ctx.Rules.Where(x => transaction.OriginalDescription.ToUpper().Contains(x.OriginalDescription.ToUpper())).ToList();
        var r2 = r1.Where(x =>
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
        }).ToList();
        */

        return (await ctx.Rules.Where(x => transaction.OriginalDescription.ToUpper().Contains(x.OriginalDescription.ToUpper()))
                .Include(x => x.Category).ToListAsync())
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
            }).ToList().AsQueryable();
    }

    /// <summary>
    /// Saves a new rule to the database.
    /// </summary>
    /// <param name="rule">
    /// The <see cref="Data.Rule"/> to save. Id should be 0 for a new rule.
    /// </param>
    /// <returns>
    /// A task representing to asynchronous operation. The task result is the saved rule with its new Id.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Sets Id to 0 to ensure EF Core treats this as a new entity.
    /// Updates the rule in-place with the auto-generated Id after save.
    /// Returns the rule for further use in UI.
    /// </remarks>
    public async Task<Rule> SaveNewRule(Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        rule.Id = 0;
        ctx.Rules.Update(rule);
        await ctx.SaveChangesAsync();
        return rule;
    }

    /// <summary>
    /// Adds a new rule to the database or updates an existing rule.
    /// </summary>
    /// <param name="rule">
    /// The <see cref="Data.Rule"/> to add or update.
    /// If Id is 0, a new rule is added; otherwise, an existing rule is updated.
    /// </param>
    /// <returns>
    /// A task representing to asynchronous operation. The task result is a queryable collection of all rules after operation.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Updates static cache after successful database operation.
    /// Returns refreshed rule list for UI display.
    /// </remarks>
    public async Task<IQueryable<Rule>> ChangeRule(Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (rule.Id == 0)
            ctx.Rules.Add(rule);
        else
            ctx.Rules.Update(rule);
        await ctx.SaveChangesAsync();

        return await GetRules();
    }

    /// <summary>
    /// Deletes a rule from the database.
    /// </summary>
    /// <param name="rule">
    /// The <see cref="Data.Rule"/> to delete.
    /// </param>
    /// <returns>
    /// A task representing to asynchronous operation. The task result is a queryable collection of all rules after deletion.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// This is a destructive operation and cannot be undone without a database backup.
    /// Returns refreshed rule list for UI display.
    /// </remarks>
    public async Task<IQueryable<Rule>> DeleteRule(Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        ctx.Rules.Remove(rule);
        await ctx.SaveChangesAsync();

        return await GetRules();
    }

    /// <summary>
    /// Applies a specific rule to a transaction, updating its description and category.
    /// </summary>
    /// <param name="transaction">
    /// The <see cref="Data.Transaction"/> to apply the rule to.
    /// </param>
    /// <param name="rule">
    /// The <see cref="Data.Rule"/> to apply to the transaction.
    /// </param>
    /// <returns>
    /// A task representing to asynchronous operation. The task result is the updated transaction.
    /// </returns>
    /// <remarks>
    /// Creates a fresh database context for this operation.
    /// Updates the transaction's <see cref="Data.Transaction.Description"/> with the rule's <see cref="Data.Rule.NewDescription"/>.
    /// Assigns the rule's <see cref="Data.Rule.Category"/> to the transaction.
    /// Sets the transaction's <see cref="Data.Transaction.IsRuleApplied"/> flag to true.
    /// Saves the changes and returns the updated transaction.
    /// </remarks>
    public async Task<Transaction> ApplyRule(Transaction transaction, Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var tran = await ctx.Transactions.FirstOrDefaultAsync(x => x.Id == transaction.Id);
        tran.Description = rule.NewDescription;
        tran.Category = rule.Category;
        tran.IsRuleApplied = true;
        await ctx.SaveChangesAsync();
        return tran;
    }

    /// <summary>
    /// Automatically applies the first matching rule to a transaction.
    /// </summary>
    /// <param name="transaction">
    /// The <see cref="Data.Transaction"/> to apply auto-categorization to.
    /// </param>
    /// <param name="ctx">
    /// An existing <see cref="DataContext"/> to use for database operations.
    /// </param>
    /// <returns>
    /// A task representing to asynchronous operation.
    /// </returns>
    /// <remarks>
    /// Retrieves all possible matching rules using <see cref="GetPossibleRules(Data.Transaction)"/>.
    /// Only applies the first rule if exactly one rule matches (to prevent ambiguity).
    /// When applied:
    /// <list type="bullet">
    /// <item><description>Transaction description is updated with rule's NewDescription</description></item>
    /// <item><description>Transaction's category is set to rule's category</description></item>
    /// <item><description>Transaction's IsRuleApplied flag is set to true</description></item>
    /// </list>
    /// Changes are saved to the provided context.
    /// </remarks>
    public async Task ApplyRule(Transaction transaction, DataContext ctx)
    {
        var rules = await GetPossibleRules(transaction);
        if (rules.Count() == 1)
        {
            var rule = rules.First();
            transaction.Description = rule.NewDescription;
            transaction.Category = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == rule.Category.Id);
            transaction.IsRuleApplied = true;
        }
    }
}
