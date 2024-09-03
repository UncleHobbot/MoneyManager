using Microsoft.AspNetCore.Components;

namespace MoneyManager.Pages.Charts;

public partial class Income
{
    [Inject] private DataService dataService { get; set; } = null!;
    private List<BalanceChart> DataGrid { get; set; } = [];
    private IQueryable<BalanceChart> DataQ => DataGrid.AsQueryable();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        isLoading = true;
        await LoadData();
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