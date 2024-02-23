namespace MoneyManager.Services;

public class SettingsService
{
    public async Task<SettingsModel> GetSettings()
    {
        var settingsFile = Path.Combine(Application.UserAppDataPath, "settings.json");
        return !File.Exists(settingsFile) ? new SettingsModel() : await settingsFile.ReadJSON<SettingsModel>();
    }

    public async Task SaveSettings(SettingsModel data)
        => await Path.Combine(Application.UserAppDataPath, "settings.json").WriteJSON(data);

}