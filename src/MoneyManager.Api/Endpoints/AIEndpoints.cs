using MoneyManager.Api.Data;
using MoneyManager.Api.Model.AI;
using MoneyManager.Api.Model.Api;
using MoneyManager.Api.Services;

namespace MoneyManager.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for AI-powered financial analysis and AI provider management.
/// </summary>
public static class AIEndpoints
{
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
        if (AnalysisType.Find(request.AnalysisType) is null)
            return TypedResults.BadRequest(new { error = $"Unknown analysis type: {request.AnalysisType}" });

        var result = await aiService.GetAnalysisAsync(
            request.Period,
            request.AnalysisType,
            request.ProviderId);

        return TypedResults.Ok(result);
    }

    internal static IResult GetAnalysisTypes()
    {
        // Project the catalog to the wire format the frontend expects.
        // Fields (type, name, group, description) are kept on AnalysisType
        // so this endpoint and the prompt-resolution path share one source
        // of truth. Prompt and Temperature are intentionally not exposed.
        return TypedResults.Ok(AnalysisType.All.Select(a => new
        {
            type = a.Key,
            name = a.Name,
            group = a.Group,
            description = a.Description,
        }));
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
