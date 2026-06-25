using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for system operations: database backup and restore.
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
        ReferenceDataCache cache)
    {
        await dbService.RestoreBackupAsync(filename);
        await cache.WarmAsync();
        return TypedResults.Ok();
    }

    internal static async Task<IResult> CleanupBackups(DBService dbService, int keep = 10)
    {
        var deleted = await dbService.CleanupBackupsAsync(keep);
        return TypedResults.Ok(deleted);
    }
}
