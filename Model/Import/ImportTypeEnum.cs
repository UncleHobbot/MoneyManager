namespace MoneyManager.Model.Import;

/// <summary>
/// Represents the supported financial data import formats for transaction CSV files.
/// </summary>
public enum ImportTypeEnum
{
    /// <summary>
    /// Mint.com CSV export format with columns: Date, Description, Original Description, Amount, Transaction Type, Category, Account Name, Labels, Notes.
    /// </summary>
    Mint_CSV,

    /// <summary>
    /// Royal Bank of Canada (RBC) CSV export format with columns: Account Type, Account Number, Transaction Date, Cheque Number, Description 1, Description 2, CAD$, USD$.
    /// </summary>
    RBC_CSV,

    /// <summary>
    /// Canadian Imperial Bank of Commerce (CIBC) CSV export format with columns: Date, Description, Amount Debit, Amount Credit, Account Number.
    /// </summary>
    CIBC_CSV
}

/// <summary>
/// Provides parameters for importing a financial CSV file, including the file path and import format type.
/// </summary>
/// <param name="importType">The type of CSV import format (Mint, RBC, or CIBC).</param>
/// <param name="fileName">The full path to the CSV file to import.</param>
public class ImportFileParams(ImportTypeEnum importType, string fileName)
{
    /// <summary>
    /// Gets the full path to the CSV file to import.
    /// </summary>
    public string FileName { get; } = fileName;

    /// <summary>
    /// Gets the type of CSV import format that should be used to parse the file.
    /// </summary>
    public ImportTypeEnum ImportType { get; } = importType;
}
