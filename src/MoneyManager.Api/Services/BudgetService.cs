using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;

namespace MoneyManager.Api.Services;

/// <summary>
/// Manages opt-in per-category monthly budgets (one row per category). Budgets
/// apply to top-level (parent) categories only — see CONTEXT.md ("Budget") and
/// ADR-0007.
/// </summary>
public class BudgetService(IDbContextFactory<DataContext> contextFactory)
{
    /// <summary>Returns all budgets with their category display fields, largest first.</summary>
    public async Task<List<BudgetDto>> GetBudgetsAsync()
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();
        var budgets = await ctx.Budgets.Include(b => b.Category).ToListAsync();
        return budgets
            .Where(b => b.Category is not null) // defensive: skip orphaned rows
            .OrderByDescending(b => b.Amount)
            .Select(b => new BudgetDto(b.Id, b.Category.Id, b.Category.Name, b.Category.Icon, b.Amount))
            .ToList();
    }

    /// <summary>
    /// Sets the budget for a top-level category (creates it, or updates the existing
    /// one — one per category). Returns null if the category does not exist or is not
    /// a top-level category (budgets roll up at the parent level; see ADR-0007).
    /// </summary>
    public async Task<BudgetDto?> SetBudgetAsync(int categoryId, decimal amount)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();
        var category = await ctx.Categories.Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null || category.Parent is not null) return null;

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
        await using var ctx = await contextFactory.CreateDbContextAsync();
        var budget = await ctx.Budgets.FirstOrDefaultAsync(b => b.Category.Id == categoryId);
        if (budget is null) return false;

        ctx.Budgets.Remove(budget);
        await ctx.SaveChangesAsync();
        return true;
    }
}
