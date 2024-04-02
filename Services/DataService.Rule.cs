﻿namespace MoneyManager.Services;

public partial class DataService
{
    public async Task<IQueryable<Rule>> GetRules(Transaction transaction)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return (await ctx.Rules.Where(x => transaction.Description.ToUpper().Contains(x.OriginalDescription.ToUpper()))
                .Include(x => x.Category).ToListAsync())
            .Where(x =>
            {
                switch (x.CompareType)
                {
                    case RuleCompareType.Contains:
                        return transaction.Description.Contains(x.OriginalDescription, StringComparison.OrdinalIgnoreCase);
                    case RuleCompareType.StartsWith:
                        return transaction.Description.StartsWith(x.OriginalDescription, StringComparison.OrdinalIgnoreCase);
                    case RuleCompareType.EndsWith:
                        return transaction.Description.EndsWith(x.OriginalDescription, StringComparison.OrdinalIgnoreCase);
                    case RuleCompareType.Equals:
                        return transaction.Description.Equals(x.OriginalDescription, StringComparison.OrdinalIgnoreCase);
                    default:
                        return false;
                }
            }).ToList().AsQueryable();
    }

    public async Task<Rule> SaveNewRule(Rule rule)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        rule.Id = 0;
        ctx.Rules.Update(rule);
        await ctx.SaveChangesAsync();
        return rule;
    }

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

    public async Task ApplyRule(Transaction transaction, DataContext ctx)
    {
        var rules = await GetRules(transaction);
        if (rules.Count() == 1)
        {
            var rule = rules.First();
            transaction.Description = rule.NewDescription;
            transaction.Category = await ctx.Categories.FirstOrDefaultAsync(x => x.Id == rule.Category.Id);
            transaction.IsRuleApplied = true;
        }
    }
}