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

    public string DisplayFilePicker(string filterName = null, string filterExt = null)
    {
        var dialog = new CommonOpenFileDialog { IsFolderPicker = false };
        if (filterName != null)
            dialog.Filters.Add(new CommonFileDialogFilter { DisplayName = filterName, Extensions = { filterExt } });
        var result = dialog.ShowDialog();
        return result == CommonFileDialogResult.Ok ? dialog.FileName : "";
    }

}