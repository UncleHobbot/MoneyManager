using CsvHelper.Configuration.Attributes;

namespace MoneyManager.Api.Model.Import;

/// <summary>
/// Represents a transaction row from an RBC (Royal Bank of Canada) CSV export file.
/// Used for mapping CSV columns during import using CsvHelper library.
/// </summary>
public class RBCCSV
{
    /// <summary>
    /// Gets or sets the type of account (e.g., "Chequing", "Savings", "Credit Card").
    /// </summary>
    [Name("Account Type")]
    public string? AccountType { get; set; }

    /// <summary>
    /// Gets or sets the account number (partial, masked for security).
    /// </summary>
    [Name("Account Number")]
    public string? AccountNumber { get; set; }

    /// <summary>
    /// Gets or sets the date when the transaction occurred.
    /// </summary>
    [Name("Transaction Date")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the cheque number (if applicable for cheque transactions).
    /// </summary>
    [Name("Cheque Number")]
    public string? ChequeNumber { get; set; }

    /// <summary>
    /// Gets or sets the primary description line of the transaction.
    /// </summary>
    [Name("Description 1")]
    public string? Description1 { get; set; }

    /// <summary>
    /// Gets or sets the secondary description line with additional transaction details.
    /// </summary>
    [Name("Description 2")]
    public string? Description2 { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount in Canadian dollars.
    /// May be null for transactions in other currencies.
    /// </summary>
    [Name("CAD$")]
    public decimal? AmountCAD { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount in US dollars.
    /// May be null for transactions in other currencies.
    /// </summary>
    [Name("USD$")]
    public decimal? AmountUSD { get; set; }
}
