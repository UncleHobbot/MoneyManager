namespace MoneyManager.Api.Model.Query;

/// <summary>
/// A category rolled up to its parent for reporting purposes. When a
/// transaction's category has a <c>Parent</c>, this is the <c>Parent</c>;
/// otherwise it is the category itself. Subcategories collapse into their
/// parent for aggregation.
/// </summary>
public sealed record ReportingCategory(int Id, string Name, string? Icon);

/// <summary>
/// A read-side projection of a listable transaction optimized for
/// aggregation. Carries the per-row facts that chart and stats methods need
/// without re-spelling the sign convention, the parent-rollup rule, or the
/// "Income"/"Transfer" name matches at every call site.
/// </summary>
/// <remarks>
/// <see cref="SignedAmount"/> follows the canonical convention from
/// <c>CONTEXT.md</c> ("Signed amount"): positive for income (credits),
/// negative for expenses (debits). Consumers do not re-spell the
/// <c>IsDebit ? -Amount : Amount</c> formula.
/// <see cref="EffectiveCategory"/> is <c>null</c> for transactions with no
/// category; consumers that need a category (e.g. spending-by-category
/// grouping) should filter. Consumers that only need Income/Expense totals
/// (e.g. net-income chart) can ignore the field — <see cref="IsIncome"/>
/// and <see cref="IsTransfer"/> are valid regardless.
/// </remarks>
public sealed record ReportingRow(
    DateTime Date,
    decimal SignedAmount,
    ReportingCategory? EffectiveCategory,
    bool IsIncome,
    bool IsTransfer);
