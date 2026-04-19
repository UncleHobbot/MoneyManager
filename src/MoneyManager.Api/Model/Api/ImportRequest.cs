namespace MoneyManager.Api.Model.Api;

/// <summary>
/// Represents the result of a transaction import operation.
/// </summary>
/// <remarks>
/// Returned after processing a CSV file import to provide a summary
/// of how many transactions were imported, skipped, and any errors encountered.
/// </remarks>
public class ImportResult
{
    /// <summary>
    /// Gets or sets the number of transactions successfully imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of transactions skipped (e.g., duplicates).
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of transactions found in the import file.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the bank type identifier used for the import (e.g., "Mint_CSV", "RBC_CSV", "CIBC_CSV").
    /// </summary>
    public string BankType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list of error messages encountered during import.
    /// Empty if no errors occurred.
    /// </summary>
    public List<string> Errors { get; set; } = [];
}
