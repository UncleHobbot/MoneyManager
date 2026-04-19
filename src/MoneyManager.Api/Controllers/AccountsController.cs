using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Controllers;

/// <summary>
/// API controller for managing financial accounts.
/// </summary>
/// <remarks>
/// Provides CRUD endpoints for <see cref="Account"/> entities.
/// Uses <see cref="DataService"/> for all data access and business logic.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AccountsController(DataService dataService) : ControllerBase
{
    /// <summary>
    /// Gets all accounts.
    /// </summary>
    /// <returns>A list of all <see cref="Account"/> objects.</returns>
    /// <response code="200">Returns the list of accounts.</response>
    [HttpGet]
    [ProducesResponseType<List<Account>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Account>>> GetAccounts()
    {
        var accounts = await dataService.GetAccountsAsync();
        return Ok(accounts);
    }

    /// <summary>
    /// Gets a single account by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the account.</param>
    /// <returns>The <see cref="Account"/> with the specified identifier.</returns>
    /// <response code="200">Returns the requested account.</response>
    /// <response code="404">No account with the given id exists.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<Account>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Account>> GetAccount(int id)
    {
        var accounts = await dataService.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.Id == id);
        if (account is null)
            return NotFound();

        return Ok(account);
    }

    /// <summary>
    /// Creates a new account.
    /// </summary>
    /// <param name="account">The account to create. The <c>Id</c> property should be 0.</param>
    /// <returns>The created <see cref="Account"/> with its assigned identifier.</returns>
    /// <response code="201">The account was created successfully.</response>
    [HttpPost]
    [ProducesResponseType<Account>(StatusCodes.Status201Created)]
    public async Task<ActionResult<Account>> CreateAccount(Account account)
    {
        account.Id = 0;
        var accounts = await dataService.ChangeAccountAsync(account);
        var created = accounts.FirstOrDefault(a => a.Name == account.Name) ?? account;
        return CreatedAtAction(nameof(GetAccount), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The unique identifier of the account to update.</param>
    /// <param name="account">The updated account data.</param>
    /// <returns>The updated list of accounts.</returns>
    /// <response code="200">The account was updated successfully.</response>
    /// <response code="400">The id in the route does not match the request body.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<List<Account>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Account>>> UpdateAccount(int id, Account account)
    {
        account.Id = id;
        var accounts = await dataService.ChangeAccountAsync(account);
        return Ok(accounts);
    }

    /// <summary>
    /// Deletes an account by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the account to delete.</param>
    /// <returns>No content on success; 409 Conflict if the account has linked transactions.</returns>
    /// <response code="204">The account was deleted successfully.</response>
    /// <response code="409">The account has linked transactions and cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var deleted = await dataService.DeleteAccountAsync(id);
        if (!deleted)
            return Conflict(new { message = "Account has linked transactions or was not found." });

        return NoContent();
    }
}
