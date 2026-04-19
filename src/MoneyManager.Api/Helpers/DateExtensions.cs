using System.Globalization;

namespace MoneyManager.Api.Helpers;

/// <summary>
/// Provides date-related extension methods to replace FluentUI date helpers.
/// </summary>
public static class DateExtensions
{
    /// <summary>
    /// Returns the first day of the month for the given date.
    /// </summary>
    /// <param name="date">The date to get the start of month for.</param>
    /// <param name="culture">The culture info (unused, kept for API compatibility).</param>
    /// <returns>A DateTime representing the first day of the month.</returns>
    public static DateTime StartOfMonth(this DateTime date, CultureInfo culture)
    {
        return new DateTime(date.Year, date.Month, 1);
    }

    /// <summary>
    /// Returns the first day of the month for the given date.
    /// </summary>
    /// <param name="date">The date to get the start of month for.</param>
    /// <returns>A DateTime representing the first day of the month.</returns>
    public static DateTime StartOfMonth(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, 1);
    }
}
