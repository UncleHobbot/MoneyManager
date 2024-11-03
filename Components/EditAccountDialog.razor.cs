using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Color = Microsoft.FluentUI.AspNetCore.Components.Color;

namespace MoneyManager.Components;

public partial class EditAccountDialog : IDialogContentComponent<Account>
{
    private EditContext _editContext = default!;
    [CascadingParameter] public FluentDialog Dialog { get; set; } = default!;
    [Parameter] public Account Content { get; set; } = default!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Content);
    }

    protected override void OnParametersSet()
    {
        stringAccountType = Content.Type.ToString();
    }

    private async Task SaveAsync()
    {
        if (_editContext.Validate())
        {
            if (stringAccountType != null)
                Content.Type = int.Parse(stringAccountType);
            await Dialog.CloseAsync(Content);
        }
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();
    private string? stringAccountType;

    private static readonly List<Option<string>> accountTypes =
    [
        new Option<string> { Value = "0", Text = "Chequing/Savings", Icon = (AccountHelper.TypeIcon(0), Color.Neutral, "start") },
        new Option<string> { Value = "1", Text = "Credit Card", Icon = (AccountHelper.TypeIcon(1), Color.Neutral, "start") },
        new Option<string> { Value = "2", Text = "Investment", Icon = (AccountHelper.TypeIcon(2), Color.Neutral, "start") },
        new Option<string> { Value = "99", Text = "Other", Icon = (AccountHelper.TypeIcon(99), Color.Neutral, "start") }
    ];
}