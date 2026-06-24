using MoneyManager.Api.Helpers;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Services;
using MoneyManager.Api.Services.Import;

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
        IConfiguration configuration,
        string? bankType = null,
        bool createAccounts = true)
    {
        if (file is null || file.Length == 0)
            return TypedResults.BadRequest("A non-empty CSV file is required.");

        // Resolve the bank importer either from an explicit query-string hint
        // or by sniffing the file's first line. The pipeline (TransactionService.
        // ImportAsync) owns backup; the endpoint owns only HTTP receipt, bank
        // detection, and archive (file-system concerns).
        IBankImporter? importer = ParseBankType(bankType);
        if (importer is null)
        {
            importer = DetectBankType(file);
            if (importer is null)
                return TypedResults.BadRequest("Unable to determine the bank type. Please specify the bankType query parameter.");
        }

        await using var stream = file.OpenReadStream();
        var result = await transactionService.ImportAsync(stream, importer, createAccounts);

        await ArchiveCsvAsync(file, importer.BankType, configuration);

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
        => DataPaths.GetImportedDirectory(configuration);

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

    /// <summary>
    /// Maps a query-string <c>bankType</c> hint to an <see cref="IBankImporter"/>.
    /// Accepts both the short form ("Mint", "RBC", "CIBC") and the legacy enum
    /// form ("Mint_CSV", "RBC_CSV", "CIBC_CSV") for backward compatibility with
    /// any client that sends the old form.
    /// </summary>
    private static IBankImporter? ParseBankType(string? bankType)
    {
        if (string.IsNullOrWhiteSpace(bankType))
            return null;

        return bankType.ToLowerInvariant() switch
        {
            "mint" or "mint_csv" => new MintImporter(),
            "rbc" or "rbc_csv" => new RbcImporter(),
            "cibc" or "cibc_csv" => new CibcImporter(),
            _ => null,
        };
    }

    /// <summary>
    /// Sniffs the first line of the uploaded file to detect the bank format.
    /// Mint files have "Original Description" + "Transaction Type" columns;
    /// RBC files have "Account Type" + "CAD$"; CIBC files have no header row,
    /// so any non-Mint/non-RBC file falls through to <see cref="CibcImporter"/>.
    /// <see cref="CibcImporter.Validate"/> rejects genuinely non-CIBC files
    /// with a clear "expected structure" error.
    /// </summary>
    internal static IBankImporter? DetectBankType(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, leaveOpen: true);
        var firstLine = reader.ReadLine();

        if (string.IsNullOrWhiteSpace(firstLine))
            return null;

        if (firstLine.Contains("Original Description", StringComparison.OrdinalIgnoreCase)
            && firstLine.Contains("Transaction Type", StringComparison.OrdinalIgnoreCase))
            return new MintImporter();

        if (firstLine.Contains("Account Type", StringComparison.OrdinalIgnoreCase)
            && firstLine.Contains("CAD$", StringComparison.OrdinalIgnoreCase))
            return new RbcImporter();

        return new CibcImporter();
    }

    private static async Task ArchiveCsvAsync(IFormFile file, string bankType, IConfiguration configuration)
    {
        var archiveDir = GetArchivePath(configuration);
        Directory.CreateDirectory(archiveDir);

        // Filename: "{yyyy-MM-dd HHmmss} {BankType} {original}.csv". The timestamp
        // plus the original name keep batch uploads of the same bank on the same
        // day unique (the previous "{date} {BankType}.csv" form overwrote them).
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HHmmss");
        var original = SanitizeForFileName(Path.GetFileNameWithoutExtension(file.FileName));
        var archiveFileName = $"{timestamp} {bankType} {original}.csv";
        var archivePath = Path.Combine(archiveDir, archiveFileName);

        await using var source = file.OpenReadStream();
        await using var dest = File.Create(archivePath);
        await source.CopyToAsync(dest);
    }

    /// <summary>
    /// Strips path separators and characters illegal in a file name from the
    /// client-supplied original name, so it can be embedded safely in the archive
    /// filename. Falls back to "import" when nothing usable remains.
    /// </summary>
    internal static string SanitizeForFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "import";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();

        return string.IsNullOrEmpty(cleaned) ? "import" : cleaned;
    }
}
