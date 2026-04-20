using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for managing expense and income categories.
/// </summary>
public static class CategoryEndpoints
{
    /// <summary>
    /// Maps all category-related endpoints under <c>/api/categories</c>.
    /// </summary>
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/categories").WithTags("Categories");

        group.MapGet("/", GetAll);
        group.MapGet("/tree", GetTree);
        group.MapGet("/icons", GetIcons);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
    }

    internal static async Task<IResult> GetAll(DataService dataService)
    {
        var categories = await dataService.GetCategoriesAsync();
        return TypedResults.Ok(categories);
    }

    internal static async Task<IResult> GetTree(DataService dataService)
    {
        var tree = await dataService.GetCategoriesTreeAsync();
        return TypedResults.Ok(tree);
    }

    internal static IResult GetIcons()
    {
        return TypedResults.Ok(CategoryHelper.CategoryIcons);
    }

    internal static async Task<IResult> GetById(int id, DataService dataService)
    {
        var category = await dataService.GetCategoryByIdAsync(id);
        return category is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(category);
    }

    internal static async Task<IResult> Create(Category category, DataService dataService)
    {
        category.Id = 0;
        var created = await dataService.ChangeCategoryAsync(category);
        return TypedResults.Created($"/api/categories/{created.Id}", created);
    }

    internal static async Task<IResult> Update(int id, Category category, DataService dataService)
    {
        if (id != category.Id)
            return TypedResults.BadRequest("Route id does not match category id.");

        var updated = await dataService.ChangeCategoryAsync(category);
        return TypedResults.Ok(updated);
    }

    internal static async Task<IResult> Delete(int id, IDbContextFactory<DataContext> contextFactory)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var category = await ctx.Categories.FindAsync(id);
        if (category is null)
            return TypedResults.NotFound();

        ctx.Categories.Remove(category);
        await ctx.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}
