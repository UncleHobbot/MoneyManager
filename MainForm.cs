using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MoneyManager;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        
        
        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddBlazorWebViewDeveloperTools();
        services.AddFluentUIComponents();
        services.AddDataGridEntityFrameworkAdapter();
        
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        //services.AddLogging(log => log.AddSerilog(dispose: true));

        services.AddTransient<FolderPicker>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<DBService>();
        services.AddSingleton<DataService>();
        services.AddSingleton<TransactionService>();

        var dataFolder = @"c:\Projects\MoneyManager\Data";
        var dataFile = Path.Combine(dataFolder, "MoneyManager.db");
        if (!File.Exists(dataFile))
        {
            var emptyDataFile = Path.Combine(dataFolder, "MoneyManagerEmpty.db");
            if (File.Exists(emptyDataFile))
                File.Copy(emptyDataFile, dataFile);
        }
        
        services.AddDbContextFactory<DataContext>(options => options.UseSqlite($"Data Source={dataFile}"));

        blazorWebView.HostPage = "wwwroot\\index.html";
        blazorWebView.Services = services.BuildServiceProvider();
        blazorWebView.RootComponents.Add<Main>("#app");
    }
}