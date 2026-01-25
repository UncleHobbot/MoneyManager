using Microsoft.WindowsAPICodePack.Dialogs;

namespace MoneyManager.Services;

/// <summary>
/// Provides folder and file picker dialog functionality for the MoneyManager application.
/// </summary>
/// <remarks>
/// This service uses Windows API Code Pack's CommonOpenFileDialog to display:
/// - Folder picker dialogs for selecting directories
/// - File picker dialogs for selecting files with custom filters
/// 
/// The dialogs are native Windows dialogs that provide:
/// - Consistent user experience with other Windows applications
/// - Network location browsing
/// - Favorite locations integration
/// - Standard file/folder navigation
/// 
/// Methods return empty string when user cancels the dialog,
/// allowing the calling code to distinguish between selection and cancellation.
/// </remarks>
public class FolderPicker
{
    /// <summary>
    /// Displays a folder picker dialog and returns the selected folder path.
    /// </summary>
    /// <returns>
    /// The full path of the selected folder, or an empty string if the user cancels the dialog.
    /// </returns>
    /// <remarks>
    /// This method:
    /// 1. Creates a CommonOpenFileDialog configured as a folder picker
    /// 2. Shows the dialog modally (blocks until user selects or cancels)
    /// 3. Returns the selected folder path
    /// 4. Returns empty string if user clicks Cancel or closes dialog without selection
    /// 
    /// Dialog Behavior:
    /// - Displays standard Windows folder browser
    /// - Starts in the last used location if available
    /// - Allows browsing network locations
    /// - Shows Create New Folder button
    /// 
    /// Return Value:
    /// - Selected folder path: "C:\Users\...\Documents\MyFolder"
    /// - Empty string: "" (when dialog is cancelled)
    /// 
    /// Notes:
    /// - The folder path returned is the full absolute path
    /// - The folder does not need to exist before selecting (user can create new)
    /// - This is a blocking operation during dialog display
    /// - Thread safety: Should be called from UI thread
    /// </remarks>
    public string DisplayFolderPicker()
    {
        var dialog = new CommonOpenFileDialog { IsFolderPicker = true };
        var result = dialog.ShowDialog();
        return result == CommonFileDialogResult.Ok ? dialog.FileName : "";
    }

    /// <summary>
    /// Displays a file picker dialog with custom filter and returns the selected file path.
    /// </summary>
    /// <param name="filterName">The display name for the file type filter (e.g., "CSV Files", "Text Files").</param>
    /// <param name="filterExt">The file extension filter (e.g., "*.csv", "*.txt").</param>
    /// <returns>
    /// The full path of the selected file, or an empty string if the user cancels the dialog.
    /// </returns>
    /// <remarks>
    /// This method:
    /// 1. Creates a CommonOpenFileDialog configured as a file picker (not folder)
    /// 2. Adds a filter based on the provided name and extension
    /// 3. Shows the dialog modally (blocks until user selects or cancels)
    /// 4. Returns the selected file path
    /// 5. Returns empty string if user clicks Cancel or closes dialog without selection
    /// 
    /// Filter Behavior:
    /// - Creates a single filter entry
    /// - Display name: "CSV Files" (from filterName parameter)
    /// - Extension pattern: "*.csv" (from filterExt parameter)
    /// - Multiple filters can be added by calling Add multiple times
    /// 
    /// Example Usage:
    /// - DisplayFolderPicker("CSV Files", "*.csv") - Shows only CSV files
    /// - DisplayFolderPicker("Text Files", "*.txt") - Shows only text files
    /// 
    /// Dialog Behavior:
    /// - Displays standard Windows file browser
    /// - Starts in the last used location if available
    /// - Allows browsing network locations
    /// - Shows file name field for typing
    /// 
    /// Return Value:
    /// - Selected file path: "C:\Users\...\Documents\MyFile.csv"
    /// - Empty string: "" (when dialog is cancelled)
    /// 
    /// Notes:
    /// - The file path returned is the full absolute path
    /// - The file must exist when selected
    /// - FilterName should be user-friendly description
    /// - FilterExt should include the asterisk (*) wildcard
    /// - This is a blocking operation during dialog display
    /// - Thread safety: Should be called from UI thread
    /// 
    /// Filter Examples:
    /// - "CSV Files" + "*.csv" for CSV documents
    /// - "Text Files" + "*.txt" for text documents
    /// - "JSON Files" + "*.json" for JSON files
    /// - "All Files" + "*.*" for any file type
    /// </remarks>
    /// <example>
    /// <code>
    /// // Select a CSV file
    /// string filePath = folderPicker.DisplayFilePicker("CSV Files", "*.csv");
    /// if (!string.IsNullOrEmpty(filePath))
    /// {
    ///     // User selected a file, process it
    ///     ProcessFile(filePath);
    /// }
    /// else
    /// {
    ///     // User cancelled the dialog
    ///     ShowMessage("No file selected");
    /// }
    /// </code>
    /// </example>
    public string DisplayFilePicker(string? filterName, string? filterExt)
    {
        var dialog = new CommonOpenFileDialog { IsFolderPicker = false };
        dialog.Filters.Add(new CommonFileDialogFilter { DisplayName = filterName, Extensions = { filterExt } });
        var result = dialog.ShowDialog();
        return result == CommonFileDialogResult.Ok ? dialog.FileName : "";
    }
}
