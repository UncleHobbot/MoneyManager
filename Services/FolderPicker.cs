using Microsoft.WindowsAPICodePack.Dialogs;

namespace MoneyManager.Services;

public class FolderPicker
{
    public string DisplayFolderPicker()
    {
        var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
        var result = dialog.ShowDialog();
        return result == CommonFileDialogResult.Ok ? dialog.FileName : "";
    }

    public string DisplayFilePicker(string? filterName, string? filterExt)
    {
        var dialog = new CommonOpenFileDialog { IsFolderPicker = false };
        dialog.Filters.Add(new CommonFileDialogFilter { DisplayName = filterName, Extensions = { filterExt } });
        var result = dialog.ShowDialog();
        return result == CommonFileDialogResult.Ok ? dialog.FileName : "";
    }
}