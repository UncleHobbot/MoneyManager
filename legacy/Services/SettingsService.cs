namespace MoneyManager.Services;

/// <summary>
/// Provides methods for managing application settings and user preferences.
/// </summary>
/// <remarks>
/// Settings are persisted to a JSON file in the application data directory.
/// Automatically migrates settings from old location (<see cref="Microsoft.AspNetCore.Hosting.HostingExtensionsExtensions.WebApplicationExtensions.WebApplicationExtensionsExtensionsExtensions.UserAppDataPath"/>) to new stable location.
/// Includes dark mode toggle and backup path configuration.
/// All operations are asynchronous to prevent blocking the UI thread.
/// </remarks>
public class SettingsService
{
    /// <summary>
    /// Gets the directory path where application settings are stored.
    /// </summary>
    /// <value>
    /// The full path to the MoneyManager settings folder in ApplicationData directory.
    /// </value>
    /// <remarks>
    /// Stable location across application versions.
    /// Format: %APPDATA%/MoneyManager/
    /// Created automatically if it doesn't exist.
    /// </remarks>
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MoneyManager"
    );

    /// <summary>
    /// Gets the full file path for the settings.json file.
    /// </summary>
    /// <value>
    /// The complete path including filename: %APPDATA%/MoneyManager/settings.json
    /// </value>
    /// <remarks>
    /// Used by <see cref="GetSettings"/> and <see cref="SaveSettings(MoneyManager.Model.SettingsModel)"/>.
    /// Combines <see cref="SettingsDirectory"/> with settings filename.
    /// </remarks>
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    /// <summary>
    /// Loads application settings from the persistent storage.
    /// </summary>
    /// <returns>
    /// A task representing to asynchronous operation. The task result is a <see cref="MoneyManager.Model.SettingsModel"/> with current settings.
    /// </returns>
    /// <remarks>
    /// Ensures <see cref="SettingsDirectory"/> exists before attempting to load.
    /// Prioritizes loading from new stable location (<see cref="SettingsFilePath"/>).
    /// If no file exists in new location, attempts to migrate from old location (<see cref="Microsoft.AspNetCore.Hosting.HostingExtensionsExtensions.WebApplicationExtensions.WebApplicationExtensionsExtensionsExtensionsExtensions.UserAppDataPath"/>).
    /// Automatically deletes old settings file after successful migration.
    /// Returns default <see cref="MoneyManager.Model.SettingsModel"/> if no settings file is found in either location.
    /// Uses <see cref="Helpers.JSONHelper.ReadJSON{T}(string)"/> for deserialization.
    /// </remarks>
    public async Task<SettingsModel> GetSettings()
    {
        // Ensure settings directory exists
        Directory.CreateDirectory(SettingsDirectory);

        // Try to load from stable path first
        if (File.Exists(SettingsFilePath))
        {
            return await SettingsFilePath.ReadJSON<SettingsModel>();
        }

        // Attempt to migrate from old Application.UserAppDataPath location
        var oldSettingsFile = Path.Combine(Application.UserAppDataPath, "settings.json");
        if (File.Exists(oldSettingsFile))
        {
            // Migrate settings to new stable location
            var settings = await oldSettingsFile.ReadJSON<SettingsModel>();
            await SaveSettings(settings);

            // Optionally delete old file after successful migration
            try { File.Delete(oldSettingsFile); } catch { /* ignore */ }

            return settings;
        }

        // Return default settings if nothing found
        return new SettingsModel();
    }

    /// <summary>
    /// Saves application settings to the persistent storage.
    /// </summary>
    /// <param name="data">
    /// The <see cref="MoneyManager.Model.SettingsModel"/> containing settings to save.
    /// </param>
    /// <returns>
    /// A task representing to asynchronous operation.
    /// </returns>
    /// <remarks>
    /// Ensures <see cref="SettingsDirectory"/> exists before attempting to save.
    /// Overwrites any existing settings file.
    /// Creates the file if it doesn't exist.
    /// Uses <see cref="Helpers.JSONHelper.WriteJSON{T}(string, T)"/> for serialization with indented formatting.
    /// All settings are saved at once; no partial updates are supported.
    /// </remarks>
    /// <exception cref="System.IO.IOException">
    /// Thrown when the file path is invalid or the application doesn't have permission to write.
    /// </exception>
    /// <exception cref="System.UnauthorizedAccessException">
    /// Thrown when the application doesn't have permission to write to the settings directory.
    /// </exception>
    public async Task SaveSettings(SettingsModel data)
    {
        // Ensure settings directory exists
        Directory.CreateDirectory(SettingsDirectory);

        await SettingsFilePath.WriteJSON(data);
    }
}
