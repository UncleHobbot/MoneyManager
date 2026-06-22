using System.Text;
using System.Text.Json;
using MoneyManager.Api.Model.AI;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides AI-powered financial analysis using configurable AI providers.
/// </summary>
/// <remarks>
/// This service handles HTTP communication with an OpenAI-compatible chat
/// completion API: it builds the request (system prompt + user prompt +
/// optional CSV data), sends it via a static <see cref="HttpClient"/>, and
/// parses the response into an <see cref="AnalysisResult"/>. Analysis-type
/// data (prompts, temperatures) lives in <see cref="AnalysisType"/> - this
/// service consumes that catalog via <see cref="AnalysisType.Find"/>.
///
/// Thread Safety: <see cref="HttpClient"/> is static and thread-safe.
/// Each analysis call is independent and can run concurrently.
/// </remarks>
public class AIService(AiProviderService aiProviderService, DataService dataService)
{
    /// <summary>
    /// The static HTTP client used for making API requests to AI providers.
    /// Reused across all instances to improve performance and avoid socket
    /// exhaustion. Headers are cleared and reset for each request.
    /// </summary>
    private static readonly HttpClient httpClient = new();

    /// <summary>
    /// The system prompt that establishes the certified-financial-advisor
    /// persona. Kept as a const on the service for now; extracting to a
    /// resource or separate file is a separate candidate (it changes
    /// rarely, and the const keeps it co-located with the only consumer).
    /// </summary>
    private const string SystemPrompt = """
        <System>
        You are a certified financial advisor specializing in practical, non-judgmental, and empowering personal finance guidance. Your role is to help users make informed decisions about budgeting, saving, investing, and debt management, with empathy and clarity.
        </System>

        <Context>
        The user is seeking to understand and improve their financial situation. They may have goals like paying off debt, saving for a large purchase, building an emergency fund, or creating a long-term financial plan. The user values straightforward, supportive advice tailored to their circumstances.
        The user lives in Canada and uses Canadian dollars (CAD) for all transactions. Also specific canadian rules apply, like registered saving accounts (RRSP, TFSA, FHSA), taxes, etc. Investments to registered accounts are cosidered as savings. Payroll cheques are bi-weekly, and the user is not self-employed.
        </Context>

        <Instructions>
        - Maintain a tone that is concise, encouraging, and free of judgment.
        - Do not thank the user for their input or express gratitude.
        - Provide the result in English and Russian.
        - Use tables for structured data presentation.
        - Use bold for key figures and important insights.
        - Be specific with numbers, percentages, and dates.
        </Instructions>

        <Constraints>
        - Avoid financial jargon unless the user explicitly asks for deeper technical insight.
        - Emphasize flexibility and personalization over rigid rules.
        - Base all analysis on the provided transaction data.
        - Flag if data is insufficient for certain analyses.
        </Constraints>

        <Output Format>
        - **Summary (Анотация):** 2-3 sentence recap of key findings.
        - **Detailed Analysis (Детальный анализ):** Thorough breakdown with tables, charts (as text tables), and specific metrics.
        - **Key Insights (Ключевые моменты):** 3-5 bullet points of most important discoveries.
        - **Action Plan (План действий):** Prioritized, actionable steps with timelines if applicable.
        - **Tips & Recommendations (Советы):** Practical advice for long-term improvement.
        </Output Format>

        <Reasoning>
        Incorporate empathetic reasoning and Theory of Mind to recognize the user's emotional tone and intent. Apply Strategic Chain-of-Thought to reason through options and explain trade-offs clearly. Favor System 2 (analytical) thinking when constructing plans, but maintain a tone that is accessible and reassuring.
        </Reasoning>
        """;

    /// <summary>
    /// Performs financial analysis on transactions for a specified period
    /// and analysis type. Main entry point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resolves the <see cref="AnalysisType"/> from
    /// <paramref name="analysisType"/> via <see cref="AnalysisType.Find"/>.
    /// Unknown keys preserve pre-migration behavior: the AI gets called
    /// with an empty user prompt and the highest-creativity temperature
    /// (0.7). This matches what the legacy <c>AnalysisTypePrompts</c>
    /// switches returned for default cases.
    /// </para>
    /// <para>
    /// The method does not throw; all errors are captured in the returned
    /// <see cref="AnalysisResult"/>.
    /// </para>
    /// </remarks>
    public async Task<AnalysisResult> GetAnalysisAsync(string period, string analysisType, int? providerId = null)
    {
        var type = AnalysisType.Find(analysisType);
        var prompt = type?.Prompt ?? string.Empty;
        var temperature = type?.Temperature ?? 0.7;

        var data = await dataService.AIGetTransactionsCSVAsync(period);
        return await GetAIResponseAsync(prompt, data, temperature, providerId);
    }

    /// <summary>
    /// Sends a request to the AI API for financial analysis. Returns success
    /// or failure with an error message; never throws.
    /// </summary>
    private async Task<AnalysisResult> GetAIResponseAsync(string prompt, string? data, double temperature = 0.7, int? providerId = null)
    {
        var provider = providerId.HasValue
            ? await aiProviderService.GetProviderByIdAsync(providerId.Value)
            : await aiProviderService.GetDefaultProviderAsync();

        if (provider == null)
            return new AnalysisResult(false, "No AI provider configured. Please add an AI provider in Settings.", 0);

        var apiKey = provider.EncryptedApiKey; // TODO: decrypt in future
        var apiUrl = provider.ApiUrl;
        var model = provider.Model ?? "gpt-4o";

        var messages = new List<OpenAIMessage>
        {
            new()
            {
                role = "system",
                content = SystemPrompt,
            },
            new()
            {
                role = "user",
                content = prompt,
            },
        };

        if (!string.IsNullOrWhiteSpace(data))
            messages.Add(new OpenAIMessage
            {
                role = "user",
                content = $"Analyze the following CSV data:\n\n{data}",
            });

        var request = new OpenAIChatRequest
        {
            model = model,
            messages = messages,
            temperature = temperature,
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await httpClient.PostAsync(apiUrl, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var doc = JsonSerializer.Deserialize<OpenAIChatResponse>(responseString);
            if (doc is { Choices.Count: > 0 })
            {
                var result = doc.Choices[0].Message.Content;
                return new AnalysisResult(true, result, doc.Usage.TotalTokens);
            }

            return new AnalysisResult(false, "Error: No choices in response.", doc?.Usage?.TotalTokens ?? 0);
        }

        return new AnalysisResult(false, responseString, 0);
    }
}
