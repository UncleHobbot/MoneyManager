using MoneyManager.Api.Model.AI;
using MoneyManager.Api.Services.Ai;

namespace MoneyManager.Api.Services;

/// <summary>
/// Builds a financial-analysis request (system persona + per-type user prompt +
/// CSV data) and maps the result to an <see cref="AnalysisResult"/>. The actual
/// transport lives behind the <see cref="IChatCompletion"/> seam (see CONTEXT.md
/// "AI Transport / Chat Completion"); this service owns prompt assembly, provider
/// resolution, and result mapping — all of it testable against a fake.
/// </summary>
public class AIService(
    AiProviderService aiProviderService,
    DataService dataService,
    IChatCompletion chatCompletion)
{
    /// <summary>
    /// The system prompt that establishes the certified-financial-advisor
    /// persona. Kept as a const here (co-located with its only consumer);
    /// extracting it to a resource is a separate candidate.
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
    /// Performs financial analysis on transactions for a period and analysis
    /// type. Unknown keys preserve pre-migration behavior: empty user prompt and
    /// the highest-creativity temperature (0.7). Never throws — all errors are
    /// captured in the returned <see cref="AnalysisResult"/>.
    /// </summary>
    public async Task<AnalysisResult> GetAnalysisAsync(string period, string analysisType, int? providerId = null)
    {
        var type = AnalysisType.Find(analysisType);
        var prompt = type?.Prompt ?? string.Empty;
        var temperature = type?.Temperature ?? 0.7;

        var provider = providerId.HasValue
            ? await aiProviderService.GetProviderByIdAsync(providerId.Value)
            : await aiProviderService.GetDefaultProviderAsync();

        if (provider == null)
            return new AnalysisResult(false, "No AI provider configured. Please add an AI provider in Settings.", 0);

        var data = await dataService.AIGetTransactionsCSVAsync(period);

        var request = new ChatRequest(
            Endpoint: provider.ApiUrl,
            ApiKey: provider.EncryptedApiKey, // TODO: decrypt in future
            Model: provider.Model ?? "gpt-4o",
            SystemPrompt: SystemPrompt,
            UserPrompt: prompt,
            Data: data,
            Temperature: temperature);

        var result = await chatCompletion.CompleteAsync(request);
        return new AnalysisResult(result.Success, result.Content, result.TotalTokens);
    }
}
