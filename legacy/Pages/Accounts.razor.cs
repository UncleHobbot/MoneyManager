using MoneyManager.Components;

namespace MoneyManager.Pages;

public partial class Accounts
{
    [Inject] private DataService dataService { get; set; }= null!;
    [Inject] private IDialogService DialogService { get; set; }= null!;

    private IQueryable<Account> accounts= Array.Empty<Account>().AsQueryable();
    private IQueryable<Account> FilteredItems => accounts.Where(x => x.ShownName.Contains(nameFilter, StringComparison.CurrentCultureIgnoreCase));
    private readonly PaginationState pagination = new() { ItemsPerPage = 20 };
    private string nameFilter = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await dataService.InitStaticStorage();
        accounts = dataService.GetAccounts().AsQueryable();
    }

    private void HandleShownNameFilter(ChangeEventArgs args)
    {
        if (args.Value is string value)
            nameFilter = value;
    }

    private void HandleShownNameClear()
    {
        if (string.IsNullOrWhiteSpace(nameFilter))
            nameFilter = string.Empty;
    }

    private async Task EditAccount(Account account)
    {
        var dialog = await DialogService.ShowDialogAsync<EditAccountDialog>(account, new DialogParameters
        {
            Height = "600px",
            Title = $"Edit Account",
            PreventDismissOnOverlayClick = true,
            PreventScroll = true,
        });

        var result = await dialog.Result;
        if (!result.Cancelled && result.Data != null)
            accounts = dataService.ChangeAccount((Account)result.Data).GetAwaiter().GetResult().AsQueryable();
    }
}