using Microsoft.AspNetCore.Components;
using MoneyManager.Components;
using MoneyManager.Model.Import;

namespace MoneyManager.Pages;

public partial class Home
{
    [Inject] private FolderPicker folderPicker { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private TransactionService TransactionService { get; set; } = null!;
    [Inject] private DBService DBService { get; set; } = null!;

    private readonly string importFileType = ImportTypeEnum.Mint_CSV.ToString();
    private string importFile = null!;
    private bool isCreateAccounts;
    private string incomeChartPeriod = "1";
    private string spendingChartPeriod = "m1";
    
    private TransactionsList uncatTransactions = null!;
    private TransactionsList recentTransactions = null!;
    private CumulativeSpending cumSpending = null!;
    private NetIncome netIncome = null!;
    private Spending spending = null!;

    private void Refresh()
    {
        StateHasChanged();
        uncatTransactions.Refresh();
        recentTransactions.Refresh();
        cumSpending.Refresh();
        netIncome.Refresh();
        spending.Refresh();
    }

    private void SelectImportFile()
    {
        string filterName = null;
        string filterExt = null;
        if (importFileType == ImportTypeEnum.Mint_CSV.ToString() || importFileType == ImportTypeEnum.RBC_CSV.ToString() || importFileType == ImportTypeEnum.CIBC_CSV.ToString())
        {
            filterName = "CSV file";
            filterExt = "csv";
        }

        importFile = folderPicker.DisplayFilePicker(filterName, filterExt);
    }

    private async Task ImportFile()
    {
        if (Enum.TryParse(importFileType, out ImportTypeEnum importTypeEnum) && !string.IsNullOrWhiteSpace(importFile))
        {
            var dialog = await DialogService.ShowDialogAsync<ImportFileDialog>(new ImportFileParams(importTypeEnum, importFile), new DialogParameters
            {
                Height = "300px",
                Width = "600px",
                Title = "Import File",
                PreventDismissOnOverlayClick = true,
                PreventScroll = true,
            });

            var result = await dialog.Result;
        }
    }

    private async Task ImportFileRBC()
    {
        importFile = folderPicker.DisplayFilePicker("RBC CSV file", "csv");
        if (string.IsNullOrWhiteSpace(importFile))
            return;
        var records = await TransactionService.ImportRBCCSV(importFile, isCreateAccounts, void (_) => { });
        var dialog = await DialogService.ShowSuccessAsync($"Imported {records} transactions from RBC file {importFile}");
        await dialog.Result;
        Refresh();
    }

    private async Task ImportFileCIBC()
    {
        importFile = folderPicker.DisplayFilePicker("CIBC CSV file", "csv");
        if (string.IsNullOrWhiteSpace(importFile))
            return;
        var records = await TransactionService.ImportCIBCCSV(importFile, isCreateAccounts, void (_) => { });
        var dialog = await DialogService.ShowSuccessAsync($"Imported {records} transactions from CIBC file {importFile}");
        await dialog.Result;
        Refresh();
    }

    private async Task ImportFileMint()
    {
        importFile = folderPicker.DisplayFilePicker("Mint.com CSV file", "csv");
        if (string.IsNullOrWhiteSpace(importFile))
            return;
        var records = await TransactionService.ImportMintCSV(importFile, void (_) => { });
        var dialog = await DialogService.ShowSuccessAsync($"Imported {records} transactions from CIBC file {importFile}");
        await dialog.Result;
        Refresh();
    }

    private async Task Backup() => await DBService.Backup();
}