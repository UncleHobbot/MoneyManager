using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace MoneyManager.Components;

public partial class EditRuleDialog: IDialogContentComponent<Rule>
{
    [Inject] protected DataService dataService { get; set; }
    private EditContext _editContext = default!;
    [CascadingParameter] public FluentDialog Dialog { get; set; } = default!;
    [Parameter] public Rule Content { get; set; } = default!;
    private HashSet<CategoryTree> TreeCategories { get; set; } = [];
    private string selectedType = "";
    private string selectedCategory = "";

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(Content);
        await dataService.InitStaticStorage();
        TreeCategories = dataService.GetCategoriesTree();
        selectedCategory = Content.Category.Id.ToString();
        selectedType = Content.CompareType.ToString();
    }

    private async Task SaveAsync()
    {
        if (_editContext.Validate())
        {
            Content.CompareType = Enum.Parse<RuleCompareType>(selectedType);
            Content.Category = dataService.GetCategoryById(int.Parse(selectedCategory));
            await Dialog.CloseAsync(Content);
        }
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();
}