namespace MoneyManager.Api.Model.AI;

/// <summary>
/// Represents a transaction prepared for AI analysis and categorization.
/// Used to format transaction data before sending to OpenAI API for processing.
/// </summary>
public class TransactionAI
{
    /// <summary>
    /// Gets or sets the unique identifier of the transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the account associated with this transaction.
    /// This property is required and cannot be null.
    /// </summary>
    public string Account { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction date as a string (ISO format: yyyy-MM-dd).
    /// This property is required and cannot be null.
    /// </summary>
    public string Date { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction description or merchant name.
    /// This property is required and cannot be null.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets the transaction amount (positive for credits, negative for debits).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the assigned category for the transaction.
    /// This property is required and cannot be null.
    /// </summary>
    public string Category { get; set; } = null!;

    /// <summary>
    /// Gets or sets the subcategory for hierarchical categorization.
    /// This property is required and cannot be null.
    /// </summary>
    public string SubCategory { get; set; } = null!;
}
