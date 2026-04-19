using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Model.Import;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Controllers;

/// <summary>
/// Handles CSV file imports from various bank formats and manages the CSV archive.
/// </summary>
/// <remarks>
/// Supports importing transactions from Mint.com, RBC, and CIBC CSV formats.
/// Uploaded files are automatically archived after a successful import.
/// A database backup is created before every import operation.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class ImportController(
    TransactionService transactionService,
    DBService dbService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Imports transactions from an uploaded CSV file.
    /// </summary>
    /// <param name="file">The CSV file to import.</param>
    /// <param name="bankType">
    /// Optional bank type identifier (<c>Mint_CSV</c>, <c>RBC_CSV</c>, or <c>CIBC_CSV</c>).
    /// When omitted the bank format is auto-detected from the file header.
    /// </param>
    /// <param name="createAccounts">
    /// When <c>true</c> (the default), accounts referenced in the CSV that do not yet exist are created automatically.
    /// </param>
    /// <returns>An <see cref="ImportResult"/> summarising the import outcome.</returns>
    /// <response code="200">Import completed successfully.</response>
    /// <response code="400">No file was provided, or the bank type could not be determined.</response>
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportResult>> UploadAsync(
        IFormFile file,
        [FromQuery] string? bankType = null,
        [FromQuery] bool createAccounts = true)
    {
        if (file is null || file.Length == 0)
            return BadRequest("A non-empty CSV file is required.");

        // Determine the import type from the query parameter or by inspecting the file.
        ImportTypeEnum? importType = ParseBankType(bankType);

        if (importType is null)
        {
            importType = await DetectBankTypeAsync(file);
            if (importType is null)
                return BadRequest("Unable to determine the bank type. Please specify the bankType query parameter.");
        }

        // Back up the database before making changes.
        await dbService.BackupAsync();

        // Run the bank-specific import.
        ImportResult result;
        await using var stream = file.OpenReadStream();

        result = importType.Value switch
        {
            ImportTypeEnum.Mint_CSV => await transactionService.ImportMintCsvAsync(stream, createAccounts),
            ImportTypeEnum.RBC_CSV => await transactionService.ImportRbcCsvAsync(stream, createAccounts),
            ImportTypeEnum.CIBC_CSV => await transactionService.ImportCibcCsvAsync(stream, createAccounts),
            _ => throw new InvalidOperationException($"Unsupported import type: {importType}")
        };

        // Archive the uploaded CSV.
        await ArchiveCsvAsync(file, importType.Value);

        return Ok(result);
    }

    /// <summary>
    /// Lists all CSV files stored in the archive directory.
    /// </summary>
    /// <returns>A collection of objects containing the file name, date, and size in bytes.</returns>
    /// <response code="200">Returns the list of archived files (may be empty).</response>
    [HttpGet("csv-archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<object>> ListCsvArchive()
    {
        var archiveDir = GetArchivePath();

        if (!Directory.Exists(archiveDir))
            return Ok(Array.Empty<object>());

        var files = new DirectoryInfo(archiveDir)
            .GetFiles("*.csv")
            .Select(f => new
            {
                fileName = f.Name,
                date = f.LastWriteTimeUtc,
                sizeBytes = f.Length
            })
            .OrderByDescending(f => f.date);

        return Ok(files);
    }

    /// <summary>
    /// Returns the content of an archived CSV file.
    /// </summary>
    /// <param name="fileName">The name of the archived CSV file.</param>
    /// <returns>The CSV file content with <c>text/csv</c> content type.</returns>
    /// <response code="200">File content returned successfully.</response>
    /// <response code="404">The requested file does not exist in the archive.</response>
    [HttpGet("csv-archive/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetCsvArchiveFile(string fileName)
    {
        var filePath = GetSafeArchiveFilePath(fileName);
        if (filePath is null || !System.IO.File.Exists(filePath))
            return NotFound();

        var stream = System.IO.File.OpenRead(filePath);
        return File(stream, "text/csv", fileName);
    }

    /// <summary>
    /// Deletes an archived CSV file.
    /// </summary>
    /// <param name="fileName">The name of the archived CSV file to delete.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">File deleted successfully.</response>
    /// <response code="404">The requested file does not exist in the archive.</response>
    [HttpDelete("csv-archive/{fileName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteCsvArchiveFile(string fileName)
    {
        var filePath = GetSafeArchiveFilePath(fileName);
        if (filePath is null || !System.IO.File.Exists(filePath))
            return NotFound();

        System.IO.File.Delete(filePath);
        return NoContent();
    }

    // ── Private helpers ─────────────────────────────────────────────

    /// <summary>
    /// Resolves the CSV archive directory path from configuration or a default location.
    /// </summary>
    /// <returns>The absolute path to the archive directory.</returns>
    private string GetArchivePath()
    {
        return configuration["CsvArchivePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "csv-archive");
    }

    /// <summary>
    /// Returns a safe, fully-qualified path inside the archive directory for the given file name,
    /// or <c>null</c> if the name attempts directory traversal.
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <returns>The full file path, or <c>null</c> when the name is invalid.</returns>
    private string? GetSafeArchiveFilePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var archiveDir = Path.GetFullPath(GetArchivePath());
        var fullPath = Path.GetFullPath(Path.Combine(archiveDir, fileName));

        // Prevent directory-traversal attacks.
        if (!fullPath.StartsWith(archiveDir, StringComparison.OrdinalIgnoreCase))
            return null;

        return fullPath;
    }

    /// <summary>
    /// Parses a bank-type string into the corresponding <see cref="ImportTypeEnum"/>.
    /// </summary>
    /// <param name="bankType">The string value (e.g. <c>"Mint_CSV"</c>).</param>
    /// <returns>The parsed enum value, or <c>null</c> if the string is empty or unrecognised.</returns>
    private static ImportTypeEnum? ParseBankType(string? bankType)
    {
        if (string.IsNullOrWhiteSpace(bankType))
            return null;

        return Enum.TryParse<ImportTypeEnum>(bankType, ignoreCase: true, out var result)
            ? result
            : null;
    }

    /// <summary>
    /// Auto-detects the bank CSV format by inspecting the first line of the uploaded file.
    /// </summary>
    /// <param name="file">The uploaded form file whose stream position will be reset after reading.</param>
    /// <returns>The detected <see cref="ImportTypeEnum"/>, or <c>null</c> if detection fails.</returns>
    private static async Task<ImportTypeEnum?> DetectBankTypeAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, leaveOpen: true);
        var firstLine = await reader.ReadLineAsync();

        if (string.IsNullOrWhiteSpace(firstLine))
            return null;

        if (firstLine.Contains("Original Description", StringComparison.OrdinalIgnoreCase)
            && firstLine.Contains("Transaction Type", StringComparison.OrdinalIgnoreCase))
        {
            return ImportTypeEnum.Mint_CSV;
        }

        if (firstLine.Contains("Account Type", StringComparison.OrdinalIgnoreCase)
            && firstLine.Contains("CAD$", StringComparison.OrdinalIgnoreCase))
        {
            return ImportTypeEnum.RBC_CSV;
        }

        // CIBC files typically have no text header — the first line is data.
        var hasTextHeader = firstLine.Any(char.IsLetter);
        if (!hasTextHeader)
            return ImportTypeEnum.CIBC_CSV;

        return null;
    }

    /// <summary>
    /// Copies the uploaded CSV file to the archive directory with a date-stamped name.
    /// </summary>
    /// <param name="file">The uploaded form file to archive.</param>
    /// <param name="importType">The bank type used to label the archived file.</param>
    private async Task ArchiveCsvAsync(IFormFile file, ImportTypeEnum importType)
    {
        var archiveDir = GetArchivePath();
        Directory.CreateDirectory(archiveDir);

        var datePart = DateTime.Now.ToString("yyyy-MM-dd");
        var archiveFileName = $"{datePart} {importType}.csv";
        var archivePath = Path.Combine(archiveDir, archiveFileName);

        await using var source = file.OpenReadStream();
        await using var dest = System.IO.File.Create(archivePath);
        await source.CopyToAsync(dest);
    }
}
