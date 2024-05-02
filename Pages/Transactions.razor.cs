﻿using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MoneyManager.Pages;

public partial class Transactions
{
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] DataService dataService { get; set; }
    [Inject] IJSRuntime JSRuntime { get; set; }

    [Parameter] public bool CanGoBack { get; set; } = false;
    [Parameter] public bool CanDeleteAll { get; set; } = false;
    [Parameter] public string ChartPeriod { get; set; } = "a";
    [Parameter] public int? Category { get; set; }

    private async Task DeleteAll()
    {
        var dialog = await DialogService.ShowConfirmationAsync("Do you want to delete all transactions?", "Yes", "No", "Transactions");
        var result = await dialog.Result;
        if (!result.Cancelled)
            await dataService.DeleteAllTransactions();
    }

    private async Task GoBack() => await JSRuntime.InvokeVoidAsync("history.back");
}