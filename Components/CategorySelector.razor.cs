using Microsoft.AspNetCore.Components;

namespace MoneyManager.Components;

public partial class CategorySelector
{
    [Inject] protected DataService dataService { get; set; } = null!;

    [Parameter] public Category Category { get; set; } = null!;
    [Parameter] public EventCallback<Category> CategoryChanged { get; set; }
    
    private HashSet<CategoryTree> TreeCategories { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        TreeCategories = dataService.GetCategoriesTree();
    }

    private string selectedCategory
    {
        get => Category.Id.ToString();
        set
        {
            Category = dataService.GetCategoryById(int.Parse(value));
            CategoryChanged.InvokeAsync(Category);
        }
    }
}