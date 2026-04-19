namespace MoneyManager.Services;

/// <summary>
/// Provides core data access and business logic operations for MoneyManager application.
/// </summary>
/// <remarks>
/// This is a partial class split across multiple files for organization:
/// <list type="bullet">
/// <item><description><see cref="DataService.cs"/> - Core service and static caching initialization</description></item>
/// <item><description><see cref="DataService.Account.cs"/> - Account CRUD operations</description></item>
/// <item><description><see cref="DataService.Transaction.cs"/> - Transaction management</description></item>
/// <item><description><see cref="DataService.Category.cs"/> - Category management</description></item>
/// <item><description><see cref="DataService.Rule.cs"/> - Rule management</description></item>
/// <item><description><see cref="DataService.Chart.cs"/> - Chart data aggregation</description></item>
/// <item><description><see cref="DataService.AI.cs"/> - AI data preparation</description></item>
/// </list>
/// Uses <see cref="IDbContextFactory{TContext}"/> for thread-safe context creation.
/// Implements static caching for Accounts and Categories for performance.
/// </remarks>
public partial class DataService(IDbContextFactory<DataContext> contextFactory)
{
    /// <summary>
    /// Initializes static storage with Accounts and Categories from the database.
    /// </summary>
    /// <remarks>
    /// Loads reference data (Accounts and Categories) into memory for fast access.
    /// Should be called once at application startup.
    /// Uses <see cref="HashSet{T}"/> for O(1) lookups.
    /// Only loads if collections are empty to prevent unnecessary database queries.
    /// </remarks>
    public async Task InitStaticStorage()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        if (Accounts.Count == 0)
            Accounts = (await ctx.Accounts.ToListAsync()).ToHashSet();
        if (Categories.Count == 0)
            Categories = (await ctx.Categories.ToListAsync()).ToHashSet();
    }

    /// <summary>
    /// Gets or sets the default chart period for net income visualization.
    /// </summary>
    /// <value>
    /// A string representing the default chart period. Defaults to "12" (last 12 months).
    /// </value>
    /// <remarks>
    /// Supported period codes:
    /// <list type="bullet">
    /// <item><description>m1 - This month</description></item>
    /// <item><description>y1 - This year</description></item>
    /// <item><description>12 - Last 12 months (default)</description></item>
    /// <item><description>w - Last 7 days</description></item>
    /// <item><description>a - All time</description></item>
    /// </list>
    /// This static property persists across the application lifecycle.
    /// </remarks>
    public static string NetIncomeChartPeriod { get; set; } = "12";
}
