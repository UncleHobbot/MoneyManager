using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace MoneyManager.Components;

public partial class EditTransactionDialog : IDialogContentComponent<Transaction>
{
    [Inject] protected DataService dataService { get; set; }
    private EditContext _editContext = default!;
    [CascadingParameter] public FluentDialog Dialog { get; set; } = default!;
    [Parameter] public Transaction Content { get; set; } = default!;
    private HashSet<CategoryTree> TreeCategories { get; set; } = [];
    private string selectedCategory = "";
    private IQueryable<Rule> rules;
    private string activeTabId = "rule-apply";
    private readonly Rule newRule = new();
    private string newRuleType = RuleCompareType.Contains.ToString();
    private string newRuleCategory;

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(Content);
        await dataService.InitStaticStorage();
        TreeCategories = dataService.GetCategoriesTree();
        selectedCategory = Content.Category.Id.ToString();
        rules = await dataService.GetPossibleRules(Content);
        if (rules == null || !rules.Any())
            activeTabId = "rule-new";

        newRule.CompareType = RuleCompareType.StartsWith;
        newRule.OriginalDescription = Content.OriginalDescription;
        newRule.Category = Content.Category;
        newRuleCategory = Content.Category.Id.ToString();
    }

    private async Task SaveAsync()
    {
        if (_editContext.Validate())
        {
            Content.Category = dataService.GetCategoryById(int.Parse(selectedCategory));
            await Dialog.CloseAsync(Content);
        }
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();

    private async Task SaveRuleAsync()
    {
        newRule.CompareType = Enum.Parse<RuleCompareType>(newRuleType);
        newRule.Category = dataService.GetCategoryById(int.Parse(newRuleCategory));
        await dataService.SaveNewRule(newRule);
        rules = await dataService.GetPossibleRules(Content);
        if (rules.Any())
            activeTabId = "rule-apply";
    }

    private async Task ApplyRule(Rule rule)
    {
        var changedTran = await dataService.ApplyRule(Content, rule);
        await Dialog.CloseAsync(changedTran);
    }
}