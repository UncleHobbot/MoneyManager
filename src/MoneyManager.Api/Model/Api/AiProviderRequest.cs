namespace MoneyManager.Api.Model.Api;

/// <summary>
/// Represents a request to create or update an AI provider configuration.
/// </summary>
/// <remarks>
/// Contains all settings needed to configure an AI provider,
/// including credentials and model selection.
/// </remarks>
public class AiProviderRequest
{
    /// <summary>
    /// Gets or sets the display name for the AI provider.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider type identifier (e.g., "OpenAI", "Azure").
    /// </summary>
    public string ProviderType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API key for authenticating with the provider.
    /// </summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>
    /// Gets or sets the API endpoint URL for the provider.
    /// </summary>
    public string ApiUrl { get; set; } = null!;

    /// <summary>
    /// Gets or sets the model identifier to use with the provider.
    /// </summary>
    public string Model { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether this is the default AI provider.
    /// </summary>
    public bool IsDefault { get; set; }
}
