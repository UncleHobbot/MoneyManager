using MoneyManager.Api.Data;

namespace MoneyManager.Api.Model.Chart;

/// <summary>
/// Represents chart data for spending breakdown by category.
/// </summary>
/// <remarks>
/// This class is used to display category-level spending analysis:
/// - Shows how much was spent in each category
/// - Groups spending by category for visualization
/// - Used by category charts and pie charts
/// 
/// The data is aggregated from transactions by category:
/// - Category: The category entity (with name, icon, parent)
/// - Amount: Sum of all transactions in that category
/// - Excludes transfers and income transactions
/// 
/// Categories can be top-level or subcategories.
/// The chart may show all categories or filter to specific ones.
/// </remarks>
public class CategoryChart
{
    /// <summary>
    /// Gets or sets the category entity associated with this spending.
    /// </summary>
    /// <value>
    /// The Category object containing category information.
    /// Includes category name, icon, parent category, and other properties.
    /// </value>
    /// <remarks>
    /// Links spending data to category details:
    /// - Category name for display
    /// - Icon for visual identification
    /// - Parent category for hierarchical grouping
    /// - Category metadata
    /// 
    /// The category may be a subcategory (has a parent)
    /// or a top-level category (no parent)
    /// This allows charts to group by top-level categories or show subcategories.
    /// </remarks>
    public Category Category { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total amount spent in this category.
    /// </summary>
    /// <value>
    /// The sum of all transaction amounts in this category.
    /// Always positive value representing total spending.
    /// </value>
    /// <remarks>
    /// Amount represents the total spending for the category:
    /// - Includes all transactions in the category
    /// - Calculated as sum of amounts
    /// - Shown as absolute value in charts
    /// 
    /// The amount excludes:
    /// - Income transactions (credits)
    /// - Transfer transactions
    /// - Transactions in other filtered-out categories
    /// 
    /// Used to determine spending by category.
    /// Categories with higher amounts are biggest expenses.
    /// </remarks>
    public decimal Amount { get; set; }
}
