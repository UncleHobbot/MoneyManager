using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Controllers;

/// <summary>
/// API controller for managing expense and income categories.
/// </summary>
/// <remarks>
/// Provides CRUD endpoints for categories including flat lists, hierarchical tree views,
/// and available icon names. Supports the parent-child category structure used throughout
/// the MoneyManager application.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(DataService dataService, IDbContextFactory<DataContext> contextFactory) : ControllerBase
{
    /// <summary>
    /// Gets all categories as a flat list.
    /// </summary>
    /// <returns>A list of all <see cref="Category"/> objects in the system.</returns>
    /// <response code="200">Returns the list of categories.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Category>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await dataService.GetCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Gets all categories as a hierarchical tree structure.
    /// </summary>
    /// <returns>A set of <see cref="CategoryTree"/> objects representing the category hierarchy.</returns>
    /// <remarks>
    /// Root categories appear at the top level with their children nested underneath.
    /// Categories marked as new (auto-generated during import) are excluded.
    /// </remarks>
    /// <response code="200">Returns the category tree.</response>
    [HttpGet("tree")]
    [ProducesResponseType(typeof(HashSet<CategoryTree>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesTree()
    {
        var tree = await dataService.GetCategoriesTreeAsync();
        return Ok(tree);
    }

    /// <summary>
    /// Gets the list of available category icon names.
    /// </summary>
    /// <returns>A list of icon name strings from <see cref="CategoryIconEnum"/>.</returns>
    /// <response code="200">Returns the list of icon names.</response>
    [HttpGet("icons")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public IActionResult GetIcons()
    {
        return Ok(CategoryHelper.CategoryIcons);
    }

    /// <summary>
    /// Gets a single category by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the category.</param>
    /// <returns>The <see cref="Category"/> with the specified ID.</returns>
    /// <response code="200">Returns the requested category.</response>
    /// <response code="404">Category with the specified ID was not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await dataService.GetCategoryByIdAsync(id);
        if (category is null)
            return NotFound();

        return Ok(category);
    }

    /// <summary>
    /// Creates a new category.
    /// </summary>
    /// <param name="category">The <see cref="Category"/> to create. The Id should be 0.</param>
    /// <returns>The newly created category with its generated ID.</returns>
    /// <response code="201">Category was created successfully.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Category), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] Category category)
    {
        category.Id = 0;
        var created = await dataService.ChangeCategoryAsync(category);
        return CreatedAtAction(nameof(GetCategory), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The unique identifier of the category to update.</param>
    /// <param name="category">The <see cref="Category"/> with updated values.</param>
    /// <returns>The updated category.</returns>
    /// <response code="200">Category was updated successfully.</response>
    /// <response code="400">The ID in the URL does not match the category body.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] Category category)
    {
        if (id != category.Id)
            return BadRequest("Route id does not match category id.");

        var updated = await dataService.ChangeCategoryAsync(category);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes a category by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the category to delete.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Category was deleted successfully.</response>
    /// <response code="404">Category with the specified ID was not found.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var category = await ctx.Categories.FindAsync(id);
        if (category is null)
            return NotFound();

        ctx.Categories.Remove(category);
        await ctx.SaveChangesAsync();
        return NoContent();
    }
}
