namespace MoneyManager.Api.Model.Api;

/// <summary>
/// Represents a request to manually create a new transaction.
/// </summary>
/// <remarks>
/// Used for transactions entered by hand rather than imported from a bank file.
/// <see cref="Amount"/> is the absolute value; direction is set via <see cref="IsDebit"/>.
/// </remarks>
public class CreateTransactionRequest
{
    /// <summary>
    /// Gets or sets the identifier of the account the transaction belongs to.
    /// </summary>
    public int AccountId { get; set; }

    /// <summary>
    /// Gets or sets the date the transaction occurred.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the human-readable description.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Gets or sets the absolute monetary amount (always positive).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the transaction is a debit (expense).
    /// </summary>
    public bool IsDebit { get; set; }

    /// <summary>
    /// Gets or sets the optional category identifier.
    /// </summary>
    public int? CategoryId { get; set; }
}

/// <summary>
/// Represents a request to update an existing transaction's properties.
/// </summary>
/// <remarks>
/// Only non-null fields will be applied during the update.
/// This allows partial updates where only specified fields are changed.
/// </remarks>
public class UpdateTransactionRequest
{
    /// <summary>
    /// Gets or sets the updated description for the transaction.
    /// Null if the description should not be changed.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the updated category identifier for the transaction.
    /// Null if the category should not be changed.
    /// </summary>
    public int? CategoryId { get; set; }
}
