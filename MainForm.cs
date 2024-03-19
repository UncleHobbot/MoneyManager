using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyManager;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddMudServices();
        services.AddFluentUIComponents();
        services.AddDataGridEntityFrameworkAdapter();

        services.AddTransient<FolderPicker>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<DataService>();
        services.AddSingleton<TransactionService>();
        
        services.AddDbContextFactory<DataContext>(options => options.UseSqlite(@"Data Source=c:\Projects\MoneyManager\Data\MoneyManager.db"));
        services.AddSingleton<Seed>();

        blazorWebView.HostPage = "wwwroot\\index.html";
        blazorWebView.Services = services.BuildServiceProvider();
        blazorWebView.RootComponents.Add<Main>("#app");
        
        
    }
}