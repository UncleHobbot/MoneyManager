using Microsoft.AspNetCore.Components;
using Color = Microsoft.FluentUI.AspNetCore.Components.Color;
using Icon = Microsoft.FluentUI.AspNetCore.Components.Icon;

namespace MoneyManager.Pages;

public partial class CategoriesS
{
    [Inject] DataService dataService { get; set; } = null!;
    [Inject] IToastService toastService { get; set; } = null!;

    private HashSet<CategoryTree> TreeCategories { get; set; } = [];
    private List<Option<string>> categoryIcons = [];

    protected override async Task OnInitializedAsync()
    {
        await dataService.InitStaticStorage();
        TreeCategories = dataService.GetCategoriesTree();
        categoryIcons = CategoryHelper.CategoryIcons.OrderBy(x => x)
            .Select(x => new Option<string> { Value = x, Text = x, Icon = (CategoryHelper.CategoryIcon(x), Color.Neutral, "start") }).ToList();
    }

    private Icon GetIcon(string? icon) => CategoryHelper.CategoryIcon(icon);
    private CategoryTree SelectedCategory = new();
    private FluentTreeItem _selected = null!;

    private FluentTreeItem selected
    {
        get => _selected;
        set
        {
            if (_selected != value)
            {
                _selected = value;
                SelectedCategory = _selected.Data as CategoryTree;
                StateHasChanged();
            }
        }
    }

    private async Task SaveAsync()
    {
        await dataService.SaveCategory(SelectedCategory);
        toastService.ShowSuccess($"Category \"{SelectedCategory.Name}\" saved");
        //TreeCategories = dataService.GetCategoriesTree();
    }
}