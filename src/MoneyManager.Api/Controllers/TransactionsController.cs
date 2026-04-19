using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Controllers;

/// <summary>
/// API controller for managing financial transactions.
/// </summary>
/// <remarks>
/// Provides endpoints for querying, updating, and deleting <see cref="Transaction"/> entities,
/// as well as aggregated statistics and CSV export.
/// Uses <see cref="DataService"/> for all data access and business logic.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class TransactionsController(DataService dataService) : ControllerBase
{
    /// <summary>
    /// Gets a paginated list of transactions filtered by period, account, and category.
    /// </summary>
    /// <param name="period">The period code for date filtering (e.g., "12", "y1", "m1", "w", "a"). Defaults to "12".</param>
    /// <param name="accountId">Optional account identifier to filter transactions by.</param>
    /// <param name="categoryId">Optional category identifier to filter transactions by.</param>
    /// <param name="page">The page number for pagination (1-based). Defaults to 1.</param>
    /// <param name="pageSize">The number of items per page. Defaults to 50.</param>
    /// <returns>A paginated result containing transaction DTOs, total count, page, and page size.</returns>
    /// <response code="200">Returns the paginated list of transactions.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string period = "12",
        [FromQuery] int? accountId = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        dataService.GetDates(period, out var startDate, out var endDate);

        var query = await dataService.GetTransactionsAsync();
        query = query.Where(t => t.Date >= startDate && t.Date < endDate);

        if (accountId.HasValue)
            query = query.Where(t => t.Account.Id == accountId.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.Category != null && t.Category.Id == categoryId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            items = items.Select(t => t.ToDto()).ToList(),
            totalCount,
            page,
            pageSize
        });
    }

    /// <summary>
    /// Gets a single transaction by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction.</param>
    /// <returns>The <see cref="TransactionDto"/> with the specified identifier.</returns>
    /// <response code="200">Returns the requested transaction.</response>
    /// <response code="404">No transaction with the given id exists.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetTransaction(int id)
    {
        var query = await dataService.GetTransactionsAsync();
        var transaction = await query.FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null)
            return NotFound();

        return Ok(transaction.ToDto());
    }

    /// <summary>
    /// Updates an existing transaction's description and/or category.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to update.</param>
    /// <param name="request">The update request containing fields to change. Only non-null fields are applied.</param>
    /// <returns>The updated <see cref="TransactionDto"/>.</returns>
    /// <response code="200">The transaction was updated successfully.</response>
    /// <response code="404">No transaction with the given id exists.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<TransactionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> UpdateTransaction(int id, UpdateTransactionRequest request)
    {
        var query = await dataService.GetTransactionsAsync();
        var transaction = await query.FirstOrDefaultAsync(t => t.Id == id);

        if (transaction is null)
            return NotFound();

        if (request.Description is not null)
            transaction.Description = request.Description;

        if (request.CategoryId.HasValue)
        {
            var category = await dataService.GetCategoryByIdAsync(request.CategoryId.Value);
            if (category is not null)
                transaction.Category = category;
        }

        await dataService.ChangeTransactionAsync(transaction);
        return Ok(transaction.ToDto());
    }

    /// <summary>
    /// Deletes a transaction by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the transaction to delete.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">The transaction was deleted successfully.</response>
    /// <response code="404">No transaction with the given id exists.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var deleted = await dataService.DeleteTransactionAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Deletes all transactions from the database.
    /// </summary>
    /// <returns>No content on success.</returns>
    /// <response code="204">All transactions were deleted successfully.</response>
    /// <remarks>
    /// This is a destructive bulk operation that removes every transaction record.
    /// Use with caution — there is no undo.
    /// </remarks>
    [HttpDelete("bulk")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAllTransactions()
    {
        await dataService.DeleteAllTransactionsAsync();
        return NoContent();
    }

    /// <summary>
    /// Gets aggregated transaction statistics for the specified period.
    /// </summary>
    /// <param name="period">The period code for date filtering (e.g., "12", "y1", "m1", "w", "a"). Defaults to "12".</param>
    /// <returns>An object containing income, expenses, net balance, and transaction count.</returns>
    /// <response code="200">Returns the transaction statistics.</response>
    /// <remarks>
    /// Income is the sum of all credit (non-debit) transaction amounts.
    /// Expenses is the sum of all debit transaction amounts.
    /// Net is calculated as income minus expenses.
    /// </remarks>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats([FromQuery] string period = "12")
    {
        dataService.GetDates(period, out var startDate, out var endDate);

        var query = await dataService.GetTransactionsAsync();
        var transactions = await query
            .Where(t => t.Date >= startDate && t.Date < endDate)
            .ToListAsync();

        var income = transactions.Where(t => !t.IsDebit).Sum(t => t.Amount);
        var expenses = transactions.Where(t => t.IsDebit).Sum(t => t.Amount);

        return Ok(new
        {
            income,
            expenses,
            net = income - expenses,
            count = transactions.Count
        });
    }

    /// <summary>
    /// Exports transactions for the specified period as a CSV file.
    /// </summary>
    /// <param name="period">The period code for date filtering (e.g., "12", "y1", "m1", "w", "a"). Defaults to "12".</param>
    /// <returns>A CSV-formatted string of transaction data.</returns>
    /// <response code="200">Returns the CSV content with text/csv content type.</response>
    /// <remarks>
    /// Uses the AI export format which includes account name, date, amount, category, and description.
    /// The CSV uses invariant culture formatting and includes a header row.
    /// </remarks>
    [HttpGet("export")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTransactions([FromQuery] string period = "12")
    {
        var csv = await dataService.AIGetTransactionsCSVAsync(period);
        return Content(csv, "text/csv");
    }
}
