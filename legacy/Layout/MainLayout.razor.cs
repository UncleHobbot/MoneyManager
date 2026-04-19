namespace MoneyManager.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private SettingsService service { get; set; } = null!;

    private SettingsModel data = new();

    protected override async Task OnInitializedAsync()
    {
        data = await service.GetSettings();
    }
}