namespace MoneyManager.Api.Model.Query;

/// <summary>
/// Paging specification for paginated reads. Intentionally generic
/// (no <c>Transaction</c> prefix) so other resources can reuse it without
/// renaming.
/// </summary>
public sealed record Paging(int Page = 1, int Size = 50);

/// <summary>
/// A page of results from a paginated query. Carries the items plus the
/// total count (before paging was applied) so callers can render
/// "showing X-Y of Z" without a second round-trip.
/// </summary>
/// <remarks>
/// The page-number property is named <see cref="PageNumber"/> (not <c>Page</c>)
/// because C# disallows a member name matching the enclosing type.
/// Endpoints that need to preserve the existing wire shape can rename the
/// JSON property via <c>[JsonPropertyName("page")]</c> when migrating.
/// </remarks>
public sealed record Page<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

