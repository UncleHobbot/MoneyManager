using System.Text.Json;
using MoneyManager.Api.Helpers;
using MoneyManager.Api.Model;

namespace MoneyManager.Api.Services;

/// <summary>
/// Provides methods for managing application settings persisted to JSON file.
/// </summary>
public class SettingsService(IConfiguration configuration)
{
    /// <summary>
    /// Gets the configured settings directory path.
    /// </summary>
    /// <returns>The settings directory path from configuration, or a default path.</returns>
    private string GetSettingsDirectory()
    {
        return configuration["SettingsPath"] ?? Path.Combine(AppContext.BaseDirectory, "data");
    }

    /// <summary>
    /// Gets the full file path for the settings JSON file.
    /// </summary>
    /// <returns>The complete path to settings.json.</returns>
    private string GetSettingsFilePath() => Path.Combine(GetSettingsDirectory(), "settings.json");

    /// <summary>
    /// Loads application settings from persistent storage.
    /// </summary>
    /// <returns>The current <see cref="SettingsModel"/>, or a default instance if no file exists.</returns>
    public async Task<SettingsModel> GetSettingsAsync()
    {
        var dir = GetSettingsDirectory();
        Directory.CreateDirectory(dir);

        var filePath = GetSettingsFilePath();
        if (File.Exists(filePath))
            return await filePath.ReadJSON<SettingsModel>() ?? new SettingsModel();

        return new SettingsModel();
    }

    /// <summary>
    /// Saves application settings to persistent storage.
    /// </summary>
    /// <param name="data">The <see cref="SettingsModel"/> containing settings to save.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    public async Task SaveSettingsAsync(SettingsModel data)
    {
        var dir = GetSettingsDirectory();
        Directory.CreateDirectory(dir);
        await GetSettingsFilePath().WriteJSON(data);
    }
}
