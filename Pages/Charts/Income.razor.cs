namespace MoneyManager.Pages.Charts;

public partial class Income
{
    [Inject] private DataService dataService { get; set; } = null!;
    private List<BalanceChart> DataGrid { get; set; } = [];
    private IQueryable<BalanceChart> DataQ => DataGrid.AsQueryable();
    private bool isLoading = true;
    private string chartPeriod = "m1";

    private string ChartPeriod
    {
        get => chartPeriod;
        set
        {
            chartPeriod = value;
            DataService.NetIncomeChartPeriod = value;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;
        await LoadData();
        ChartPeriod = DataService.NetIncomeChartPeriod;
        isLoading = false;
    }

    private async Task UpdateChart()
    {
        isLoading = true;
        await LoadData();
        StateHasChanged();
        isLoading = false;
    }

    private async Task LoadData()
    {
        DataGrid = await dataService.ChartNetIncome(DataService.NetIncomeChartPeriod);
        DataGrid.Add(new BalanceChart
        {
            Month = "Total",
            FirstDate = DateTime.Today.AddYears(100),
            MonthLabel = "Total",
            MonthKey = "t",
            Income = DataGrid.Sum(x => x.Income),
            Expenses = DataGrid.Sum(x => x.Expenses)
        });
    }
}