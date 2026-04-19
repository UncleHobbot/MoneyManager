using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides database backup and restore functionality.
/// </summary>
public class DBService(IDbContextFactory<DataContext> contextFactory, IConfiguration configuration)
{
    private static readonly SemaphoreSlim _backupLock = new(1, 1);

    /// <summary>
    /// Gets the configured backup directory path.
    /// </summary>
    /// <returns>The backup directory path from configuration, or a default path.</returns>
    private string GetBackupPath()
    {
        return configuration["BackupPath"] ?? Path.Combine(AppContext.BaseDirectory, "backups");
    }

    /// <summary>
    /// Creates a backup of the MoneyManager SQLite database.
    /// </summary>
    /// <returns>A task representing the asynchronous backup operation.</returns>
    public async Task BackupAsync()
    {
        await _backupLock.WaitAsync();
        try
        {
            var backupPath = GetBackupPath();
            Directory.CreateDirectory(backupPath);

            var ctx = await contextFactory.CreateDbContextAsync();
            await using var location = new SqliteConnection(ctx.Database.GetConnectionString());
            var backupFile = Path.Combine(backupPath, $"MoneyManagerBackup_{DateTime.Now:yyyyMMddHHmmss}.db");
            await using var destination = new SqliteConnection($"Data Source={backupFile}");
            location.Open();
            destination.Open();
            location.BackupDatabase(destination);
        }
        finally
        {
            _backupLock.Release();
        }
    }

    /// <summary>
    /// Lists all available backup files with metadata.
    /// </summary>
    /// <returns>A list of <see cref="BackupInfo"/> ordered by creation time descending.</returns>
    public Task<List<BackupInfo>> ListBackupsAsync()
    {
        var backupPath = GetBackupPath();
        if (!Directory.Exists(backupPath))
            return Task.FromResult(new List<BackupInfo>());

        var backups = Directory.GetFiles(backupPath, "MoneyManagerBackup_*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupInfo
            {
                FileName = f.Name,
                CreatedAt = f.CreationTime,
                SizeBytes = f.Length
            })
            .ToList();

        return Task.FromResult(backups);
    }

    /// <summary>
    /// Restores the database from a backup file.
    /// </summary>
    /// <param name="filename">The backup file name to restore from.</param>
    /// <returns>A task representing the asynchronous restore operation.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified backup file does not exist.</exception>
    public async Task RestoreBackupAsync(string filename)
    {
        await _backupLock.WaitAsync();
        try
        {
            var backupPath = GetBackupPath();
            var backupFile = Path.Combine(backupPath, filename);
            if (!File.Exists(backupFile))
                throw new FileNotFoundException($"Backup file not found: {filename}");

            var ctx = await contextFactory.CreateDbContextAsync();
            var connectionString = ctx.Database.GetConnectionString();

            await using var source = new SqliteConnection($"Data Source={backupFile}");
            await using var destination = new SqliteConnection(connectionString);
            source.Open();
            destination.Open();
            source.BackupDatabase(destination);
        }
        finally
        {
            _backupLock.Release();
        }
    }

    /// <summary>
    /// Deletes old backups, keeping only the N most recent.
    /// </summary>
    /// <param name="keepCount">The number of most recent backups to keep.</param>
    /// <returns>The number of backup files deleted.</returns>
    public Task<int> CleanupBackupsAsync(int keepCount = 10)
    {
        var backupPath = GetBackupPath();
        if (!Directory.Exists(backupPath))
            return Task.FromResult(0);

        var files = Directory.GetFiles(backupPath, "MoneyManagerBackup_*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Skip(keepCount)
            .ToList();

        foreach (var file in files)
            file.Delete();

        return Task.FromResult(files.Count);
    }
}

/// <summary>
/// Represents metadata about a database backup file.
/// </summary>
public class BackupInfo
{
    /// <summary>Gets or sets the backup file name.</summary>
    public string FileName { get; set; } = null!;

    /// <summary>Gets or sets when the backup was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the backup file size in bytes.</summary>
    public long SizeBytes { get; set; }
}
