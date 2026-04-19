namespace MoneyManager.Api.Model.Api;

/// <summary>
/// Represents a request to perform an AI-powered financial analysis.
/// </summary>
/// <remarks>
/// Specifies the time period, analysis type, and optional AI provider
/// to use for generating financial insights.
/// </remarks>
public class AnalysisRequest
{
    /// <summary>
    /// Gets or sets the time period for the analysis.
    /// Defaults to "12" (last 12 months).
    /// </summary>
    /// <value>
    /// Common values: "m1" (month), "y1" (year), "12" (last 12 months), "w" (7 days), "a" (all).
    /// </value>
    public string Period { get; set; } = "12";

    /// <summary>
    /// Gets or sets the type of analysis to perform (e.g., "SpendingGeneral", "SpendingTrends").
    /// </summary>
    public string AnalysisType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the optional AI provider identifier to use.
    /// Null to use the default provider.
    /// </summary>
    public int? ProviderId { get; set; }
}
