namespace MoneyManager.Services;

public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MoneyManager"
    );

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

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

    public async Task SaveSettings(SettingsModel data)
    {
        // Ensure settings directory exists
        Directory.CreateDirectory(SettingsDirectory);

        await SettingsFilePath.WriteJSON(data);
    }
}