using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MoneyManager.Api.Services.Ai;

/// <summary>
/// HTTP adapter over any OpenAI-compatible chat-completions endpoint — OpenAI,
/// DeepSeek, Z.AI GLM, and anything else that speaks the same wire format. The
/// per-provider differences (base URL, model, key) arrive on the
/// <see cref="ChatRequest"/>, so a single adapter serves every compatible
/// provider; a provider needing a different wire format gets its own
/// <see cref="IChatCompletion"/> adapter (see ADR-0011).
/// </summary>
/// <remarks>
/// Never throws: non-2xx responses and transport exceptions both map to
/// <c>ChatResult(false, ...)</c>. The <c>Authorization</c> header is set per
/// request (not on the shared <see cref="HttpClient"/>), so concurrent calls
/// with different keys cannot race.
/// </remarks>
public class OpenAiCompatibleChatCompletion(HttpClient httpClient) : IChatCompletion
{
    public async Task<ChatResult> CompleteAsync(ChatRequest request)
    {
        var messages = new List<OpenAiMessage>
        {
            new() { role = "system", content = request.SystemPrompt },
            new() { role = "user", content = request.UserPrompt },
        };

        if (!string.IsNullOrWhiteSpace(request.Data))
            messages.Add(new OpenAiMessage { role = "user", content = $"Analyze the following CSV data:\n\n{request.Data}" });

        var payload = new OpenAiChatRequest
        {
            model = request.Model,
            messages = messages,
            temperature = request.Temperature,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(httpRequest);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new ChatResult(false, body, 0);

            var doc = JsonSerializer.Deserialize<OpenAiChatResponse>(body);
            if (doc is { Choices.Count: > 0 })
                return new ChatResult(true, doc.Choices[0].Message.Content, doc.Usage?.TotalTokens ?? 0);

            return new ChatResult(false, "Error: No choices in response.", doc?.Usage?.TotalTokens ?? 0);
        }
        catch (Exception ex)
        {
            // Transport failures (timeout, DNS, connection reset) preserve the
            // "never throws" contract: the endpoint returns 200 + IsSuccess=false.
            return new ChatResult(false, ex.Message, 0);
        }
    }
}

// ── OpenAI wire format — internal to the adapter ──────────────────────────────
// Only the fields the adapter reads/writes are modelled. The rich response
// (id, created, finish_reason, token-detail breakdowns, ...) is intentionally
// not mapped — see git history for the previous over-modelled version.

internal class OpenAiChatRequest
{
    public string model { get; set; } = "";
    public List<OpenAiMessage> messages { get; set; } = [];
    public double temperature { get; set; }
}

internal class OpenAiMessage
{
    public string role { get; set; } = "";
    public string content { get; set; } = "";
}

internal class OpenAiChatResponse
{
    [JsonPropertyName("choices")] public List<OpenAiChatChoice>? Choices { get; set; }
    [JsonPropertyName("usage")] public OpenAiChatUsage? Usage { get; set; }
}

internal class OpenAiChatChoice
{
    [JsonPropertyName("message")] public OpenAiChatMessage Message { get; set; } = new();
}

internal class OpenAiChatMessage
{
    [JsonPropertyName("content")] public string Content { get; set; } = "";
}

internal class OpenAiChatUsage
{
    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
}
