namespace MoneyManager.Api.Model.Import;

/// <summary>
/// Represents a transaction row from a CIBC (Canadian Imperial Bank of Commerce) CSV export file.
/// Used for mapping CSV columns during import using CsvHelper library.
/// </summary>
public class CIBCCSV
{
    /// <summary>
    /// Gets or sets the transaction date (column index 0 in CSV).
    /// </summary>
    [CsvHelper.Configuration.Attributes.Index(0)]
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the transaction description (column index 1 in CSV).
    /// This is a required field and cannot be null.
    /// </summary>
    [CsvHelper.Configuration.Attributes.Index(1)]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets the debit amount (money going out, column index 2 in CSV).
    /// May be null for credit transactions.
    /// </summary>
    [CsvHelper.Configuration.Attributes.Index(2)]
    public decimal? AmountDebit { get; set; }

    /// <summary>
    /// Gets or sets the credit amount (money coming in, column index 3 in CSV).
    /// May be null for debit transactions.
    /// </summary>
    [CsvHelper.Configuration.Attributes.Index(3)]
    public decimal? AmountCredit { get; set; }

    /// <summary>
    /// Gets or sets the account number (column index 4 in CSV).
    /// This is a required field and cannot be null.
    /// </summary>
    [CsvHelper.Configuration.Attributes.Index(4)]
    public string AccountNumber { get; set; } = null!;
}
