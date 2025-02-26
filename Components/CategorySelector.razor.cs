﻿namespace MoneyManager.Components;

public partial class CategorySelector
{
    [Inject] protected DataService dataService { get; set; } = null!;

    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Category? Category { get; set; } = null!;
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public EventCallback<Category> CategoryChanged { get; set; }
    
    private HashSet<CategoryTree> TreeCategories { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        TreeCategories = dataService.GetCategoriesTree();
    }

    private string? selectedCategory
    {
        get => Category?.Id.ToString();
        set
        {
            if (value != null)
            {
                Category = dataService.GetCategoryById(int.Parse(value));
                CategoryChanged.InvokeAsync(Category);
            }
        }
    }
}