namespace MoneyManager.Api.Model.Chart;

/// <summary>
/// Represents cumulative spending data for day-by-day comparison between months.
/// </summary>
/// <remarks>
/// This class is used to compare spending patterns between:
/// - Last month (previous calendar month)
/// - Current month (this calendar month up to today)
/// 
/// The chart shows spending velocity:
/// - How quickly spending accumulated in the current month vs last month
/// - Whether the current month is ahead or behind the previous month's pace
/// - Day-by-day cumulative totals for both months
/// 
/// This helps users:
/// - Track their spending pace
/// - Identify if they're spending faster or slower than last month
/// - See which days had the most spending
/// 
/// The data is calculated from transactions up to the current day.
/// Days beyond the current month have no spending data (null values).
/// </remarks>
public class CumulativeSpendingChart
{
    /// <summary>
    /// Gets or sets the day number in the month (1-31).
    /// </summary>
    /// <value>
    /// An integer from 1 to 31 representing the day of the month.
    /// </value>
    /// <remarks>
    /// Used for x-axis labeling in the cumulative spending chart.
    /// Allows displaying daily progress through the month.
    /// 
    /// Days beyond the actual month length (e.g., day 31 in February) will have null values.
    /// </remarks>
    public int DayNumber { get; set; }

    /// <summary>
    /// Gets or sets the cumulative spending up to this day in the last month.
    /// </summary>
    /// <value>
    /// The total amount spent from day 1 through this day in the last month.
    /// Null if the day doesn't exist in that month (e.g., February 30th).
    /// </value>
    /// <remarks>
    /// This represents last month's spending pattern:
    /// - Cumulative total increases each day
    /// - Shows running total for last month
    /// - Used for comparison with current month
    /// 
    /// The calculation includes all expense transactions up to this day.
    /// Used to show where last month was at this point in time.
    /// </remarks>
    public decimal? LastMonthExpenses { get; set; }

    /// <summary>
    /// Gets or sets the cumulative spending up to this day in the current month.
    /// </summary>
    /// <value>
    /// The total amount spent from day 1 through today in the current month.
    /// Null if today hasn't occurred yet for this day.
    /// </value>
    /// <remarks>
    /// This represents current month's spending pattern:
    /// - Cumulative total increases each day
    /// - Shows running total for current month
    /// - Used for comparison with last month
    /// - Stops at the current day (today)
    /// 
    /// The calculation includes all expense transactions up to today.
    /// Future days (after today) will be null.
    /// </remarks>
    public decimal? ThisMonthExpenses { get; set; }
}
