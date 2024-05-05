﻿using ApexCharts;
using Microsoft.AspNetCore.Components;

namespace MoneyManager.Components;

public partial class CumulativeSpending
{
    [Inject] private DataService dataService { get; set; } = null!;

    [Parameter] public int Width { get; set; } = 0;
    [Parameter] public int Height { get; set; } = 800;
    [Parameter] public bool ShowCaption { get; set; } = true;
    [Parameter] public bool ShowToolbar { get; set; } = true;
    private ApexChart<CumulativeSpendingChart>? chart;
    private ApexChartOptions<CumulativeSpendingChart>? options;
    private List<CumulativeSpendingChart> spending = [];

    protected override async Task OnInitializedAsync()
    {
        options = new ApexChartOptions<CumulativeSpendingChart>
        {
            Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 },
            Yaxis = [new YAxis { Labels = new YAxisLabels { Formatter = @"function (value) { return '$' + Number(value).toLocaleString();}" } }],
            NoData = new NoData { Text = "Calculating..." },
            Chart = new Chart { Background = "transparent", Toolbar = new Toolbar { Show = ShowToolbar } }
        };

        if (Width > 0)
            options.Chart.Width = Width;

        spending = await dataService.ChartCumulativeSpending();
    }
}