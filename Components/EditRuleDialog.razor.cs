using Microsoft.AspNetCore.Components.Forms;

namespace MoneyManager.Components;

public partial class EditRuleDialog: IDialogContentComponent<Rule>
{
    [Inject] protected DataService dataService { get; set; } = null!;
    private EditContext _editContext = default!;
    [CascadingParameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public FluentDialog Dialog { get; set; } = default!;
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Rule Content { get; set; } = default!;
    private string selectedType = "";

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(Content);
        await dataService.InitStaticStorage();
        selectedType = Content.CompareType.ToString();
    }

    private async Task SaveAsync()
    {
        if (_editContext.Validate())
        {
            Content.CompareType = Enum.Parse<RuleCompareType>(selectedType);
            await Dialog.CloseAsync(Content);
        }
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();
}