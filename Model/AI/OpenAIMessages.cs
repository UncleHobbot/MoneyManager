using System.Text.Json.Serialization;

namespace MoneyManager.Model.AI;

/// <summary>
/// Contains configuration settings for OpenAI API integration.
/// </summary>
/// <remarks>
/// This class stores API credentials and model configuration:
/// - ApiKey: The authentication key for OpenAI API access
/// - ApiUrl: The endpoint URL for OpenAI API
/// - Model: The specific OpenAI model to use for chat completions
/// 
/// These settings are loaded from:
/// - User Secrets (recommended for security)
/// - appsettings.json configuration file
/// - Environment variables
/// 
/// Security Notes:
/// - ApiKey should never be committed to version control
/// - ApiKey should use a least privilege principle
/// - Different keys should be used for development vs production
/// 
/// Model Selection:
/// - Common models: gpt-4, gpt-4-turbo, gpt-3.5-turbo
/// - The model specified must match an available OpenAI model
/// - Different models have different capabilities and pricing
/// </remarks>
public class OpenAISettings
{
    /// <summary>
    /// Gets or sets the API key for authenticating with OpenAI.
    /// </summary>
    /// <value>
    /// The OpenAI API key string. This value is required for API access.
    /// </value>
    /// <remarks>
    /// The API key is used for HTTP Bearer authentication.
    /// This value should be stored securely in User Secrets or environment variables.
    /// Never commit this value to version control or share it publicly.
    /// </remarks>
    public string ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the base URL for the OpenAI chat completion API endpoint.
    /// </summary>
    /// <value>
    /// The full URL of the OpenAI chat completion API.
    /// Example: "https://api.openai.com/v1/chat/completions"
    /// </value>
    /// <remarks>
    /// This URL specifies where API requests are sent.
    /// The default OpenAI endpoint is:
    /// https://api.openai.com/v1/chat/completions
    /// 
    /// This can be customized to:
    /// - Use a different OpenAI API version
    /// - Use a proxy or gateway service
    /// - Use a compatible OpenAI-compatible API
    /// 
    /// The URL must be accessible and return valid responses.
    /// </remarks>
    public string ApiUrl { get; set; }

    /// <summary>
    /// Gets or sets the OpenAI model identifier to use for chat completions.
    /// </summary>
    /// <value>
    /// The model name string for the OpenAI API.
    /// Common values: "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo", "gpt-4o", "gpt-4o-mini"
    /// </value>
    /// <remarks>
    /// This specifies which AI model will process chat requests.
    /// 
    /// **Model Selection Considerations:**
    /// - Capability: Different models have different capabilities
    /// - Performance: Some models are faster (Turbo models)
    /// - Cost: Some models are cheaper (Mini models)
    /// - Quality: Some models produce better outputs (Latest models)
    /// - Context Window: Different models support different maximum token limits
    /// 
    /// **Common Models:**
    /// - gpt-4o: Latest model, highest quality, best reasoning
    /// - gpt-4o-mini: Faster, cheaper version of gpt-4o
    /// - gpt-4-turbo: Previous generation, very fast
    /// - gpt-3.5-turbo: Older generation, cost-optimized
    /// 
    /// The model specified must be available in your OpenAI account region.
    /// Changing this value will affect all AI analyses until changed again.
    /// </remarks>
    public string Model { get; set; }
}

/// <summary>
/// Represents a request to the OpenAI chat completion API.
/// </summary>
/// <remarks>
/// This class encapsulates request data sent to OpenAI:
/// - model: The AI model to use
/// - messages: The conversation history (system prompt plus user messages)
/// - temperature: Controls randomness or creativity (0.0 to 2.0)
/// 
/// The messages array should include:
/// 1. System message (first): Sets AI behavior, instructions, and context
/// 2. User messages: The actual prompts and data to analyze
/// 
/// Temperature Effects:
/// - 0.0: Deterministic, focused responses
/// - 0.7: Balanced creativity and coherence (default)
/// - 1.0+: More random, creative, less predictable
/// 
/// The request is serialized to JSON and sent via HTTP POST.
/// </remarks>
public class OpenAIChatRequest
{
    /// <summary>
    /// Gets or sets the model identifier to use for this request.
    /// </summary>
    /// <value>
    /// The OpenAI model name string.
    /// Must match a model available in the API account.
    /// Example: "gpt-4o", "gpt-4-turbo"
    /// </value>
    /// <remarks>
    /// This determines which AI model processes the request.
    /// Different models have different capabilities and pricing.
    /// </remarks>
    public string model { get; set; }

    /// <summary>
    /// Gets or sets the array of messages in the conversation.
    /// </summary>
    /// <value>
    /// A list of OpenAIMessage objects representing the conversation.
    /// Should typically include one system message followed by one or more user messages.
    /// </value>
    /// <remarks>
    /// The messages array represents the conversation context:
    /// 
    /// **Message Types:**
    /// - System: Sets AI persona, rules, and behavior (usually first message)
    /// - User: Contains the actual prompt or question to answer
    /// - Assistant: Previous AI responses (for multi-turn conversations)
    /// 
    /// **Typical Structure:**
    /// 1. System message: "You are a financial advisor..."
    /// 2. User message with data: "Analyze these transactions..."
    /// 
    /// Messages are processed in order by the AI.
    /// The system message influences all subsequent responses in the request.
    /// </remarks>
    public List<OpenAIMessage> messages { get; set; }

    /// <summary>
    /// Gets or sets the sampling temperature for response generation.
    /// </summary>
    /// <value>
    /// A double value between 0.0 and 2.0 controlling response randomness.
    /// </value>
    /// <remarks>
    /// Temperature controls the randomness of the AI's response:
    /// 
    /// **Temperature Values:**
    /// - 0.0: Deterministic, focused, minimal randomness
    /// - 0.3: Low creativity, precise calculations (DebtAnalysis, CashFlowForecast)
    /// - 0.5: Medium creativity, balanced recommendations (SpendingBudget, SavingsEmergencyFund)
    /// - 0.7: High creativity, varied insights (SpendingGeneral, SpendingTrends)
    /// - 1.0+: Very random, creative, less predictable
    /// 
    /// **Use Cases:**
    /// - Lower (0.3-0.5): Use for factual analysis, calculations, precise answers
    /// - Higher (0.7-1.0): Use for brainstorming, creative insights, varied phrasing
    /// 
    /// Higher temperature makes responses more diverse but less focused.
    /// Lower temperature makes responses more consistent and deterministic.
    /// </remarks>
    public double? temperature { get; set; }
}

/// <summary>
/// Represents a single message in a conversation with the OpenAI API.
/// </summary>
/// <remarks>
/// Each message has a role and content:
/// - role: Who is sending the message ("system", "user", or "assistant")
/// - content: The actual text of the message
/// 
/// Messages are processed sequentially to form the conversation context.
/// The API uses this history to maintain context across turns.
/// </remarks>
public class OpenAIMessage
{
    /// <summary>
    /// Gets or sets the role of the message sender.
    /// </summary>
    /// <value>
    /// The role identifier string.
    /// Valid values: "system", "user", or "assistant".
    /// </value>
    /// <remarks>
    /// The role indicates who is sending the message:
    /// 
    /// **System**: Sets AI behavior, instructions, persona, and constraints
    /// - Usually the first message in the array
    /// - Influences how the AI responds
    /// - Can include rules, tone, format requirements
    /// 
    /// **User**: The actual prompt, question, or data from the application user
    /// - Contains what the user wants the AI to do
    /// - Can include transaction data, questions, instructions
    /// 
    /// **Assistant**: Previous AI responses (for multi-turn conversations)
    /// - Used to maintain conversation history
    /// - Not typically used in single-turn analysis requests
    /// 
    /// Messages are processed in array order.
    /// The system message's instructions apply to all subsequent messages.
    /// </remarks>
    public string role { get; set; }

    /// <summary>
    /// Gets or sets the text content of the message.
    /// </summary>
    /// <value>
    /// The actual text content of the message.
    /// Can include prompts, instructions, data, or analysis results.
    /// </value>
    /// <remarks>
    /// The content field contains the message body:
    /// 
    /// **For System Messages:**
    /// - AI persona description
    /// - Behavioral guidelines
    /// - Output format instructions
    /// - Constraints and rules
    /// - Context about the user
    /// 
    /// **For User Messages:**
    /// - The actual question or request
    /// - Transaction data as CSV
    /// - Analysis type specification
    /// - User context or preferences
    /// 
    /// **For Assistant Messages:**
    /// - Previous AI responses
    /// - Used in multi-turn conversations
    /// 
    /// The content can be multi-line, include markdown, or contain special characters.
    /// </remarks>
    public string content { get; set; }
}

/// <summary>
/// Represents a response from the OpenAI chat completion API.
/// </summary>
/// <remarks>
/// This class encapsulates the complete API response including:
/// - Response metadata (ID, creation time, model used)
/// - Choices: Array of possible responses (usually one)
/// - Usage: Token counts for billing and monitoring
/// 
/// The response is deserialized from JSON returned by the API.
/// The most important data is in the Choices array.
/// </remarks>
public class OpenAIChatResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for this API response.
    /// </summary>
    /// <value>
    /// A unique ID string for the response (e.g., "chatcmpl-abc123...").
    /// This value is generated by the OpenAI API.
    /// </value>
    /// <remarks>
    /// This ID can be used for:
    /// - Tracking requests for debugging
    /// - Matching responses to webhooks
    /// - Auditing API usage
    /// - Reference in support tickets
    /// 
    /// The ID is an opaque string with no guaranteed format.
    /// Different responses will have different IDs even for identical requests.
    /// </remarks>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the object type identifier.
    /// </summary>
    /// <value>
    /// A string identifying the type of object (usually "chat.completion").
    /// </value>
    /// <remarks>
    /// This field indicates the API endpoint that generated the response.
    /// For the chat completion API, this will be "chat.completion".
    /// Other endpoints might return different object types.
    /// This is used for versioning and routing within OpenAI's systems.
    /// </remarks>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// Gets or sets the Unix timestamp when this response was created.
    /// </summary>
    /// <value>
    /// The creation timestamp in seconds since Unix epoch (January 1, 1970).
    /// </value>
    /// <remarks>
    /// This indicates when the OpenAI server generated the response.
    /// Can be converted to DateTime for logging and display.
    /// 
    /// Higher timestamps indicate server processing latency.
    /// The timestamp is in UTC timezone.
    /// </remarks>
    [JsonPropertyName("created")]
    public long Created { get; set; }

    /// <summary>
    /// Gets or sets the model name that was used to generate this response.
    /// </summary>
    /// <value>
    /// The actual OpenAI model name used for this response.
    /// May differ from the requested model if an alias or routing was used.
    /// </value>
    /// <remarks>
    /// This confirms which model actually processed the request.
    /// It might differ from the requested model if:
    /// - An alias was used (e.g., requesting "gpt-4" routes to gpt-4-turbo)
    /// - Model routing based on load balancing
    /// - Regional availability differences
    /// 
    /// Useful for:
    /// - Debugging which model handled a request
    /// - Auditing model-specific performance
    /// - Cost calculation (different models have different pricing)
    /// </remarks>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets the array of generated choices (responses).
    /// </summary>
    /// <value>
    /// A list of OpenAIChatChoice objects containing the AI's responses.
    /// Usually contains one choice for single-turn requests.
    /// </value>
    /// <remarks>
    /// The choices array contains all generated response candidates:
    /// 
    /// **Typical Usage:**
    /// - Single completion: Array with 1 choice
    /// - Multiple choices (not commonly used): Array with several options
    /// 
    /// Each choice includes:
    /// - The generated message content
    /// - Finish reason (why it stopped)
    /// - Token usage details
    /// - Log probability information
    /// 
    /// The application typically uses the first choice (index 0).
    /// </remarks>
    [JsonPropertyName("choices")]
    public List<OpenAIChatChoice> Choices { get; set; }

    /// <summary>
    /// Gets or sets the token usage information for this request.
    /// </summary>
    /// <value>
    /// An OpenAIChatUsage object containing token counts.
    /// </value>
    /// <remarks>
    /// Token usage is used for:
    /// - Billing: OpenAI charges per token used
    /// - Monitoring: Tracking API costs
    /// - Optimization: Reducing token usage to lower costs
    /// - Rate limiting: Some models have token limits
    /// 
    /// The usage includes both input and output tokens.
    /// This helps understand which parts of the request used the most tokens.
    /// </remarks>
    [JsonPropertyName("usage")]
    public OpenAIChatUsage Usage { get; set; }

    /// <summary>
    /// Gets or sets the service tier information for this request.
    /// </summary>
    /// <value>
    /// A string identifying the service tier (e.g., "free", "paid", "enterprise").
    /// </value>
    /// <remarks>
    /// This indicates the service tier of the OpenAI account used.
    /// Different tiers may have:
    /// - Different rate limits
    /// - Different model availability
    /// - Different pricing
    /// - Different SLA guarantees
    /// 
    /// Useful for understanding which features are available.
    /// Not all API endpoints or models may return this field.
    /// </remarks>
    [JsonPropertyName("service_tier")]
    public string ServiceTier { get; set; }

    /// <summary>
    /// Gets or sets the system fingerprint for this response.
    /// </summary>
    /// <value>
    /// A string identifying the system configuration fingerprint.
    /// </value>
    /// <remarks>
    /// This indicates which system configuration generated the response.
    /// Changes when OpenAI updates their models or infrastructure.
    /// Useful for:
    /// - Tracking which version of a model was used
    /// - Debugging inconsistencies in responses
    /// - Understanding model updates and migrations
    /// 
    /// This is not a standard field for all responses.
    /// May be null in some scenarios.
    /// </remarks>
    [JsonPropertyName("system_fingerprint")]
    public string SystemFingerprint { get; set; }
}

/// <summary>
/// Represents a single choice (response) in the OpenAI chat completion response.
/// </summary>
/// <remarks>
/// Each choice contains one possible response from the model:
/// - The actual message content
/// - Why generation stopped (finish_reason)
/// - Token usage breakdown
/// - Log probability (probability distribution)
/// 
/// The application typically uses the first choice (index 0).
/// Multiple choices can occur with temperature greater than 0, but uncommon.
/// </remarks>
public class OpenAIChatChoice
{
    /// <summary>
    /// Gets or sets the index of this choice in the choices array.
    /// </summary>
    /// <value>
    /// The zero-based index of this choice.
    /// </value>
    /// <remarks>
    /// The index indicates the position in the choices array.
    /// Usually 0 for the primary response.
    /// Used for tracking when multiple choices are requested.
    /// Not typically needed for single-choice responses.
    /// </remarks>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the message object containing the AI's response.
    /// </summary>
    /// <value>
    /// An OpenAIChatMessage object containing role and content.
    /// Role is typically "assistant" for responses.
    /// </value>
    /// <remarks>
    /// The message field contains the actual AI-generated content:
    /// - The text response from the AI
    /// - Can include markdown formatting
    /// - Can include structured data (tables, lists)
    /// - May be in bilingual format (English and Russian)
    /// 
    /// This is the primary output value used by the application.
    /// The Content property contains the actual analysis text.
    /// </remarks>
    [JsonPropertyName("message")]
    public OpenAIChatMessage Message { get; set; }

    /// <summary>
    /// Gets or sets the log probability information for this response.
    /// </summary>
    /// <value>
    /// An object containing log probability data or null if not available.
    /// </value>
    /// <remarks>
    /// Log probabilities provide information about the AI's confidence:
    /// - Indicates probability distribution across tokens
    /// - Used for fine-tuning and debugging
    /// - Not commonly used in production applications
    /// 
    /// This field may be null depending on the model and request.
    /// It is primarily useful for developers and researchers.
    /// </remarks>
    [JsonPropertyName("logprobs")]
    public object Logprobs { get; set; }

    /// <summary>
    /// Gets or sets the reason why generation stopped for this choice.
    /// </summary>
    /// <value>
    /// A string indicating why the model stopped generating.
    /// Common values: "stop", "length", "content_filter", "tool_calls".
    /// </value>
    /// <remarks>
    /// The finish reason explains why generation completed:
    /// 
    /// **Common Reasons:**
    /// - "stop": Model naturally completed (most common)
    /// - "length": Reached max_tokens limit
    /// - "content_filter": Content policy violation
    /// - "tool_calls": Called a tool or function instead of generating text
    /// 
    /// Understanding the finish reason helps:
    /// - Optimize prompts for better completion
    /// - Detect issues (e.g., frequent length limit)
    /// - Handle errors appropriately
    /// 
    /// "stop" is the expected and desired finish reason.
    /// </remarks>
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

/// <summary>
/// Represents a chat message with role and content.
/// </summary>
/// <remarks>
/// This is the same structure as OpenAIMessage but used in responses.
/// It contains the AI's response content and role information.
/// 
/// In responses, role is always "assistant".
/// The content is the actual generated text or data.
/// </remarks>
public class OpenAIChatMessage
{
    /// <summary>
    /// Gets or sets the role of the message sender in the response.
    /// </summary>
    /// <value>
    /// The role identifier string.
    /// For AI responses, this is always "assistant".
    /// </value>
    /// <remarks>
    /// In chat completions, response messages always have role "assistant".
    /// This distinguishes the AI's output from user inputs.
    /// The role helps maintain conversation structure.
    /// </remarks>
    [JsonPropertyName("role")]
    public string Role { get; set; }

    /// <summary>
    /// Gets or sets the text content of the AI's response.
    /// </summary>
    /// <value>
    /// The actual text content generated by the AI.
    /// Can include markdown, tables, lists, or other formatting.
    /// </value>
    /// <remarks>
    /// The content field contains the AI-generated output:
    /// - Financial analysis text
    /// - Recommendations and insights
    /// - Tables for structured data
    /// - Bilingual content (English and Russian)
    /// - Formatting with bold, lists, etc.
    /// 
    /// This is the primary value used by the application for display.
    /// Content may include line breaks and special characters.
    /// </remarks>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the refusal information if the request was refused.
    /// </summary>
    /// <value>
    /// An object containing refusal details or null if no refusal occurred.
    /// </value>
    /// <remarks>
    /// Refusal indicates when the AI cannot fulfill the request:
    /// - Content policy violations
    /// - Safety concerns
    /// - Policy restrictions
    /// - System-level blocks
    /// 
    /// If this is null, the request was successful.
    /// If this has a value, the request was refused and content may be null.
    /// 
    /// This is part of OpenAI's safety and content moderation system.
    /// The application should check for refusals and handle them appropriately.
    /// </remarks>
    [JsonPropertyName("refusal")]
    public object Refusal { get; set; }

    /// <summary>
    /// Gets or sets the additional annotations provided by OpenAI.
    /// </summary>
    /// <value>
    /// A list of annotation objects or null if no annotations.
    /// </value>
    /// <remarks>
    /// Annotations provide additional metadata about the response:
    /// - Content warnings
    /// - Safety scores
    /// - Category classifications
    /// - Moderation information
    /// 
    /// This is an advanced feature not always available.
    /// When present, annotations help understand response characteristics.
    /// Typically used for enterprise or specialized applications.
    /// </remarks>
    [JsonPropertyName("annotations")]
    public List<object> Annotations { get; set; }
}

/// <summary>
/// Represents the token usage information for a chat completion request.
/// </summary>
/// <remarks>
/// This class tracks how many tokens were used:
/// - Input tokens: Tokens in the request (messages)
/// - Output tokens: Tokens in the response (message content)
/// - Total tokens: Sum of input and output
/// 
/// Token usage is used for:
/// - Billing calculation
/// - Cost optimization
/// - Rate limiting
/// - Debugging request efficiency
/// 
/// Different models may have different tokenization and costs.
/// Monitoring usage helps control API costs.
/// </remarks>
public class OpenAIChatUsage
{
    /// <summary>
    /// Gets or sets the number of tokens used in the prompt.
    /// </summary>
    /// <value>
    /// The total number of tokens in the messages array sent to the API.
    /// </value>
    /// <remarks>
    /// Prompt tokens are charged based on:
    /// - All messages in the request (system + user + assistant history)
    /// - Each message's content
    /// - Metadata (if applicable)
    /// 
    /// Longer prompts and more context increase prompt tokens.
    /// System prompts can add significant token overhead.
    /// Prompt tokens are typically less expensive than completion tokens for some models.
    /// </remarks>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens in the generated response.
    /// </summary>
    /// <value>
    /// The total number of tokens in the AI's generated message content.
    /// </value>
    /// <remarks>
    /// Completion tokens are based on the response message content:
    /// - Text length
    /// - Formatting (markdown, tables, lists)
    /// - Language (some languages use more tokens)
    /// - Model-specific tokenization
    /// 
    /// Longer responses cost more in both time and money.
    /// Monitoring this helps optimize prompts for shorter responses.
    /// </remarks>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens used in the request.
    /// </summary>
    /// <value>
    /// The sum of prompt_tokens and completion_tokens.
    /// </value>
    /// <remarks>
    /// Total tokens represent the complete request cost:
    /// - Used for billing
    /// - Used for cost tracking
    /// - Used for rate limit calculations
    /// 
    /// Formula: Total Tokens = Prompt Tokens + Completion Tokens
    /// This is the number charged to your OpenAI account.
    /// </remarks>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the detailed breakdown of prompt tokens.
    /// </summary>
    /// <value>
    /// An OpenAITokenDetails object with detailed prompt token information.
    /// </value>
    /// <remarks>
    /// Provides detailed information about how prompt tokens were counted:
    /// - Cached tokens (from cache hits)
    /// - Audio tokens (from voice input)
    /// - Reasoning tokens (from chain-of-thought models)
    /// - Accepted/rejected prediction tokens
    /// 
    /// This is an advanced field for detailed usage analysis.
    /// Helps understand which parts of the prompt used the most tokens.
    /// May be null if detailed breakdown is not available.
    /// </remarks>
    [JsonPropertyName("prompt_tokens_details")]
    public OpenAITokenDetails PromptTokensDetails { get; set; }

    /// <summary>
    /// Gets or sets the detailed breakdown of completion tokens.
    /// </summary>
    /// <value>
    /// An OpenAITokenDetails object with detailed completion token information.
    /// </value>
    /// <remarks>
    /// Provides detailed information about how completion tokens were counted:
    /// - Cached tokens (from cache hits)
    /// - Audio tokens (from voice output)
    /// - Reasoning tokens (from chain-of-thought models)
    /// - Accepted/rejected prediction tokens
    /// 
    /// This is an advanced field for detailed usage analysis.
    /// Helps understand which parts of the response used the most tokens.
    /// May be null if detailed breakdown is not available.
    /// </remarks>
    [JsonPropertyName("completion_tokens_details")]
    public OpenAITokenDetails CompletionTokensDetails { get; set; }
}

/// <summary>
/// Represents detailed token breakdown information.
/// </summary>
/// <remarks>
/// This class provides granular token usage information for advanced tracking.
/// Useful for understanding token distribution across different components.
/// 
/// Token Types Documented:
/// - Cached tokens: Tokens served from cache
/// - Audio tokens: Tokens for audio generation
/// - Reasoning tokens: Tokens for chain-of-thought reasoning
/// - Prediction tokens: Tokens for prediction operations
/// 
/// Not all fields are populated for all requests.
/// Fields will be null if that component was not used.
/// </remarks>
public class OpenAITokenDetails
{
    /// <summary>
    /// Gets or sets the number of tokens served from cache.
    /// </summary>
    /// <value>
    /// The number of tokens returned from cache (if any).
    /// </value>
    /// <remarks>
    /// Cached tokens indicate when a request was served from cache:
    /// - Reduces cost (cached requests are often free or cheaper)
    /// - Reduces latency (cache is faster)
    /// - Improves performance
    /// 
    /// If cache was not used, this will be 0 or null.
    /// OpenAI automatically caches common prompts and responses.
    /// </remarks>
    [JsonPropertyName("cached_tokens")]
    public int CachedTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens used for audio generation.
    /// </summary>
    /// <value>
    /// The number of tokens for audio input or output.
    /// </value>
    /// <remarks>
    /// Audio tokens are used for speech-related features:
    /// - Text-to-speech requests
    /// - Speech-to-text requests
    /// - Audio generation or processing
    /// 
    /// For text-based chat completions, this will typically be 0 or null.
    /// This is an advanced field for specialized use cases.
    /// </remarks>
    [JsonPropertyName("audio_tokens")]
    public int AudioTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens used for chain-of-thought reasoning.
    /// </summary>
    /// <value>
    /// The number of tokens spent on reasoning before generating response.
    /// </value>
    /// <remarks>
    /// Reasoning tokens are used by advanced models (e.g., o1-series):
    /// - Tokens spent on "thinking" before generating response
    /// - Improves accuracy and problem-solving
    /// - Increases cost and latency
    /// 
    /// For simpler models (gpt-4, gpt-3.5-turbo), this will be 0 or null.
    /// This helps understand the "thinking" overhead in advanced models.
    /// </remarks>
    public int? ReasoningTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens used for prediction operations.
    /// </summary>
    /// <value>
    /// The number of tokens for prediction-related features.
    /// </value>
    /// <remarks>
    /// Prediction tokens are used for specialized operations:
    /// - Structured output prediction
    /// - Multi-modal reasoning
    /// - Advanced feature detection
    /// 
    /// For standard chat completions, this will be 0 or null.
    /// This is an advanced field for specialized models.
    /// </remarks>
    public int? AcceptedPredictionTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens spent on rejected prediction operations.
    /// </summary>
    /// <value>
    /// The number of tokens spent on prediction attempts that were rejected.
    /// </value>
    /// <remarks>
    /// Rejected prediction tokens are billed even if prediction was rejected:
    /// - Model may try multiple predictions
    /// - Some attempts may be rejected
    /// - All attempts consume tokens
    /// 
    /// This helps understand the efficiency of prediction operations.
    /// For standard chat completions, this will be 0 or null.
    /// </remarks>
    public int? RejectedPredictionTokens { get; set; }
}

/// <summary>
/// Represents a result of an AI analysis request.
/// </summary>
/// <remarks>
/// This class wraps the analysis result with success information:
/// - Whether the request succeeded
/// - The analysis result text (or error message)
/// - Total tokens used
/// 
/// This provides a unified result type for the application:
/// - Success: Analysis completed successfully
/// - Failure: Error occurred (API error, network issue, etc.)
/// - Tokens: Always provided for billing tracking
/// 
/// The application checks IsSuccess before using the Result.
/// Error messages are returned in the Result field when IsSuccess is false.
/// </remarks>
public class AnalysisResult(bool isSuccess, string result, int totalTokens)
{
    /// <summary>
    /// Gets a value indicating whether the analysis request was successful.
    /// </summary>
    /// <value>
    /// True if the analysis completed successfully, false if an error occurred.
    /// </value>
    /// <remarks>
    /// Success indicates:
    /// - API request completed successfully
    /// - Response was received and parsed
    /// - Analysis content was extracted
    /// 
    /// Failure indicates:
    /// - Network connectivity issues
    /// - API authentication failures
    /// - Rate limiting errors
    /// - Invalid API responses
    /// - JSON parsing errors
    /// 
    /// When false, the Result field contains the error message.
    /// When true, the Result field contains the actual analysis text.
    /// </remarks>
    public bool IsSuccess { get; set; } = isSuccess;

    /// <summary>
    /// Gets or sets the analysis result text or error message.
    /// </summary>
    /// <value>
    /// When IsSuccess is true: The AI-generated financial analysis text.
    /// When IsSuccess is false: An error message describing what went wrong.
    /// </value>
    /// <remarks>
    /// This field contains the primary output or error:
    /// 
    /// **When IsSuccess is true:**
    /// - Bilingual financial analysis (English and Russian)
    /// - May include markdown formatting
    /// - May include tables and lists
    /// - Contains recommendations and insights
    /// 
    /// **When IsSuccess is false:**
    /// - Error description for user
    /// - Possible solution or workaround
    /// - Technical error details
    /// 
    /// The application should always check IsSuccess before using the Result.
    /// </remarks>
    public string Result { get; set; } = result;

    /// <summary>
    /// Gets or sets the total number of tokens used for the analysis.
    /// </summary>
    /// <value>
    /// The sum of prompt_tokens and completion_tokens from the API response.
    /// </value>
    /// <remarks>
    /// This represents the total cost in tokens for the analysis:
    /// - Used for billing calculations
    /// - Used for cost tracking
    /// - Used for budgeting API usage
    /// 
    /// Includes both:
    /// - Input tokens: Tokens in the request (messages + CSV data)
    /// - Output tokens: Tokens in the AI response
    /// 
    /// This value is always provided, even if the request fails.
    /// Helps understand the cost of failed requests too.
    /// </remarks>
    public int TotalTokens { get; set; } = totalTokens;
}
