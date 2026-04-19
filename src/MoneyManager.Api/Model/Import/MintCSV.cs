using CsvHelper.Configuration.Attributes;

namespace MoneyManager.Api.Model.Import;

/// <summary>
/// Represents a transaction row from a Mint.com CSV export file.
/// Used for mapping CSV columns during import using CsvHelper library.
/// </summary>
public class MintCSV
{
    /// <summary>
    /// Gets or sets the transaction date.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the transaction description (user-editable in Mint).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the original description as provided by the merchant/bank.
    /// </summary>
    [Name("Original Description")]
    public string? OriginalDescription { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount (positive for credits, negative for debits).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the transaction type (credit, debit, or transfer).
    /// </summary>
    [Name("Transaction Type")]
    public string? TransactionType { get; set; }

    /// <summary>
    /// Gets or sets the category assigned to the transaction (e.g., "Groceries", "Income").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the name of the account associated with the transaction.
    /// </summary>
    [Name("Account Name")]
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets or sets any custom labels assigned to the transaction in Mint.
    /// </summary>
    public string? Labels { get; set; }

    /// <summary>
    /// Gets or sets any user notes attached to the transaction.
    /// </summary>
    public string? Notes { get; set; }
}
