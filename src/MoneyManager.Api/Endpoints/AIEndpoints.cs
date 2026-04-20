using MoneyManager.Api.Data;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for AI-powered financial analysis and AI provider management.
/// </summary>
public static class AIEndpoints
{
    private static readonly object[] AnalysisTypes =
    [
        new { type = AnalysisTypePrompts.SpendingGeneral,  name = "General Spending",  group = "Spending Analysis",        description = "Comprehensive overview of spending habits, top categories, outliers, and daily/weekly averages." },
        new { type = AnalysisTypePrompts.SpendingBudget,   name = "Budget Analysis",   group = "Spending Analysis",        description = "Compares actual spending to budgeting methods (50/30/20, zero-based, envelope) with recommendations." },
        new { type = AnalysisTypePrompts.SpendingTrends,   name = "Spending Trends",   group = "Spending Analysis",        description = "Month-over-month and year-over-year comparisons with inflation-adjusted real spending changes." },
        new { type = AnalysisTypePrompts.DebtAnalysis,         name = "Debt Analysis",         group = "Debt & Savings", description = "Evaluates debt situation and recommends avalanche vs snowball payoff strategies with timelines." },
        new { type = AnalysisTypePrompts.SavingsEmergencyFund, name = "Savings & Emergency Fund", group = "Debt & Savings", description = "Evaluates emergency fund position, savings rate, and strategies to reach 3\u20136 months of expenses." },
        new { type = AnalysisTypePrompts.CashFlowForecast, name = "Cash Flow Forecast",  group = "Planning & Forecasting", description = "Predicts cash flow over the next 6 months, identifying potential shortfalls and surplus periods." },
        new { type = AnalysisTypePrompts.GoalBasedPlanning, name = "Goal-Based Planning", group = "Planning & Forecasting", description = "Creates savings plans for specific goals (car, house, vacation) with timelines and account allocation." },
        new { type = AnalysisTypePrompts.RecurringIncome,   name = "Recurring Income",    group = "Planning & Forecasting", description = "Analyzes income patterns, sources, and irregularities for better forecasting." },
        new { type = AnalysisTypePrompts.BehavioralInsights,        name = "Behavioral Insights",        group = "Behavior & Optimization", description = "Analyzes weekend/weekday patterns, trigger events, and psychological spending behaviors." },
        new { type = AnalysisTypePrompts.SubscriptionsOptimization, name = "Subscriptions Optimization", group = "Behavior & Optimization", description = "Identifies all subscriptions, evaluates usage value, and suggests cancellations or downgrades." },
        new { type = AnalysisTypePrompts.AnomalyDetection,          name = "Anomaly Detection",          group = "Behavior & Optimization", description = "Identifies unusual, duplicate, or suspicious transactions and spending spikes." },
        new { type = AnalysisTypePrompts.TaxEfficiency,      name = "Tax Efficiency",      group = "Canadian-Specific", description = "Analyzes Canadian tax-deductible expenses and registered account (RRSP, TFSA, FHSA) recommendations." },
        new { type = AnalysisTypePrompts.RegisteredAccounts, name = "Registered Accounts", group = "Canadian-Specific", description = "RRSP vs TFSA vs FHSA optimization, contribution room analysis, and allocation strategy." },
        new { type = AnalysisTypePrompts.SeasonalAnalysis,   name = "Seasonal Analysis",   group = "Canadian-Specific", description = "Identifies seasonal spending patterns, holiday impacts, and weather-related expense fluctuations." }
    ];

    /// <summary>
    /// Maps all AI-related endpoints under <c>/api/ai</c>.
    /// </summary>
    public static void MapAIEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/ai").WithTags("AI");

        group.MapGet("/providers", GetProviders);
        group.MapGet("/providers/{id:int}", GetProvider);
        group.MapPost("/providers", CreateProvider);
        group.MapPut("/providers/{id:int}", UpdateProvider);
        group.MapDelete("/providers/{id:int}", DeleteProvider);
        group.MapPost("/analyze", Analyze);
        group.MapGet("/analysis-types", GetAnalysisTypes);
    }

    internal static async Task<IResult> GetProviders(AiProviderService aiProviderService)
    {
        var providers = await aiProviderService.GetProvidersAsync();
        return TypedResults.Ok(providers);
    }

    internal static async Task<IResult> GetProvider(int id, AiProviderService aiProviderService)
    {
        var provider = await aiProviderService.GetProviderByIdAsync(id);
        return provider is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(provider);
    }

    internal static async Task<IResult> CreateProvider(
        AiProviderRequest request,
        AiProviderService aiProviderService)
    {
        var provider = MapToEntity(request);
        var created = await aiProviderService.AddProviderAsync(provider);
        return TypedResults.Created($"/api/ai/providers/{created.Id}", created);
    }

    internal static async Task<IResult> UpdateProvider(
        int id,
        AiProviderRequest request,
        AiProviderService aiProviderService)
    {
        var provider = MapToEntity(request);
        provider.Id = id;

        var updated = await aiProviderService.UpdateProviderAsync(provider);
        return updated is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(updated);
    }

    internal static async Task<IResult> DeleteProvider(int id, AiProviderService aiProviderService)
    {
        var deleted = await aiProviderService.DeleteProviderAsync(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    internal static async Task<IResult> Analyze(AnalysisRequest request, AIService aiService)
    {
        if (string.IsNullOrWhiteSpace(AnalysisTypePrompts.GetPrompt(request.AnalysisType)))
            return TypedResults.BadRequest(new { error = $"Unknown analysis type: {request.AnalysisType}" });

        var result = await aiService.GetAnalysisAsync(
            request.Period,
            request.AnalysisType,
            request.ProviderId);

        return TypedResults.Ok(result);
    }

    internal static IResult GetAnalysisTypes()
    {
        return TypedResults.Ok(AnalysisTypes);
    }

    private static AiProvider MapToEntity(AiProviderRequest request) => new()
    {
        Name = request.Name,
        ProviderType = request.ProviderType,
        EncryptedApiKey = request.ApiKey,
        ApiUrl = request.ApiUrl,
        Model = request.Model,
        IsDefault = request.IsDefault
    };
}
