using ApexCharts;

namespace MoneyManager.Pages.Charts;

public partial class Spending
{
    [Inject] private DataService dataService { get; set; } = null!;
    // [Inject] IJSRuntime JSRuntime { get; set; } = null!;

    private List<Transaction> transactions = [];
    private List<CategoryChart> income = [];
    private List<CategoryChart> expense = [];
    private ApexChart<CategoryChart>? chartI;
    private ApexChart<CategoryChart>? chartE;
    private ApexChartOptions<CategoryChart> optionsI { get; set; } = new();
    private ApexChartOptions<CategoryChart> optionsE { get; set; } = new();

    private string ChartPeriod { get; set; } = "m1";
    private DateTime dateStart;
    private DateTime dateEnd;
    private Category? selectedCategory;

    protected override async Task OnInitializedAsync()
    {
        optionsI = new ApexChartOptions<CategoryChart>
        {
            Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 },
            DataLabels = new DataLabels { Formatter = @"function(value, opts) {return  Math.round(Number(value)) + '%';}" },
            PlotOptions = new PlotOptions
            {
                Pie = new PlotOptionsPie
                {
                    Donut = new PlotOptionsDonut
                    {
                        Labels = new DonutLabels
                        {
                            Total = new DonutLabelTotal
                            {
                                FontSize = "24px", Color = "#D807B8",
                                Formatter = @"function (w) {return (w.globals.seriesTotals.reduce((a, b) => { return (a + b) }, 0)).toLocaleString('en-US', {style: 'currency', currency: 'USD'})}"
                            },
                            // Value = new DonutLabelValue
                            // {
                            //     FontSize = "22px", 
                            //     Formatter = @"function (w) {return (w.globals.seriesTotals.reduce((a, b) => { return (a + b) }, 0)).toLocaleString('en-US', {style: 'currency', currency: 'USD'})}"
                            // }
                        }
                    }
                }
            },
            Tooltip = new Tooltip
            {
                Y = new TooltipY
                {
                    Formatter = @"function(value, opts) {
                    if (value === undefined) {return '';}
                    return Number(value).toLocaleString('en-US', {style: 'currency', currency: 'USD'});}"
                }
            },
            Legend = new Legend { Formatter = @"function(seriesName, opts) { return [seriesName, ': ', Number(opts.w.globals.series[opts.seriesIndex]).toLocaleString('en-US', {style: 'currency', currency: 'USD'})] }" },
            Chart = { Background = "transparent" }
        };

        optionsE = new ApexChartOptions<CategoryChart>
        {
            Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 },
            DataLabels = new DataLabels { Formatter = @"function(value, opts) {return  Math.round(Number(value)) + '%';}" },
            PlotOptions = new PlotOptions
            {
                Pie = new PlotOptionsPie
                {
                    Donut = new PlotOptionsDonut
                    {
                        Labels = new DonutLabels
                        {
                            Total = new DonutLabelTotal
                            {
                                FontSize = "24px", Color = "#D807B8",
                                Formatter = @"function (w) {return (w.globals.seriesTotals.reduce((a, b) => { return (a + b) }, 0)).toLocaleString('en-US', {style: 'currency', currency: 'USD'})}"
                            }
                        }
                    }
                }
            },
            Tooltip = new Tooltip
            {
                Y = new TooltipY
                {
                    Formatter = @"function(value, opts) {
                    if (value === undefined) {return '';}
                    return Number(value).toLocaleString('en-US', {style: 'currency', currency: 'USD'});}"
                }
            },
            Legend = new Legend { Formatter = @"function(seriesName, opts) { return [seriesName, ': ', Number(opts.w.globals.series[opts.seriesIndex]).toLocaleString('en-US', {style: 'currency', currency: 'USD'})] }" },
            Chart = { Background = "transparent" }
        };

        await LoadData();
    }

    // protected override Task OnParametersSetAsync()
    // {
    //     dataService.GetDates(ChartPeriod, out dateStart, out dateEnd);
    //     return base.OnParametersSetAsync();
    // }

    private async Task UpdateChart()
    {
        await LoadData();
        StateHasChanged();
    }

    private void DataPointsSelected(SelectedData<CategoryChart> selectedData)
    {
        if (selectedData.DataPoint?.Items != null && selectedData.DataPoint?.Items?.Count() > 0)
            selectedCategory = selectedData.DataPoint.Items.First().Category;
    }

    private async Task LoadData()
    {
        dataService.GetDates(ChartPeriod, out dateStart, out dateEnd);
        transactions = await dataService.ChartGetTransactions(dateStart, dateEnd);
        var catIncome = await dataService.GetCategoryByName("Income");

        if (catIncome != null)
        {
            income = transactions.Where(x => (x.Category.Parent ?? x.Category).Id == catIncome.Id)
                .GroupBy(x => x.Category)
                .Select(x => new CategoryChart
                {
                    Category = x.Key,
                    Amount = x.Sum(y => (y.IsDebit ? -1 : 1) * y.Amount)
                }).OrderBy(x => x.Category.Name).ToList();

            expense = transactions.Where(x => (x.Category.Parent ?? x.Category).Id != catIncome.Id)
                .GroupBy(x => x.Category.Parent ?? x.Category)
                .Select(x => new CategoryChart
                {
                    Category = x.Key,
                    Amount = x.Sum(y => (y.IsDebit ? 1 : -1) * y.Amount)
                }).OrderBy(x => x.Category.Name).ToList();

            if (chartI != null)
                await chartI.UpdateOptionsAsync(true, true, false);
            if (chartE != null)
                await chartE.UpdateOptionsAsync(true, true, false);
        }
    }

    private async Task TransactionChanged()
    {
        await LoadData();
    }
}