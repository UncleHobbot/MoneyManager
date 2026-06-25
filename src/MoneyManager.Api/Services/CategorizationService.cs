using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Applies the auto-categorization <see cref="Rule"/>s to transactions: matching a
/// transaction's <see cref="Transaction.OriginalDescription"/> against the rule
/// patterns and, when exactly one rule matches, assigning that rule's
/// <see cref="Rule.NewDescription"/> and <see cref="Rule.Category"/>. See CONTEXT.md
/// ("Categorization") and ADR-0009.
/// </summary>
/// <remarks>
/// Rule CRUD lives elsewhere (<see cref="DataService"/>); this module owns *applying*
/// rules, not editing them. The matcher returns all matches; the "exactly one"
/// ambiguity rule lives in the apply paths. <see cref="DataContext"/> appears in the
/// interface only on <see cref="AutoApplyAsync"/>, which must join the caller's unit of
/// work (import) — see ADR-0009.
/// </remarks>
public class CategorizationService(IDbContextFactory<DataContext> contextFactory)
{
    /// <summary>
    /// Returns every rule whose pattern matches the transaction's
    /// <see cref="Transaction.OriginalDescription"/>. Two-stage: a coarse SQL
    /// <c>LIKE</c> pre-filter, then a precise in-memory pass applying each rule's
    /// <see cref="RuleCompareType"/> (case-insensitive).
    /// </summary>
    public async Task<IReadOnlyList<Rule>> GetMatchingRulesAsync(Transaction transaction)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();

        var candidates = await ctx.Rules
            .Where(x => transaction.OriginalDescription.ToUpper().Contains(x.OriginalDescription.ToUpper()))
            .Include(x => x.Category)
            .ToListAsync();

        return candidates.Where(x => Matches(transaction.OriginalDescription, x)).ToList();
    }

    /// <summary>
    /// Auto-categorizes an in-flight transaction inside the caller's unit of work: if
    /// exactly one rule matches, sets its description, category (resolved in
    /// <paramref name="ctx"/> so it is tracked), and <see cref="Transaction.IsRuleApplied"/>.
    /// Does not save — the caller owns persistence (import).
    /// </summary>
    public async Task AutoApplyAsync(Transaction transaction, DataContext ctx)
    {
        var matches = await GetMatchingRulesAsync(transaction);
        if (matches.Count != 1)
            return;

        var rule = matches[0];
        transaction.Description = rule.NewDescription;
        transaction.Category = await ctx.Categories.FirstOrDefaultAsync(c => c.Id == rule.Category.Id);
        transaction.IsRuleApplied = true;
    }

    /// <summary>
    /// Applies a specific rule to a persisted transaction and saves.
    /// </summary>
    /// <returns>The updated transaction, or <c>null</c> if the transaction or rule is not found.</returns>
    public async Task<Transaction?> ApplyRuleAsync(int transactionId, int ruleId)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();

        var transaction = await ctx.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId);
        if (transaction is null)
            return null;

        var rule = await ctx.Rules.Include(r => r.Category).FirstOrDefaultAsync(r => r.Id == ruleId);
        if (rule is null)
            return null;

        transaction.Description = rule.NewDescription;
        transaction.Category = await ctx.Categories.FirstOrDefaultAsync(c => c.Id == rule.Category.Id);
        transaction.IsRuleApplied = true;
        await ctx.SaveChangesAsync();

        // Re-fetch with the includes ToDto needs (account + category hierarchy).
        return await ctx.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Include(t => t.Category!.Parent)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    /// <summary>
    /// Re-applies rules to every transaction that still needs categorization
    /// (<c>Category == null || !IsRuleApplied</c>), saving once.
    /// </summary>
    /// <returns>The number of transactions newly marked as rule-applied.</returns>
    public async Task<int> RecategorizePendingAsync()
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
            await AutoApplyAsync(transaction, ctx);
            if (transaction.IsRuleApplied && !wasApplied)
                appliedCount++;
        }

        await ctx.SaveChangesAsync();
        return appliedCount;
    }

    private static bool Matches(string originalDescription, Rule rule) => rule.CompareType switch
    {
        RuleCompareType.Contains => originalDescription.Contains(rule.OriginalDescription, StringComparison.OrdinalIgnoreCase),
        RuleCompareType.StartsWith => originalDescription.StartsWith(rule.OriginalDescription, StringComparison.OrdinalIgnoreCase),
        RuleCompareType.EndsWith => originalDescription.EndsWith(rule.OriginalDescription, StringComparison.OrdinalIgnoreCase),
        RuleCompareType.Equals => originalDescription.Equals(rule.OriginalDescription, StringComparison.OrdinalIgnoreCase),
        _ => false,
    };
}
