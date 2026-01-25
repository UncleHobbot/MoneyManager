namespace MoneyManager.Model.Chart;

/// <summary>
/// Represents chart data for income and expenses aggregated by month.
/// </summary>
/// <remarks>
/// This class is used to display monthly financial summaries:
/// - Shows income vs expenses for each month
/// - Calculates net balance
/// - Used by income/expense charts to visualize trends
/// 
/// The data is typically aggregated by month from transactions:
/// - Income: Money received (positive amounts)
/// - Expenses: Money spent (positive amounts)
/// - Balance: Income - Expenses (can be positive or negative)
/// 
/// This is a flat data model suitable for serialization and chart display.
/// </remarks>
public class BalanceChart
{
    /// <summary>
    /// Gets or sets the month label for grouping (e.g., "Jan", "Feb").
    /// </summary>
    /// <value>
    /// The abbreviated month name derived from the transaction date.
    /// Common formats: "Jan", "Feb", "Mar", etc.
    /// </value>
    /// <remarks>
    /// Used for x-axis grouping in charts.
    /// The format is short and consistent for display purposes.
    /// Multiple months with the same label will be grouped together in the chart.
    /// </remarks>
    public string Month { get; set; } = null!;

    /// <summary>
    /// Gets or sets the first date (earliest) in the month.
    /// </summary>
    /// <value>
    /// The DateTime representing the earliest transaction date in this month.
    /// Used for sorting and chronological ordering.
    /// </value>
    /// <remarks>
    /// This helps sort the months chronologically in charts.
    /// It's typically the first day of the month (e.g., January 1st).
    /// Used for proper time series ordering in visualizations.
    /// </remarks>
    public DateTime FirstDate { get; set; }

    /// <summary>
    /// Gets or sets the full month name for display (e.g., "January 2025").
    /// </summary>
    /// <value>
    /// A user-friendly month label with year.
    /// Example: "January 2025", "February 2025".
    /// </value>
    /// <remarks>
    /// This is the primary label shown to users in the chart.
    /// More descriptive than the short Month property.
    /// Includes the year to distinguish months across different years.
    /// </remarks>
    public string MonthLabel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the month key for sorting (format: "yyMM").
    /// </summary>
    /// <value>
    /// A string in "yyMM" format for programmatic sorting.
    /// Example: "2501" for January 2025.
    /// </value>
    /// <remarks>
    /// Used for sorting and programmatic month comparisons.
    /// The format ensures consistent string comparison.
    /// Year-month format enables proper chronological sorting.
    /// 
    /// Format breakdown:
    /// - yy: Last two digits of year
    /// - MM: Month number (01-12, zero-padded)
    /// </remarks>
    public string MonthKey { get; set; } = null!;

    /// <summary>
    /// Gets or sets the total income for the month.
    /// </summary>
    /// <value>
    /// The sum of all positive transactions (money received).
    /// </value>
    /// <remarks>
    /// Income includes:
    /// - Salary deposits
    /// - Investment returns
    /// - Other money received
    /// 
    /// Income is calculated as positive values in chart display.
    /// In the database, debits subtract and credits add.
    /// For the chart, credits are shown as positive income amounts.
    /// </remarks>
    public decimal Income { get; set; }

    /// <summary>
    /// Gets or sets the total expenses for the month.
    /// </summary>
    /// <value>
    /// The sum of all expense transactions (money spent).
    /// </value>
    /// <remarks>
    /// Expenses include:
    /// - Purchases
    /// - Bills
    /// - Services
    /// - Other money spent
    /// 
    /// Expenses are calculated as positive values in chart display.
    /// In the database, debits subtract and credits add.
    /// For the chart, debits are shown as positive expense amounts.
    /// 
    /// Excludes transfers which are filtered at the query level.
    /// </remarks>
    public decimal Expenses { get; set; }

    /// <summary>
    /// Gets or sets the calculated net balance (income minus expenses).
    /// </summary>
    /// <value>
    /// The net financial position: Income - Expenses.
    /// Positive values indicate net gain for the month.
    /// Negative values indicate net loss for the month.
    /// </value>
    /// <remarks>
    /// This is a computed property, not stored in the database.
    /// Calculated as: Income - Expenses
    /// 
    /// Net Balance Analysis:
    /// - Positive: More income than expenses (savings)
    /// - Negative: More expenses than income (deficit)
    /// - Zero: Income equals expenses (break-even)
    /// 
    /// This metric shows the month's overall financial health.
    /// Used in charts to display whether users are in the positive or negative.
    /// </remarks>
    public decimal Balance => Income + Expenses;
}
