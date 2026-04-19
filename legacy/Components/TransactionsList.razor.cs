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
    public int PageSize { get; set; } = 20;

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
    private readonly string gridTemplateColumns = "0.8fr 1.5fr 1fr 1.8fr 2.5fr 0.5fr 0.5fr";
    private readonly GridSort<TransactionDto> accountSort = GridSort<TransactionDto>.ByAscending(x => x.Account.Name);
    //private GridSort<TransactionDto> amountSort = GridSort<TransactionDto>.ByAscending(x => x.Amount);

    private readonly GridSort<TransactionDto> categorySort = GridSort<TransactionDto>
        .ByAscending(x => x.Category!.Parent == null ? x.Category.Name : x.Category.Parent.Name)
        .ThenAscending(x => x.Category!.Name);

    private readonly GridSort<TransactionDto> ruleSort = GridSort<TransactionDto>.ByAscending(x => x.IsRuleApplied);
    private IQueryable<Transaction> allTransactions = Array.Empty<Transaction>().AsQueryable();

    private IQueryable<TransactionDto> transactions
    {
        get
        {
            var result = allTransactions;

            // External date filter
            if (DateStart.HasValue) result = result.Where(x => x.Date >= DateStart);
            if (DateEnd.HasValue) result = result.Where(x => x.Date < DateEnd);

            // Custom date filter
            if (dateFilterType == "exact" && exactDate.HasValue)
            {
                result = result.Where(x => x.Date.Date == exactDate.Value.Date);
            }
            else if (dateFilterType == "range")
            {
                var effectiveStartDate = startDate;
                var effectiveEndDate = endDate;

                // Swap if min > max
                if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
                {
                    effectiveStartDate = endDate;
                    effectiveEndDate = startDate;
                }

                if (effectiveStartDate.HasValue)
                    result = result.Where(x => x.Date.Date >= effectiveStartDate.Value.Date);
                if (effectiveEndDate.HasValue)
                    result = result.Where(x => x.Date.Date <= effectiveEndDate.Value.Date);
            }

            // Custom category filter
            if (filterCategory != 0)
            {
                if (includeSubcategories)
                {
                    result = result.Where(x => x.Category != null &&
                        (x.Category.Id == filterCategory ||
                         (x.Category.Parent != null && x.Category.Parent.Id == filterCategory)));
                }
                else
                {
                    result = result.Where(x => x.Category != null && x.Category.Id == filterCategory);
                }
            }

            // External category filter
            if (Category.HasValue)
                result = result.Where(x => x.Category != null && (x.Category.Id == Category ||
                                                                  (x.Category.Parent != null &&
                                                                   x.Category.Parent.Id == Category)));

            // Custom account filter
            result = result.Where(x => filterAccount == 0 || x.Account.Id == filterAccount);

            // External account filter
            if (OnlyVisibleAccounts)
                result = result.Where(x => !x.Account.IsHideFromGraph);
            if (RuleApplied.HasValue)
            {
                var uCategory = dataService.GetCategoryByNameFromCache("Uncategorized");
                result = result.Where(x => x.Category != null && uCategory != null && x.Category.Id == uCategory.Id);
            }

            // Description filter
            result = result.Where(x =>
                string.IsNullOrWhiteSpace(filterDescription) ||
                x.Description.ToUpper().Contains(filterDescription.ToUpper()));

            // Convert to DTO for amount filtering
            var dtoList = result.ToList().Select(x => x.ToDto()).AsQueryable();

            // Amount filter (applied on DTO since AmountExt is calculated)
            if (amountFilterType == "exact" && exactAmount.HasValue)
            {
                dtoList = dtoList.Where(x => x.AmountExt == exactAmount.Value);
            }
            else if (amountFilterType == "range")
            {
                var effectiveMinAmount = minAmount;
                var effectiveMaxAmount = maxAmount;

                // Swap if min > max
                if (minAmount.HasValue && maxAmount.HasValue && minAmount.Value > maxAmount.Value)
                {
                    effectiveMinAmount = maxAmount;
                    effectiveMaxAmount = minAmount;
                }

                if (effectiveMinAmount.HasValue)
                    dtoList = dtoList.Where(x => x.AmountExt >= effectiveMinAmount.Value);
                if (effectiveMaxAmount.HasValue)
                    dtoList = dtoList.Where(x => x.AmountExt <= effectiveMaxAmount.Value);
            }

            return dtoList;
        }
    }

    // Filter properties
    private int filterCategory;
    private Category? activeFilterCategory;
    private Category? selectedCategoryFilter;
    private bool includeSubcategories = true;

    private int filterAccount;
    private Account? selectedAccountFilter;

    private string filterDescription = string.Empty;

    // Amount filters
    private string amountFilterType = "none";
    private decimal? exactAmount;
    private decimal? minAmount;
    private decimal? maxAmount;

    // Date filters
    private string dateFilterType = "none";
    private DateTime? exactDate;
    private DateTime? startDate;
    private DateTime? endDate;

    // Accessibility
    private string filterAnnouncement = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await dataService.InitStaticStorage();
        allTransactions = await dataService.GetTransactions();
    }

    protected override Task OnParametersSetAsync()
    {
        pagination.ItemsPerPage = PageSize;

        // Use flex layout for full-screen mode, fixed height for embedded mode
        if (HeightPx > 0)
        {
            gridStyle = $"height: {HeightPx}px; width: 100%; max-width: 100%; overflow: auto;";
        }
        else
        {
            gridStyle = "flex: 1; min-height: 0; width: 100%; max-width: 100%; overflow: auto;";
        }

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

    // Account filter methods
    private IEnumerable<Account> GetAccountOptions()
    {
        return dataService.GetAccounts().OrderBy(a => a.ShownName);
    }

    private string GetAccountColumnTitle()
    {
        if (filterAccount != 0)
        {
            var account = dataService.GetAccounts().FirstOrDefault(a => a.Id == filterAccount);
            return $"Account ({account?.ShownName})";
        }
        return "Account";
    }

    private void ApplyAccountFilter()
    {
        filterAccount = selectedAccountFilter?.Id ?? 0;
        pagination.SetCurrentPageIndexAsync(0);
        AnnounceFilterChange(selectedAccountFilter != null
            ? $"Filtered by account: {selectedAccountFilter.ShownName}"
            : "Account filter cleared");
    }

    // Category filter methods
    private IEnumerable<Category> GetCategoryOptions()
    {
        return dataService.GetCategories().OrderBy(c => c.Name);
    }

    private string GetCategoryColumnTitle()
    {
        if (filterCategory != 0)
        {
            var category = dataService.GetCategories().FirstOrDefault(c => c.Id == filterCategory);
            var suffix = includeSubcategories ? " + subcategories" : "";
            return $"Category ({category?.Name}{suffix})";
        }
        return "Category";
    }

    private void ApplyCategoryFilter()
    {
        filterCategory = selectedCategoryFilter?.Id ?? 0;
        activeFilterCategory = selectedCategoryFilter;
        pagination.SetCurrentPageIndexAsync(0);

        var message = selectedCategoryFilter != null
            ? $"Filtered by category: {selectedCategoryFilter.Name}" +
              (includeSubcategories ? " and subcategories" : "")
            : "Category filter cleared";

        AnnounceFilterChange(message);
    }

    // Amount filter methods
    private bool HasAmountFilter()
    {
        return amountFilterType != "none" &&
               (exactAmount.HasValue || minAmount.HasValue || maxAmount.HasValue);
    }

    private void OnAmountFilterTypeChanged()
    {
        exactAmount = null;
        minAmount = null;
        maxAmount = null;

        // Only apply immediately if clearing the filter (selecting "none")
        if (amountFilterType == "none")
        {
            ApplyAmountFilter();
        }
    }

    private void ApplyAmountFilter()
    {
        pagination.SetCurrentPageIndexAsync(0);

        string message = amountFilterType switch
        {
            "exact" when exactAmount.HasValue => $"Filtered by exact amount: {exactAmount:C}",
            "range" when minAmount.HasValue || maxAmount.HasValue =>
                $"Filtered by range: {minAmount?.ToString("C") ?? "any"} to {maxAmount?.ToString("C") ?? "any"}",
            _ => "Amount filter cleared"
        };

        AnnounceFilterChange(message);
    }

    // Date filter methods
    private bool HasDateFilter()
    {
        return dateFilterType != "none" &&
               (exactDate.HasValue || startDate.HasValue || endDate.HasValue);
    }

    private void OnDateFilterTypeChanged()
    {
        exactDate = null;
        startDate = null;
        endDate = null;

        // Only apply immediately if clearing the filter (selecting "none")
        if (dateFilterType == "none")
        {
            ApplyDateFilter();
        }
    }

    private void ApplyDateFilter()
    {
        pagination.SetCurrentPageIndexAsync(0);

        string message = dateFilterType switch
        {
            "exact" when exactDate.HasValue => $"Filtered by date: {exactDate:d}",
            "range" when startDate.HasValue || endDate.HasValue =>
                $"Filtered by date range: {startDate?.ToString("d") ?? "any"} to {endDate?.ToString("d") ?? "any"}",
            _ => "Date filter cleared"
        };

        AnnounceFilterChange(message);
    }

    // Description filter
    private void FilterByDescription()
    {
        pagination.SetCurrentPageIndexAsync(0);
        AnnounceFilterChange(string.IsNullOrWhiteSpace(filterDescription)
            ? "Description filter cleared"
            : $"Filtered by description: {filterDescription}");
    }

    // Accessibility
    private void AnnounceFilterChange(string message)
    {
        filterAnnouncement = message;
        StateHasChanged();

        // Clear after announcement to allow re-announcement
        Task.Delay(100).ContinueWith(_ =>
        {
            filterAnnouncement = string.Empty;
            InvokeAsync(StateHasChanged);
        });
    }

    // Clear filter methods
    private void ClearAccountFilter()
    {
        selectedAccountFilter = null;
        filterAccount = 0;
        pagination.SetCurrentPageIndexAsync(0);
        AnnounceFilterChange("Account filter cleared");
    }

    private void ClearCategoryFilter()
    {
        selectedCategoryFilter = null;
        filterCategory = 0;
        activeFilterCategory = null;
        pagination.SetCurrentPageIndexAsync(0);
        AnnounceFilterChange("Category filter cleared");
    }

    private void ClearAmountFilter()
    {
        amountFilterType = "none";
        exactAmount = null;
        minAmount = null;
        maxAmount = null;
        pagination.SetCurrentPageIndexAsync(0);
        AnnounceFilterChange("Amount filter cleared");
    }

    private void ClearDateFilter()
    {
        dateFilterType = "none";
        exactDate = null;
        startDate = null;
        endDate = null;
        pagination.SetCurrentPageIndexAsync(0);
        AnnounceFilterChange("Date filter cleared");
    }

    private void ClearDescriptionFilter()
    {
        filterDescription = string.Empty;
        pagination.SetCurrentPageIndexAsync(0);
        AnnounceFilterChange("Description filter cleared");
    }

    private void ClearAllFilters()
    {
        ClearAccountFilter();
        ClearCategoryFilter();
        ClearAmountFilter();
        ClearDateFilter();
        ClearDescriptionFilter();
        AnnounceFilterChange("All filters cleared");
    }

    // Check if any filter is active
    private bool HasAnyActiveFilter()
    {
        return filterAccount != 0 ||
               filterCategory != 0 ||
               HasAmountFilter() ||
               HasDateFilter() ||
               !string.IsNullOrWhiteSpace(filterDescription);
    }

    private string GetActiveFiltersSummary()
    {
        var activeFilters = new List<string>();

        if (filterAccount != 0)
            activeFilters.Add("Account");
        if (filterCategory != 0)
            activeFilters.Add("Category");
        if (HasAmountFilter())
            activeFilters.Add("Amount");
        if (HasDateFilter())
            activeFilters.Add("Date");
        if (!string.IsNullOrWhiteSpace(filterDescription))
            activeFilters.Add("Description");

        return activeFilters.Count > 0
            ? $"Active filters: {string.Join(", ", activeFilters)}"
            : string.Empty;
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