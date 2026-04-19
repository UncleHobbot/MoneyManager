using MoneyManager.Components;

namespace MoneyManager.Pages;

public partial class Rules
{
    [Inject] private DataService dataService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private readonly PaginationState pagination = new() { ItemsPerPage = 19 };
    private IQueryable<Rule> rules = Array.Empty<Rule>().AsQueryable();

    protected override async Task OnInitializedAsync()
    {
        rules = await dataService.GetRules();
    }

    private async Task EditRule(Rule rule)
    {
        var dialog = await DialogService.ShowDialogAsync<EditRuleDialog>(rule, new DialogParameters
        {
            Height = "400px",
            Width = "500px",
            Title = "Edit Rule",
            PreventDismissOnOverlayClick = true,
            PreventScroll = true,
        });

        var result = await dialog.Result;
        if (result is { Cancelled: false, Data: not null })
            rules = await dataService.ChangeRule((Rule)result.Data);
    }

    private async Task DeleteRule(Rule rule)
    {
        var dialog = await DialogService.ShowConfirmationAsync("Do you want to delete this rule?", "Yes", "No", "Rules");
        var result = await dialog.Result;
        if (!result.Cancelled)
            rules = await dataService.DeleteRule(rule);
    }
}