namespace MoneyManager.Model;

/// <summary>
/// Defines the display mode options for the transaction list view.
/// </summary>
public enum TransactionListModeEnum
{
    /// <summary>
    /// Full view mode showing all transaction details including date, description, amount, category, and notes.
    /// </summary>
    Full,

    /// <summary>
    /// Short/compact view mode showing only essential transaction information (date, description, amount).
    /// </summary>
    Short
}
