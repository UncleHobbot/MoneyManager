namespace MoneyManager.Api.Model.Query;

/// <summary>
/// Filters applied to the listable-transaction read path. All read surfaces
/// (page, stats, future aggregates) accept the same filter shape so that
/// "transactions matching X" means the same thing across callers.
/// </summary>
/// <remarks>
/// Date semantics follow the existing endpoint convention: <see cref="StartDate"/>
/// is inclusive, <see cref="EndDate"/> is exclusive. The caller (endpoint) is
/// responsible for translating period codes ("12", "y1", ...) into a concrete
/// date window before constructing this record; see ADR-0001 / Candidate 1
/// (ChartPeriod value object) for the planned consolidation of period codes.
/// </remarks>
public sealed record TransactionFilters(
    DateTime StartDate,
    DateTime EndDate,
    int? AccountId = null,
    int? CategoryId = null,
    string? Search = null,
    bool Uncategorized = false);

/// <summary>
/// Sort specification for transaction queries. <see cref="Field"/> is matched
/// case-insensitively against a small fixed vocabulary ("date", "amount",
/// "description"); unknown values fall back to the date sort. Kept as a string
/// (not an enum) so endpoint bindings can pass through URL query parameters
/// without a separate validation layer.
/// </summary>
public sealed record TransactionSort(
    string Field = "date",
    SortDirection Direction = SortDirection.Descending);

/// <summary>
/// Direction of a sort operation.
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending,
}

/// <summary>
/// Aggregate statistics over a filtered set of listable transactions. Returned
/// by <c>GetStatsAsync</c>. <see cref="Net"/> is derived from
/// <see cref="Income"/> and <see cref="Expenses"/> so callers cannot accidentally
/// re-spell the formula.
/// </summary>
public sealed record TransactionStats(decimal Income, decimal Expenses, int Count)
{
    public decimal Net => Income - Expenses;
}
