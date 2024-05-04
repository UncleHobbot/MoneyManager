using Microsoft.Data.Sqlite;

namespace MoneyManager.Services;

public class DBService(IDbContextFactory<DataContext> contextFactory, SettingsService settingsService)
{
    public async Task Backup()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettings();
        var backupPath = settings.BackupPath;
        if (string.IsNullOrEmpty(settings.BackupPath))
            backupPath = Path.Combine(Application.UserAppDataPath, "Backup");

        using var location = new SqliteConnection(ctx.Database.GetConnectionString());
        using var destination = new SqliteConnection(string.Format(@$"Data Source={backupPath}\MoneyManagerBackup_{DateTime.Now:yyyyMMddHHmmss}.db"));
        location.Open();
        destination.Open();
        location.BackupDatabase(destination);
    }
}