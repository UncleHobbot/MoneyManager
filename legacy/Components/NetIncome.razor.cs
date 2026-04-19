using ApexCharts;

namespace MoneyManager.Components;

public partial class NetIncome
{
    [Inject] private DataService dataService { get; set; } = null!;
    
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public string ChartPeriod { get; set; } = "1";
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public int Width { get; set; } = 0;
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public int Height { get; set; } = 800;
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public bool ShowCaption { get; set; } = true;
    [Parameter] [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] public bool ShowToolbar { get; set; } = true;
    
    private ApexChart<BalanceChart> chart = null!;
    private ApexChartOptions<BalanceChart> options = null!;
    private bool isLoading = true;
    private List<BalanceChart> DataChart { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        options = new ApexChartOptions<BalanceChart>
        {
            Theme = new Theme { Mode = Mode.Dark, Palette = PaletteType.Palette1 },
            Chart = new Chart { Stacked = true, Background = "transparent", Toolbar = new Toolbar { Show = ShowToolbar } },
            Colors = ["#5cb85c", "#d9534f", "#545454"],
            //DataLabels = new DataLabels { Formatter = @"function(value, opts) { return '$' + Number(value).toLocaleString();}" },
            Yaxis = [new YAxis { Labels = new YAxisLabels { Formatter = @"function (value) { return '$' + Number(value).toLocaleString();}" } }],
            NoData = new NoData { Text = "Calculating..." },
        };

        if (Width > 0)
            options.Chart.Width = Width;
    }

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        DataChart = await dataService.ChartNetIncome(ChartPeriod);
        if (chart != null)
            await chart.UpdateSeriesAsync();
        isLoading = false;
    }
    
    public void Refresh() => StateHasChanged();
}