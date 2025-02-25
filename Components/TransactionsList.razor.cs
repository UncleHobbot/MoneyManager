namespace MoneyManager.Components;

public partial class TransactionsList
{
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private DataService dataService { get; set; } = null!;

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TransactionListModeEnum Mode { get; set; } = TransactionListModeEnum.Full;

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int HeightVH { get; set; } = 80;

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int HeightPx { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ChartPeriod { get; set; } = "a";

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime? DateStart { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateTime? DateEnd { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PageSize { get; set; } = 19;

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int? Category { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CategoryFilters { get; set; } = true;

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool OnlyVisibleAccounts { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool? RuleApplied { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EditEnable { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback Changed { get; set; }

    private readonly PaginationState pagination = new();
    private string gridStyle = string.Empty;
    private string gridTemplateColumns = "100px 200px 150px 300px 500px";
    private GridSort<TransactionDto> accountSort = GridSort<TransactionDto>.ByAscending(x => x.Account.Name);
    private GridSort<TransactionDto> amountSort = GridSort<TransactionDto>.ByAscending(x => x.Amount);

    private GridSort<TransactionDto> categorySort = GridSort<TransactionDto>
        .ByAscending(x => x.Category!.Parent == null ? x.Category.Name : x.Category.Parent.Name)
        .ThenAscending(x => x.Category!.Name);

    private GridSort<TransactionDto> ruleSort = GridSort<TransactionDto>.ByAscending(x => x.IsRuleApplied);
    private IQueryable<Transaction> allTransactions = Array.Empty<Transaction>().AsQueryable();

    private IQueryable<TransactionDto> transactions
    {
        get
        {
            var result = allTransactions;
            // external date filter
            if (DateStart.HasValue) result = result.Where(x => x.Date >= DateStart);
            if (DateEnd.HasValue) result = result.Where(x => x.Date < DateEnd);
            // custom category filter
            result = result.Where(x => x.Category != null && (filterCategory == 0 || x.Category.Id == filterCategory ||
                                                              (x.Category.Parent != null &&
                                                               x.Category.Parent.Id == filterCategory)));
            // external category filter   
            if (Category.HasValue)
                result = result.Where(x => x.Category != null && (x.Category.Id == Category ||
                                                                  (x.Category.Parent != null &&
                                                                   x.Category.Parent.Id == Category)));
            // custom account filter
            result = result.Where(x => filterAccount == 0 || x.Account.Id == filterAccount);
            // external account filter
            if (OnlyVisibleAccounts)
                result = result.Where(x => !x.Account.IsHideFromGraph);
            if (RuleApplied.HasValue)
            {
                var uCategory = dataService.GetCategoryByNameFromCache("Uncategorized");
                result = result.Where(x => x.Category != null && uCategory != null && x.Category.Id == uCategory.Id);
            }

            result = result.Where(x =>
                string.IsNullOrWhiteSpace(filterDescription) ||
                x.Description.ToUpper().Contains(filterDescription.ToUpper()));
            return result.ToList().Select(x => x.ToDto()).AsQueryable();
        }
    }

    private int filterCategory = 0;
    private Category? activeFilterCategory;
    private int filterAccount = 0;
    private string filterDescription = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await dataService.InitStaticStorage();
        allTransactions = await dataService.GetTransactions();
    }

    protected override Task OnParametersSetAsync()
    {
        pagination.ItemsPerPage = PageSize;
        var height = $"{HeightVH}vh";
        if (HeightPx > 0)
            height = $"{HeightPx}px";
        gridStyle = $"height: {height};width: 100%;overflow:auto;";
        if (string.IsNullOrEmpty(ChartPeriod))
            ChartPeriod = "a";

        if (ChartPeriod != "a")
        {
            dataService.GetDates(ChartPeriod, out var startDate, out var endDate);
            DateStart = startDate;
            DateEnd = endDate;
        }

        return base.OnParametersSetAsync();
    }

    public void Refresh() => StateHasChanged();

    private void FilterByCategory(TransactionDto transaction, bool isSet)
    {
        if (transaction.Category != null)
        {
            filterCategory = isSet ? transaction.Category.Id : 0;
            activeFilterCategory = isSet ? transaction.Category : null;
        }
    }

    private void FilterByAccount(TransactionDto transaction, bool isSet) =>
        filterAccount = isSet ? transaction.Account.Id : 0;

    private void FilterByDescription(ChangeEventArgs args)
    {
        if (args.Value is string value)
            filterDescription = value;
    }

    private void FilterByDescriptionClear()
    {
        if (string.IsNullOrWhiteSpace(filterDescription))
            filterDescription = string.Empty;
    }

    private async Task EditTransaction(TransactionDto transactionDTO)
    {
        var transaction = transactionDTO.Transaction;
        var dialog = await DialogService.ShowDialogAsync<EditTransactionDialog>(transaction, new DialogParameters
        {
            Height = "700px",
            Width = "800px",
            Title = "Edit Transaction",
            PreventDismissOnOverlayClick = true,
            PreventScroll = true,
        });

        var result = await dialog.Result;
        if (result is { Cancelled: false, Data: not null })
        {
            allTransactions = await dataService.ChangeTransaction((Transaction)result.Data);
            await Changed.InvokeAsync();
        }
    }
}