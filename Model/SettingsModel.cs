namespace MoneyManager.Model;

/// <summary>
/// Represents application-wide user settings and preferences.
/// </summary>
public class SettingsModel
{
    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled for the UI.
    /// Defaults to true.
    /// </summary>
    public bool IsDarkMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the file system path where database backups should be stored.
    /// If null or empty, the default backup location will be used.
    /// </summary>
    public string? BackupPath { get; set; }
}
