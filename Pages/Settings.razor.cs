using Microsoft.AspNetCore.Components;

namespace MoneyManager.Pages;

public partial class Settings
{
    [Inject] private SettingsService service { get; set; } = null!;
    [Inject] private FolderPicker folderPicker { get; set; } = null!;
    private SettingsModel settings = new();

    protected override async Task OnInitializedAsync()
    {
        settings = await service.GetSettings();
    }

    private void SelectImportFile()
    {
        settings.BackupPath = folderPicker.DisplayFolderPicker();
    }

    private async Task SaveSettings()
    {
        await service.SaveSettings(settings);
    }
}