using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoneyManager.Model.AI;
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
        services.AddSingleton<AIService>();
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<MainForm>(optional: true);

        IConfiguration configuration = builder.Build();
        services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));

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