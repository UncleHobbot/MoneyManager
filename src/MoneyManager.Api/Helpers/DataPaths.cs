using Microsoft.Data.Sqlite;

namespace MoneyManager.Api.Helpers;

/// <summary>
/// Resolves on-disk data locations from the SQLite connection string. The
/// directory holding the database file is the single source of truth for where
/// backups and CSV archives live — there is no separate path override (ADR-0008).
/// </summary>
/// <remarks>
/// In production the DB is <c>/app/data/MoneyManager.db</c>, so the data dir is
/// the mounted volume <c>/app/data</c>; in dev it is <c>../../data</c> next to the
/// dev database. Relative paths are resolved to absolute against the current
/// working directory.
/// </remarks>
public static class DataPaths
{
    /// <summary>
    /// Gets the absolute directory containing the SQLite database file, derived
    /// from <c>ConnectionStrings:DefaultConnection</c>.
    /// </summary>
    public static string GetDataDirectory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        var dbPath = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));

        return directory
            ?? throw new InvalidOperationException($"Could not resolve a data directory from '{dbPath}'.");
    }

    /// <summary>Gets the absolute directory where database backups are stored.</summary>
    public static string GetBackupDirectory(IConfiguration configuration)
        => Path.Combine(GetDataDirectory(configuration), "backup");

    /// <summary>Gets the absolute directory where imported CSV files are archived.</summary>
    public static string GetImportedDirectory(IConfiguration configuration)
        => Path.Combine(GetDataDirectory(configuration), "imported");
}
