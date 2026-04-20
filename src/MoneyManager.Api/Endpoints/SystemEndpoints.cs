using MoneyManager.Api.Model;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for system operations including backup/restore and settings.
/// </summary>
public static class SystemEndpoints
{
    /// <summary>
    /// Maps all system-related endpoints under <c>/api/system</c>.
    /// </summary>
    public static void MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/system").WithTags("System");

        group.MapPost("/backup", Backup);
        group.MapGet("/backups", ListBackups);
        group.MapPost("/backups/{filename}/restore", RestoreBackup);
        group.MapDelete("/backups/cleanup", CleanupBackups);
        group.MapGet("/settings", GetSettings);
        group.MapPut("/settings", SaveSettings);
    }

    internal static async Task<IResult> Backup(DBService dbService)
    {
        await dbService.BackupAsync();
        return TypedResults.Ok();
    }

    internal static async Task<IResult> ListBackups(DBService dbService)
    {
        var backups = await dbService.ListBackupsAsync();
        return TypedResults.Ok(backups);
    }

    internal static async Task<IResult> RestoreBackup(
        string filename,
        DBService dbService,
        DataService dataService)
    {
        await dbService.RestoreBackupAsync(filename);
        await dataService.WarmCacheAsync();
        return TypedResults.Ok();
    }

    internal static async Task<IResult> CleanupBackups(DBService dbService, int keep = 10)
    {
        var deleted = await dbService.CleanupBackupsAsync(keep);
        return TypedResults.Ok(deleted);
    }

    internal static async Task<IResult> GetSettings(SettingsService settingsService)
    {
        var settings = await settingsService.GetSettingsAsync();
        return TypedResults.Ok(settings);
    }

    internal static async Task<IResult> SaveSettings(SettingsModel data, SettingsService settingsService)
    {
        await settingsService.SaveSettingsAsync(data);
        return TypedResults.Ok();
    }
}
