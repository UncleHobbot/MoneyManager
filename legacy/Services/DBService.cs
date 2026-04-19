using Microsoft.Data.Sqlite;

namespace MoneyManager.Services;

/// <summary>
/// Provides database backup functionality for the MoneyManager application.
/// </summary>
/// <remarks>
/// This service:
/// - Creates backups of the SQLite database to a configured location
/// - Uses SQLite's built-in backup functionality for reliable backups
/// - Automatically determines backup location based on user settings or default path
/// - Generates timestamped backup files for easy identification
/// 
/// Backup Process:
/// 1. Creates database context
/// 2. Retrieves user settings to get configured backup path
/// 3. Falls back to UserAppDataPath/Backup if no path configured
/// 4. Opens source database connection
/// 5. Creates new backup database with timestamp filename
/// 6. Performs backup using SQLite's BackupDatabase method
/// 
/// The backup is a complete copy of the database at the time of backup.
/// </remarks>
public class DBService(IDbContextFactory<DataContext> contextFactory, SettingsService settingsService)
{
    /// <summary>
    /// Creates a backup of the MoneyManager SQLite database.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous backup operation.
    /// </returns>
    /// <remarks>
    /// This method:
    /// 1. Creates a database context from the factory
    /// 2. Retrieves user settings to determine backup location
    /// 3. Uses configured BackupPath if available, otherwise uses:
    ///    Application.UserAppDataPath + "Backup" folder
    /// 4. Opens a connection to the source database
    /// 5. Creates a new backup database file with timestamp:
    ///    Format: MoneyManagerBackup_yyyyMMddHHmmss.db
    ///    Example: MoneyManagerBackup_20250125153045.db
    /// 6. Performs the actual backup operation
    /// 
    /// The SQLite BackupDatabase method:
    /// - Copies all data from source to destination
    /// - Maintains database integrity
    /// - Creates a complete, independent backup file
    /// - Can be used for disaster recovery
    /// 
    /// Notes:
    /// - The backup location must have write permissions
    /// - Sufficient disk space must be available for the backup file
    /// - Multiple backups can exist side-by-side due to timestamp
    /// - This is a blocking operation for the duration of the backup
    /// 
    /// Thread Safety: This method is not thread-safe. Multiple concurrent backups may cause issues.
    /// </remarks>
    public async Task Backup()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var settings = await settingsService.GetSettings();
        var backupPath = settings.BackupPath;
        if (string.IsNullOrEmpty(settings.BackupPath))
            backupPath = Path.Combine(Application.UserAppDataPath, "Backup");

        await using var location = new SqliteConnection(ctx.Database.GetConnectionString());
        await using var destination = new SqliteConnection(string.Format(@$"Data Source={backupPath}\MoneyManagerBackup_{DateTime.Now:yyyyMMddHHmmss}.db"));
        location.Open();
        destination.Open();
        location.BackupDatabase(destination);
    }
}
