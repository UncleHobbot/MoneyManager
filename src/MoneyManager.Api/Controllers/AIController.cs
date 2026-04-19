using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Controllers;

/// <summary>
/// API controller for AI-powered financial analysis and AI provider management.
/// </summary>
/// <remarks>
/// Provides endpoints for:
/// <list type="bullet">
///   <item><description>Running AI financial analyses (spending, debt, savings, forecasting, etc.)</description></item>
///   <item><description>CRUD operations on AI provider configurations (OpenAI, Azure, custom endpoints)</description></item>
///   <item><description>Listing available analysis types with descriptions and groupings</description></item>
/// </list>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AIController(AIService aiService, AiProviderService aiProviderService) : ControllerBase
{
    // ── Providers ────────────────────────────────────────────────

    /// <summary>
    /// Gets all configured AI providers.
    /// </summary>
    /// <returns>A list of all <see cref="AiProvider"/> configurations ordered by name.</returns>
    /// <response code="200">Returns the list of providers.</response>
    [HttpGet("providers")]
    public async Task<ActionResult<List<AiProvider>>> GetProviders()
    {
        var providers = await aiProviderService.GetProvidersAsync();
        return Ok(providers);
    }

    /// <summary>
    /// Gets a single AI provider by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the AI provider.</param>
    /// <returns>The matching <see cref="AiProvider"/>, or 404 if not found.</returns>
    /// <response code="200">Returns the provider.</response>
    /// <response code="404">Provider with the specified ID was not found.</response>
    [HttpGet("providers/{id:int}")]
    public async Task<ActionResult<AiProvider>> GetProvider(int id)
    {
        var provider = await aiProviderService.GetProviderByIdAsync(id);
        if (provider is null)
            return NotFound();

        return Ok(provider);
    }

    /// <summary>
    /// Creates a new AI provider configuration.
    /// </summary>
    /// <param name="request">The provider details including name, type, API key, URL, and model.</param>
    /// <returns>The newly created <see cref="AiProvider"/>.</returns>
    /// <response code="201">Provider was created successfully.</response>
    [HttpPost("providers")]
    public async Task<ActionResult<AiProvider>> CreateProvider(AiProviderRequest request)
    {
        var provider = MapToEntity(request);
        var created = await aiProviderService.AddProviderAsync(provider);
        return CreatedAtAction(nameof(GetProvider), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing AI provider configuration.
    /// </summary>
    /// <param name="id">The unique identifier of the provider to update.</param>
    /// <param name="request">The updated provider details.</param>
    /// <returns>The updated <see cref="AiProvider"/>, or 404 if not found.</returns>
    /// <response code="200">Provider was updated successfully.</response>
    /// <response code="404">Provider with the specified ID was not found.</response>
    [HttpPut("providers/{id:int}")]
    public async Task<ActionResult<AiProvider>> UpdateProvider(int id, AiProviderRequest request)
    {
        var provider = MapToEntity(request);
        provider.Id = id;

        var updated = await aiProviderService.UpdateProviderAsync(provider);
        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    /// <summary>
    /// Deletes an AI provider configuration.
    /// </summary>
    /// <param name="id">The unique identifier of the provider to delete.</param>
    /// <returns>No content on success, or 404 if not found.</returns>
    /// <response code="204">Provider was deleted successfully.</response>
    /// <response code="404">Provider with the specified ID was not found.</response>
    [HttpDelete("providers/{id:int}")]
    public async Task<IActionResult> DeleteProvider(int id)
    {
        var deleted = await aiProviderService.DeleteProviderAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // ── Analysis ─────────────────────────────────────────────────

    /// <summary>
    /// Runs an AI-powered financial analysis for the specified period and analysis type.
    /// </summary>
    /// <param name="request">
    /// The analysis parameters including period, analysis type, and optional provider ID.
    /// </param>
    /// <returns>The analysis result containing the AI-generated content or an error message.</returns>
    /// <response code="200">Analysis completed (check <c>IsSuccess</c> for outcome).</response>
    /// <response code="400">The requested analysis type is not recognized.</response>
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(AnalysisRequest request)
    {
        // Validate the analysis type is known
        if (string.IsNullOrWhiteSpace(AnalysisTypePrompts.GetPrompt(request.AnalysisType)))
            return BadRequest(new { error = $"Unknown analysis type: {request.AnalysisType}" });

        var result = await aiService.GetAnalysisAsync(
            request.Period,
            request.AnalysisType,
            request.ProviderId);

        return Ok(result);
    }

    /// <summary>
    /// Returns the list of all supported AI analysis types with display names, groups, and descriptions.
    /// </summary>
    /// <returns>
    /// An array of objects containing <c>type</c>, <c>name</c>, <c>group</c>, and <c>description</c>
    /// for each of the 14 available analysis types.
    /// </returns>
    /// <response code="200">Returns the analysis type catalog.</response>
    [HttpGet("analysis-types")]
    public IActionResult GetAnalysisTypes()
    {
        return Ok(AnalysisTypes);
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Maps an <see cref="AiProviderRequest"/> to an <see cref="AiProvider"/> entity.
    /// </summary>
    /// <param name="request">The incoming request DTO.</param>
    /// <returns>A new <see cref="AiProvider"/> populated from the request.</returns>
    private static AiProvider MapToEntity(AiProviderRequest request) => new()
    {
        Name = request.Name,
        ProviderType = request.ProviderType,
        EncryptedApiKey = request.ApiKey, // encryption handled by the service / middleware layer
        ApiUrl = request.ApiUrl,
        Model = request.Model,
        IsDefault = request.IsDefault
    };

    /// <summary>
    /// Static catalog of all supported analysis types grouped into functional categories.
    /// </summary>
    private static readonly object[] AnalysisTypes =
    [
        // ── Spending Analysis ──
        new { type = AnalysisTypePrompts.SpendingGeneral,  name = "General Spending",  group = "Spending Analysis",        description = "Comprehensive overview of spending habits, top categories, outliers, and daily/weekly averages." },
        new { type = AnalysisTypePrompts.SpendingBudget,   name = "Budget Analysis",   group = "Spending Analysis",        description = "Compares actual spending to budgeting methods (50/30/20, zero-based, envelope) with recommendations." },
        new { type = AnalysisTypePrompts.SpendingTrends,   name = "Spending Trends",   group = "Spending Analysis",        description = "Month-over-month and year-over-year comparisons with inflation-adjusted real spending changes." },

        // ── Debt & Savings ──
        new { type = AnalysisTypePrompts.DebtAnalysis,         name = "Debt Analysis",         group = "Debt & Savings", description = "Evaluates debt situation and recommends avalanche vs snowball payoff strategies with timelines." },
        new { type = AnalysisTypePrompts.SavingsEmergencyFund, name = "Savings & Emergency Fund", group = "Debt & Savings", description = "Evaluates emergency fund position, savings rate, and strategies to reach 3–6 months of expenses." },

        // ── Planning & Forecasting ──
        new { type = AnalysisTypePrompts.CashFlowForecast, name = "Cash Flow Forecast",  group = "Planning & Forecasting", description = "Predicts cash flow over the next 6 months, identifying potential shortfalls and surplus periods." },
        new { type = AnalysisTypePrompts.GoalBasedPlanning, name = "Goal-Based Planning", group = "Planning & Forecasting", description = "Creates savings plans for specific goals (car, house, vacation) with timelines and account allocation." },
        new { type = AnalysisTypePrompts.RecurringIncome,   name = "Recurring Income",    group = "Planning & Forecasting", description = "Analyzes income patterns, sources, and irregularities for better forecasting." },

        // ── Behavior & Optimization ──
        new { type = AnalysisTypePrompts.BehavioralInsights,        name = "Behavioral Insights",        group = "Behavior & Optimization", description = "Analyzes weekend/weekday patterns, trigger events, and psychological spending behaviors." },
        new { type = AnalysisTypePrompts.SubscriptionsOptimization, name = "Subscriptions Optimization", group = "Behavior & Optimization", description = "Identifies all subscriptions, evaluates usage value, and suggests cancellations or downgrades." },
        new { type = AnalysisTypePrompts.AnomalyDetection,          name = "Anomaly Detection",          group = "Behavior & Optimization", description = "Identifies unusual, duplicate, or suspicious transactions and spending spikes." },

        // ── Canadian-Specific ──
        new { type = AnalysisTypePrompts.TaxEfficiency,      name = "Tax Efficiency",      group = "Canadian-Specific", description = "Analyzes Canadian tax-deductible expenses and registered account (RRSP, TFSA, FHSA) recommendations." },
        new { type = AnalysisTypePrompts.RegisteredAccounts, name = "Registered Accounts", group = "Canadian-Specific", description = "RRSP vs TFSA vs FHSA optimization, contribution room analysis, and allocation strategy." },
        new { type = AnalysisTypePrompts.SeasonalAnalysis,   name = "Seasonal Analysis",   group = "Canadian-Specific", description = "Identifies seasonal spending patterns, holiday impacts, and weather-related expense fluctuations." }
    ];
}
