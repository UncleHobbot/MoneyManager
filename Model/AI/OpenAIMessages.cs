using System.Text.Json.Serialization;

namespace MoneyManager.Model.AI;

public class OpenAISettings
{
    public string ApiKey { get; set; }
    public string ApiUrl { get; set; }
    public string Model { get; set; }
}

// OpenAI Chat Request
public class OpenAIChatRequest
{
    public string model { get; set; }
    public List<OpenAIMessage> messages { get; set; }

    public double? temperature { get; set; }
    // public int? maxTokens { get; set; }
    // public double? TopP { get; set; }
    // public int? N { get; set; }
    // public bool? Stream { get; set; }
    // public object? Stop { get; set; } // string or string[]
    // public double? PresencePenalty { get; set; }
    // public double? FrequencyPenalty { get; set; }
    // public Dictionary<string, int>? LogitBias { get; set; }
    // public string? user { get; set; }
}

public class OpenAIMessage
{
    public string role { get; set; } // "system", "user", or "assistant"
    public string content { get; set; }
}

// OpenAI Chat Response
public class OpenAIChatResponse
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("object")] public string Object { get; set; }

    [JsonPropertyName("created")] public long Created { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; }

    [JsonPropertyName("choices")] public List<OpenAIChatChoice> Choices { get; set; }

    [JsonPropertyName("usage")] public OpenAIChatUsage Usage { get; set; }

    [JsonPropertyName("service_tier")] public string ServiceTier { get; set; }

    [JsonPropertyName("system_fingerprint")]
    public string SystemFingerprint { get; set; }
}

public class OpenAIChatChoice
{
    [JsonPropertyName("index")] public int Index { get; set; }

    [JsonPropertyName("message")] public OpenAIChatMessage Message { get; set; }

    [JsonPropertyName("logprobs")] public object Logprobs { get; set; }

    [JsonPropertyName("finish_reason")] public string FinishReason { get; set; }
}

public class OpenAIChatMessage
{
    [JsonPropertyName("role")] public string Role { get; set; }

    [JsonPropertyName("content")] public string Content { get; set; }

    [JsonPropertyName("refusal")] public object Refusal { get; set; }

    [JsonPropertyName("annotations")] public List<object> Annotations { get; set; }
}

public class OpenAIChatUsage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }

    [JsonPropertyName("prompt_tokens_details")]
    public OpenAITokenDetails PromptTokensDetails { get; set; }

    [JsonPropertyName("completion_tokens_details")]
    public OpenAITokenDetails CompletionTokensDetails { get; set; }
}

public class OpenAITokenDetails
{
    [JsonPropertyName("cached_tokens")] public int CachedTokens { get; set; }

    [JsonPropertyName("audio_tokens")] public int AudioTokens { get; set; }

    [JsonPropertyName("reasoning_tokens")] public int? ReasoningTokens { get; set; }

    [JsonPropertyName("accepted_prediction_tokens")]
    public int? AcceptedPredictionTokens { get; set; }

    [JsonPropertyName("rejected_prediction_tokens")]
    public int? RejectedPredictionTokens { get; set; }
}

public class AnalysisResult(bool isSuccess, string result, int totalTokens)
{
    public bool IsSuccess { get; set; } = isSuccess;
    public string Result { get; set; } = result;
    public int TotalTokens { get; set; } = totalTokens;
}