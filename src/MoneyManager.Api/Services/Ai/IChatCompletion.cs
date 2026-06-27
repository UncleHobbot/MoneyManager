namespace MoneyManager.Api.Services.Ai;

/// <summary>
/// A provider-neutral chat-completion request. Carries the resolved provider
/// coordinates (<see cref="Endpoint"/>/<see cref="ApiKey"/>/<see cref="Model"/>)
/// plus the assembled prompt. The adapter owns the wire format; this record
/// speaks the domain. See CONTEXT.md ("AI Transport / Chat Completion").
/// </summary>
public record ChatRequest(
    string Endpoint,
    string ApiKey,
    string Model,
    string SystemPrompt,
    string UserPrompt,
    string? Data,
    double Temperature);

/// <summary>
/// The outcome of a chat completion. On failure <see cref="Content"/> carries
/// the error text (mirrors the legacy <c>AnalysisResult(false, body, 0)</c>
/// shape). The transport adapter never throws — non-2xx responses and transport
/// exceptions both map to <c>Success == false</c>.
/// </summary>
public record ChatResult(bool Success, string Content, int TotalTokens);

/// <summary>
/// The AI transport seam. Implemented in production by
/// <see cref="OpenAiCompatibleChatCompletion"/> (HTTP) and in tests by a fake —
/// two adapters justify the seam (ADR-0011, and ADR-0003's "two = real" rule).
/// </summary>
public interface IChatCompletion
{
    Task<ChatResult> CompleteAsync(ChatRequest request);
}
