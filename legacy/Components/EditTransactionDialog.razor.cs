using Microsoft.AspNetCore.Components.Forms;

namespace MoneyManager.Components;

public partial class EditTransactionDialog : IDialogContentComponent<Transaction>
{
    [Inject] protected DataService dataService { get; set; } = null!;
    private EditContext _editContext = default!;
    [CascadingParameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public FluentDialog Dialog { get; set; } = default!;
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public Transaction Content { get; set; } = default!;

    private IQueryable<Rule> rules = new List<Rule>().AsQueryable();
    private string activeTabId = "rule-apply";
    private readonly Rule newRule = new();
    private string newRuleType = RuleCompareType.Contains.ToString();

    protected override async Task OnInitializedAsync()
    {
        _editContext = new EditContext(Content);
        await dataService.InitStaticStorage();
        rules = await dataService.GetPossibleRules(Content);
        if (!rules.Any())
            activeTabId = "rule-new";

        newRule.CompareType = RuleCompareType.StartsWith;
        newRule.OriginalDescription = Content.OriginalDescription;
        newRule.Category = Content.Category;
    }

    private async Task SaveAsync()
    {
        if (_editContext.Validate())
            await Dialog.CloseAsync(Content);
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();

    private async Task SaveRuleAsync()
    {
        newRule.CompareType = Enum.Parse<RuleCompareType>(newRuleType);
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