using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Model;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Controllers;

/// <summary>
/// Provides system-level operations including database backup/restore,
/// backup management, and application settings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemController(DBService dbService, SettingsService settingsService, DataService dataService)
    : ControllerBase
{
    /// <summary>
    /// Creates a new backup of the application database.
    /// </summary>
    /// <returns>An <see cref="OkResult"/> when the backup completes successfully.</returns>
    [HttpPost("backup")]
    public async Task<IActionResult> Backup()
    {
        await dbService.BackupAsync();
        return Ok();
    }

    /// <summary>
    /// Lists all available database backups.
    /// </summary>
    /// <returns>A list of <see cref="BackupInfo"/> describing each backup file.</returns>
    [HttpGet("backups")]
    public async Task<ActionResult<List<BackupInfo>>> ListBackups()
    {
        return await dbService.ListBackupsAsync();
    }

    /// <summary>
    /// Restores the database from a specified backup file and refreshes in-memory caches.
    /// </summary>
    /// <param name="filename">The backup file name to restore from.</param>
    /// <returns>An <see cref="OkResult"/> when the restore and cache warm-up complete successfully.</returns>
    [HttpPost("backups/{filename}/restore")]
    public async Task<IActionResult> RestoreBackup(string filename)
    {
        await dbService.RestoreBackupAsync(filename);
        await dataService.WarmCacheAsync();
        return Ok();
    }

    /// <summary>
    /// Removes old backups, keeping only the most recent ones.
    /// </summary>
    /// <param name="keep">The number of most recent backups to retain. Defaults to 10.</param>
    /// <returns>The number of backup files that were deleted.</returns>
    [HttpDelete("backups/cleanup")]
    public async Task<ActionResult<int>> CleanupBackups([FromQuery] int keep = 10)
    {
        return await dbService.CleanupBackupsAsync(keep);
    }

    /// <summary>
    /// Retrieves the current application settings.
    /// </summary>
    /// <returns>The current <see cref="SettingsModel"/>.</returns>
    [HttpGet("settings")]
    public async Task<ActionResult<SettingsModel>> GetSettings()
    {
        return await settingsService.GetSettingsAsync();
    }

    /// <summary>
    /// Updates the application settings.
    /// </summary>
    /// <param name="data">The updated settings to save.</param>
    /// <returns>An <see cref="OkResult"/> when the settings are saved successfully.</returns>
    [HttpPut("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] SettingsModel data)
    {
        await settingsService.SaveSettingsAsync(data);
        return Ok();
    }
}
