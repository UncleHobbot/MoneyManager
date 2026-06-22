using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;

namespace MoneyManager.Api.Services;

/// <summary>
/// Manages opt-in per-category monthly budgets (one row per category). See
/// CONTEXT.md ("Budget") and ADR-0007.
/// </summary>
public class BudgetService(IDbContextFactory<DataContext> contextFactory)
{
    /// <summary>Returns all budgets with their category display fields, largest first.</summary>
    public async Task<List<BudgetDto>> GetBudgetsAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var budgets = await ctx.Budgets.Include(b => b.Category).ToListAsync();
        return budgets
            .OrderByDescending(b => b.Amount)
            .Select(b => new BudgetDto(b.Id, b.Category.Id, b.Category.Name, b.Category.Icon, b.Amount))
            .ToList();
    }

    /// <summary>
    /// Sets the budget for a category (creates it, or updates the existing one — one
    /// per category). Returns null if the category does not exist.
    /// </summary>
    public async Task<BudgetDto?> SetBudgetAsync(int categoryId, decimal amount)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var category = await ctx.Categories.FindAsync(categoryId);
        if (category is null) return null;

        var budget = await ctx.Budgets.Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Category.Id == categoryId);
        if (budget is null)
        {
            budget = new Budget { Category = category, Amount = amount };
            ctx.Budgets.Add(budget);
        }
        else
        {
            budget.Amount = amount;
        }

        await ctx.SaveChangesAsync();
        return new BudgetDto(budget.Id, category.Id, category.Name, category.Icon, amount);
    }

    /// <summary>Removes the budget for a category. Returns false if none existed.</summary>
    public async Task<bool> DeleteBudgetAsync(int categoryId)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var budget = await ctx.Budgets.FirstOrDefaultAsync(b => b.Category.Id == categoryId);
        if (budget is null) return false;

        ctx.Budgets.Remove(budget);
        await ctx.SaveChangesAsync();
        return true;
    }
}
