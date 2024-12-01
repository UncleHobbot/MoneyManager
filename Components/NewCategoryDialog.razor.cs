using Microsoft.AspNetCore.Components.Forms;

namespace MoneyManager.Components;

public partial class NewCategoryDialog: IDialogContentComponent<Category>
{
    private EditContext _editContext = default!;
    [CascadingParameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public FluentDialog Dialog { get; set; } = default!;
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Category Content { get; set; } = default!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(Content);
    }

    private FluentTextField refname;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            refname.FocusAsync();
    }
    
    private async Task SaveAsync()
    {
        if (_editContext.Validate())
        {
            await Dialog.CloseAsync(Content);
        }
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();

}