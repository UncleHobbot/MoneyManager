using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides core data access and business logic operations for MoneyManager application.
/// </summary>
/// <remarks>
/// This is a partial class split across multiple files for organization:
/// <list type="bullet">
/// <item><description><see cref="DataService"/> - Core service and cache initialization</description></item>
/// <item><description>DataService.Account.cs - Account CRUD operations</description></item>
/// <item><description>DataService.Category.cs - Category management</description></item>
/// </list>
/// Uses <see cref="IMemoryCache"/> for thread-safe account/category caching in the web environment.
/// Uses <see cref="IDbContextFactory{TContext}"/> for creating per-operation database contexts.
/// Chart aggregation lives in <see cref="ChartService"/> (see CONTEXT.md "Chart aggregation").
/// </remarks>
public partial class DataService(
    IDbContextFactory<DataContext> contextFactory,
    ReferenceDataCache cache)
{
    /// <summary>
    /// Gets or sets the default chart period for net income visualization. Defaults to "12" (last 12 months).
    /// </summary>
    /// <value>
    /// A string representing the default chart period.
    /// Supported period codes: m1 (this month), y1 (this year), 12 (last 12 months), w (last 7 days), a (all time).
    /// </value>
    public static string NetIncomeChartPeriod { get; set; } = "12";
}
