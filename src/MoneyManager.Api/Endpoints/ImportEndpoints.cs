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
    /// RBC files have "Account Type" + "CAD$"; CIBC files have no header row
    /// (the line is purely numeric/data).
    /// </summary>
    private static IBankImporter? DetectBankType(IFormFile file)
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

        var hasTextHeader = firstLine.Any(char.IsLetter);
        if (!hasTextHeader)
            return new CibcImporter();

        return null;
    }

    private static async Task ArchiveCsvAsync(IFormFile file, string bankType, IConfiguration configuration)
    {
        var archiveDir = GetArchivePath(configuration);
        Directory.CreateDirectory(archiveDir);

        var datePart = DateTime.Now.ToString("yyyy-MM-dd");
        // Wire-format change: archive filenames now use the short BankType
        // ("Mint.csv", "RBC.csv", "CIBC.csv") instead of the legacy enum form
        // ("Mint_CSV.csv", ...). Old archive files remain readable.
        var archiveFileName = $"{datePart} {bankType}.csv";
        var archivePath = Path.Combine(archiveDir, archiveFileName);

        await using var source = file.OpenReadStream();
        await using var dest = File.Create(archivePath);
        await source.CopyToAsync(dest);
    }
}
