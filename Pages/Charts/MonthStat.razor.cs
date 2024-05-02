using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;
using Microsoft.JSInterop;

namespace MoneyManager.Pages.Charts;

public partial class MonthStat
{
    [Inject] private DataService dataService { get; set; }
    [Inject] IJSRuntime JSRuntime { get; set; }

    [Parameter] public string Month { get; set; }
    private List<Transaction> transactions = [];
    private List<CategoryChart> income = [];
    private List<CategoryChart> expense = [];
    private ApexChart<CategoryChart> chartI;
    private ApexChart<CategoryChart> chartE;
    private ApexChartOptions<CategoryChart> optionsI { get; set; } = new();
    private ApexChartOptions<CategoryChart> optionsE { get; set; } = new();
    private DateTime dateStart;
    private DateTime dateEnd;
    private Category selectedCategory;

    protected override async Task OnInitializedAsync()
    {
        optionsI = new ApexChartOptions<CategoryChart>
        {
            Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 },
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

        optionsE = new ApexChartOptions<CategoryChart>
        {
            Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 },
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

    protected override Task OnParametersSetAsync()
    {
        dateStart = DateTime.ParseExact(Month, "yyMM", Thread.CurrentThread.CurrentCulture);
        dateEnd = dateStart.AddMonths(1).StartOfMonth(Thread.CurrentThread.CurrentCulture);

        return base.OnParametersSetAsync();
    }

    private async Task GoBack() => await JSRuntime.InvokeVoidAsync("history.back");

    private void DataPointsSelected(SelectedData<CategoryChart> selectedData)
    {
        if (selectedData?.DataPoint?.Items != null && selectedData.DataPoint?.Items?.Count() > 0)
            selectedCategory = selectedData.DataPoint.Items.First().Category;
    }

    private async Task LoadData()
    {
        transactions = await dataService.ChartGetTransactions(Month);
        var catIncome = await dataService.GetCategoryByName("Income");

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
    }

    private async Task TransactionChanged()
    {
        await LoadData();
        await chartI.UpdateSeriesAsync();
        await chartE.UpdateSeriesAsync();
    }
}