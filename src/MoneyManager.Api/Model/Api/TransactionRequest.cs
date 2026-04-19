namespace MoneyManager.Api.Model.Api;

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
