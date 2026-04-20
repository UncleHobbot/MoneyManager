using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Model.Import;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for CSV file imports and archive management.
/// </summary>
public static class ImportEndpoints
{
    /// <summary>
    /// Maps all import-related endpoints under <c>/api/import</c>.
    /// </summary>
    public static void MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/import").WithTags("Import");

        group.MapPost("/upload", Upload).DisableAntiforgery();
        group.MapGet("/csv-archive", ListCsvArchive);
        group.MapGet("/csv-archive/{fileName}", GetCsvArchiveFile);
        group.MapDelete("/csv-archive/{fileName}", DeleteCsvArchiveFile);
    }

    internal static async Task<IResult> Upload(
        IFormFile file,
        TransactionService transactionService,
        DBService dbService,
        IConfiguration configuration,
        string? bankType = null,
        bool createAccounts = true)
    {
        if (file is null || file.Length == 0)
            return TypedResults.BadRequest("A non-empty CSV file is required.");

        ImportTypeEnum? importType = ParseBankType(bankType);

        if (importType is null)
        {
            importType = await DetectBankTypeAsync(file);
            if (importType is null)
                return TypedResults.BadRequest("Unable to determine the bank type. Please specify the bankType query parameter.");
        }

        await dbService.BackupAsync();

        ImportResult result;
        await using var stream = file.OpenReadStream();

        result = importType.Value switch
        {
            ImportTypeEnum.Mint_CSV => await transactionService.ImportMintCsvAsync(stream, createAccounts),
            ImportTypeEnum.RBC_CSV => await transactionService.ImportRbcCsvAsync(stream, createAccounts),
            ImportTypeEnum.CIBC_CSV => await transactionService.ImportCibcCsvAsync(stream, createAccounts),
            _ => throw new InvalidOperationException($"Unsupported import type: {importType}")
        };

        await ArchiveCsvAsync(file, importType.Value, configuration);

        return TypedResults.Ok(result);
    }

    internal static IResult ListCsvArchive(IConfiguration configuration)
    {
        var archiveDir = GetArchivePath(configuration);

        if (!Directory.Exists(archiveDir))
            return TypedResults.Ok(Array.Empty<object>());

        var files = new DirectoryInfo(archiveDir)
            .GetFiles("*.csv")
            .Select(f => new
            {
                fileName = f.Name,
                date = f.LastWriteTimeUtc,
                sizeBytes = f.Length
            })
            .OrderByDescending(f => f.date);

        return TypedResults.Ok(files);
    }

    internal static IResult GetCsvArchiveFile(string fileName, IConfiguration configuration)
    {
        var filePath = GetSafeArchiveFilePath(fileName, configuration);
        if (filePath is null || !File.Exists(filePath))
            return TypedResults.NotFound();

        var stream = File.OpenRead(filePath);
        return Results.File(stream, "text/csv", fileName);
    }

    internal static IResult DeleteCsvArchiveFile(string fileName, IConfiguration configuration)
    {
        var filePath = GetSafeArchiveFilePath(fileName, configuration);
        if (filePath is null || !File.Exists(filePath))
            return TypedResults.NotFound();

        File.Delete(filePath);
        return TypedResults.NoContent();
    }

    // ── Private helpers ─────────────────────────────────────────────

    private static string GetArchivePath(IConfiguration configuration)
    {
        return configuration["CsvArchivePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "csv-archive");
    }

    private static string? GetSafeArchiveFilePath(string fileName, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var archiveDir = Path.GetFullPath(GetArchivePath(configuration));
        var fullPath = Path.GetFullPath(Path.Combine(archiveDir, fileName));

        if (!fullPath.StartsWith(archiveDir, StringComparison.OrdinalIgnoreCase))
            return null;

        return fullPath;
    }

    private static ImportTypeEnum? ParseBankType(string? bankType)
    {
        if (string.IsNullOrWhiteSpace(bankType))
            return null;

        return Enum.TryParse<ImportTypeEnum>(bankType, ignoreCase: true, out var result)
            ? result
            : null;
    }

    private static async Task<ImportTypeEnum?> DetectBankTypeAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, leaveOpen: true);
        var firstLine = await reader.ReadLineAsync();

        if (string.IsNullOrWhiteSpace(firstLine))
            return null;

        if (firstLine.Contains("Original Description", StringComparison.OrdinalIgnoreCase)
            && firstLine.Contains("Transaction Type", StringComparison.OrdinalIgnoreCase))
            return ImportTypeEnum.Mint_CSV;

        if (firstLine.Contains("Account Type", StringComparison.OrdinalIgnoreCase)
            && firstLine.Contains("CAD$", StringComparison.OrdinalIgnoreCase))
            return ImportTypeEnum.RBC_CSV;

        var hasTextHeader = firstLine.Any(char.IsLetter);
        if (!hasTextHeader)
            return ImportTypeEnum.CIBC_CSV;

        return null;
    }

    private static async Task ArchiveCsvAsync(IFormFile file, ImportTypeEnum importType, IConfiguration configuration)
    {
        var archiveDir = GetArchivePath(configuration);
        Directory.CreateDirectory(archiveDir);

        var datePart = DateTime.Now.ToString("yyyy-MM-dd");
        var archiveFileName = $"{datePart} {importType}.csv";
        var archivePath = Path.Combine(archiveDir, archiveFileName);

        await using var source = file.OpenReadStream();
        await using var dest = File.Create(archivePath);
        await source.CopyToAsync(dest);
    }
}
