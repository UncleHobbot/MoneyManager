using MoneyManager.Model.Import;

namespace MoneyManager.Components;

public partial class ImportFileDialog : IDialogContentComponent<ImportFileParams>
{
    [Inject] private TransactionService TransactionService { get; set; }

    [CascadingParameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public FluentDialog Dialog { get; set; } = default!;
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public ImportFileParams Content { get; set; } = default!;
    private async Task CancelAsync() => await Dialog.CancelAsync();
    private int progress;
    private string Status;
    private bool IsComplete;

    private async Task Import()
    {
        var actionProgress = void (int p) => progress = p;

        switch (Content.ImportType)
        {
            case ImportTypeEnum.Mint_CSV:
                await TransactionService.ImportMintCSV(Content.FileName, actionProgress);
                break;
            case ImportTypeEnum.RBC_CSV:
                await TransactionService.ImportRBCCSV(Content.FileName, false, actionProgress);
                break;
            case ImportTypeEnum.CIBC_CSV:
                await TransactionService.ImportCIBCCSV(Content.FileName, false, actionProgress);
                break;
        }

        Status = "Import complete";
        IsComplete = true;
    }
}