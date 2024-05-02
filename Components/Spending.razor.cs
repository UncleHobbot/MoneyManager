﻿using ApexCharts;
using Microsoft.AspNetCore.Components;

namespace MoneyManager.Components;

public partial class Spending
{
    [Inject] private DataService dataService { get; set; }
    [Inject] private IDialogService DialogService { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }

    [Parameter] public string ChartPeriod { get; set; } = "1";
    [Parameter] public int Width { get; set; } = 0;
    [Parameter] public int Height { get; set; } = 800;
    [Parameter] public bool ShowCaption { get; set; } = true;
    [Parameter] public bool ShowToolbar { get; set; } = true;
    private ApexChart<CategoryChart> chart;
    private ApexChartOptions<CategoryChart> options;
    private bool isLoading = true;
    private List<CategoryChart> expense = [];

    protected override async Task OnInitializedAsync()
    {
        options = new ApexChartOptions<CategoryChart>
        {
            Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 },
            Tooltip = new Tooltip
            {
                Y = new TooltipY
                {
                    Formatter = @"function(value, opts) {
                    if (value === undefined) {return '';}
                    return Number(value).toLocaleString('en-US', {style: 'currency', currency: 'USD'});}"
                }
            },
            Legend = new Legend { Formatter = @"function(seriesName, opts) { return [seriesName, ': ', Number(opts.w.globals.series[opts.seriesIndex]).toLocaleString('en-US', {style: 'currency', currency: 'USD'})] }" }
        };
        options.Chart.Background = "transparent";
        if (Width > 0)
            options.Chart.Width = Width;
    }

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        var transactions = await dataService.ChartGetTransactionsP(ChartPeriod);
        var catIncome = await dataService.GetCategoryByName("Income");
        expense = transactions.Where(x => (x.Category.Parent ?? x.Category).Id != catIncome.Id)
            .GroupBy(x => x.Category.Parent ?? x.Category)
            .Select(x => new CategoryChart
            {
                Category = x.Key,
                Amount = x.Sum(x => (x.IsDebit ? 1 : -1) * x.Amount)
            }).OrderBy(x => x.Category.Name).ToList();

        if (chart != null)
        {
            await chart.UpdateOptionsAsync(true, true, false);
            //await chart.UpdateSeriesAsync(true);
        }

        isLoading = false;
    }

    private async Task DataPointsSelected(SelectedData<CategoryChart> selectedData)
    {
        if (selectedData?.DataPoint?.Items != null && selectedData?.DataPoint?.Items?.Count() > 0)
        {
            var selectedCategory = selectedData.DataPoint.Items.First().Category;
            NavigationManager.NavigateTo($"transactions/{ChartPeriod}/{selectedCategory.Id}/true");
        }
    }
}