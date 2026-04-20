using MoneyManager.Api.Data;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for managing financial accounts.
/// </summary>
public static class AccountEndpoints
{
    /// <summary>
    /// Maps all account-related endpoints under <c>/api/accounts</c>.
    /// </summary>
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/accounts").WithTags("Accounts");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:int}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:int}", Update);
        group.MapDelete("/{id:int}", Delete);
    }

    internal static async Task<IResult> GetAll(DataService dataService)
    {
        var accounts = await dataService.GetAccountsAsync();
        return TypedResults.Ok(accounts);
    }

    internal static async Task<IResult> GetById(int id, DataService dataService)
    {
        var accounts = await dataService.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.Id == id);
        return account is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(account);
    }

    internal static async Task<IResult> Create(Account account, DataService dataService)
    {
        account.Id = 0;
        var accounts = await dataService.ChangeAccountAsync(account);
        var created = accounts.FirstOrDefault(a => a.Name == account.Name) ?? account;
        return TypedResults.Created($"/api/accounts/{created.Id}", created);
    }

    internal static async Task<IResult> Update(int id, Account account, DataService dataService)
    {
        account.Id = id;
        var accounts = await dataService.ChangeAccountAsync(account);
        return TypedResults.Ok(accounts);
    }

    internal static async Task<IResult> Delete(int id, DataService dataService)
    {
        var deleted = await dataService.DeleteAccountAsync(id);
        return deleted
            ? TypedResults.NoContent()
            : TypedResults.Conflict(new { message = "Account has linked transactions or was not found." });
    }
}
