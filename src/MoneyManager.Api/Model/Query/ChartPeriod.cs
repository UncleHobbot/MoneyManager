using System.Globalization;
using MoneyManager.Api.Helpers;

namespace MoneyManager.Api.Model.Query;

/// <summary>
/// A chart period: a code, a user-facing label, and a function that maps a
/// reference date to a (Start, End) window. Replaces the 60-line switch in
/// the legacy <c>DataService.GetDates</c> with a single declarative list.
/// See <c>CONTEXT.md</c> ("ChartPeriod") and Candidate 1 grilling.
/// </summary>
/// <remarks>
/// <para>
/// The reference-date parameter makes the date math testable: tests pass a
/// deterministic <see cref="DateTime"/> instead of relying on
/// <see cref="DateTime.Today"/>. Callers in production pass
/// <c>DateTime.Today</c>.
/// </para>
/// <para>
/// <see cref="All"/> is the single source of truth for the period vocabulary.
/// The <c>/api/charts/periods</c> endpoint enumerates it directly; backend
/// callers look up via <see cref="Find"/>; frontend fetches the list. Adding
/// a new period = adding one entry to <see cref="All"/>.
/// </para>
/// <para>
/// Unknown codes silently fall back to <see cref="Default"/> (the "y1"
/// period). This preserves pre-migration behavior; tightening to throw is
/// a separate decision that requires auditing frontend usage.
/// </para>
/// </remarks>
public sealed record ChartPeriod(
    string Code,
    string Label,
    Func<DateTime, (DateTime Start, DateTime End)> GetDateRange)
{
    /// <summary>
    /// All known chart periods, in the order the <c>/api/charts/periods</c>
    /// endpoint exposes them. To add a period, append one entry.
    /// </summary>
    public static readonly IReadOnlyList<ChartPeriod> All = new[]
    {
        new ChartPeriod("12", "Last 12 Months",
            t => (t.AddMonths(-12).StartOfMonth(), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("y1", "This Year",
            t => (new DateTime(t.Year, 1, 1), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("y2", "Last Year",
            t => (new DateTime(t.Year - 1, 1, 1), new DateTime(t.Year, 1, 1))),

        new ChartPeriod("y3", "2 Years Ago",
            t => (new DateTime(t.Year - 2, 1, 1), new DateTime(t.Year - 1, 1, 1))),

        new ChartPeriod("y12", "Last + This Year",
            t => (new DateTime(t.Year - 1, 1, 1), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("m1", "This Month",
            t => (new DateTime(t.Year, t.Month, 1), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("m2", "Last Month",
            t => (new DateTime(t.Year, t.Month, 1).AddMonths(-1), new DateTime(t.Year, t.Month, 1))),

        new ChartPeriod("m1+2", "Last 2 Months",
            t => (new DateTime(t.Year, t.Month, 1).AddMonths(-1), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("m1+3", "Last 3 Months",
            t => (new DateTime(t.Year, t.Month, 1).AddMonths(-2), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("w", "Last 7 Days",
            t => (t.AddDays(-7), t.AddMonths(1).StartOfMonth())),

        // "w1" is an alias for "w". Pre-migration, GetDates accepted both;
        // ChartEndpoints.Periods array only listed "w". After consolidation,
        // both appear in /api/charts/periods — minor behavior change, the
        // frontend dropdown will show one extra item.
        new ChartPeriod("w1", "Last 7 Days",
            t => (t.AddDays(-7), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("w2", "Last 14 Days",
            t => (t.AddDays(-14), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("w3", "Last 31 Days",
            t => (t.AddDays(-31), t.AddMonths(1).StartOfMonth())),

        new ChartPeriod("a", "All Time",
            t => (DateTime.MinValue, t.AddMonths(1).StartOfMonth())),
    };

    /// <summary>
    /// The fallback period used when <see cref="Find"/> returns null.
    /// Equivalent to the "y1" period (this year so far), which matches the
    /// pre-migration default initialization in <c>DataService.GetDates</c>.
    /// </summary>
    public static ChartPeriod Default => All.Single(p => p.Code == "y1");

    /// <summary>
    /// Looks up a <see cref="ChartPeriod"/> by its code. Returns null for
    /// unknown codes, null, or empty strings. Callers that want silent
    /// fallback should use <c>?? ChartPeriod.Default</c>.
    /// </summary>
    public static ChartPeriod? Find(string? code) =>
        string.IsNullOrEmpty(code) ? null : All.FirstOrDefault(p => p.Code == code);
}
