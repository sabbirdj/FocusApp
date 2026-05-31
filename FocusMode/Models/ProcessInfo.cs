namespace FocusMode.Models;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Represents a running process (or a group of processes sharing the same name)
/// for UI display in the process picker and system tray menu.
/// Inherits from <see cref="ObservableObject"/> so that property changes
/// (e.g. selection toggle) are automatically reflected in data-bound UI elements.
/// </summary>
public partial class ProcessInfo : ObservableObject
{
    /// <summary>
    /// The representative process identifier (PID) — typically the main/parent process.
    /// </summary>
    public int Pid { get; set; }

    /// <summary>
    /// All PIDs belonging to this logical application group (e.g. all chrome.exe instances).
    /// Used by <c>ActivateFocusMode</c> to kill every instance of a grouped app.
    /// </summary>
    public List<int> AllPids { get; set; } = new();

    /// <summary>
    /// The process name (e.g. "chrome", "devenv").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A user-friendly display name, typically the main window title
    /// or a friendly application name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the process has an active visible window.
    /// </summary>
    public bool IsWindowed { get; set; }

    /// <summary>
    /// The aggregated working set memory size in bytes across all PIDs in this group.
    /// </summary>
    public long WorkingSetBytes { get; set; }

    /// <summary>
    /// The file path to the extracted process icon (PNG), or
    /// <c>null</c> if no icon could be extracted.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Indicates whether the user has selected this process in the
    /// preview / multi-select UI. This property raises
    /// <see cref="ObservableObject.PropertyChanged"/> automatically
    /// via the CommunityToolkit source generator.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Returns a human-readable representation of <see cref="WorkingSetBytes"/>.
    /// Examples: "1.2 GB", "340 MB", "512 KB".
    /// </summary>
    public string FormattedMemory
    {
        get
        {
            const double KB = 1024;
            const double MB = KB * 1024;
            const double GB = MB * 1024;

            return WorkingSetBytes switch
            {
                >= (long)GB => $"{WorkingSetBytes / GB:F1} GB",
                >= (long)MB => $"{WorkingSetBytes / MB:F0} MB",
                >= (long)KB => $"{WorkingSetBytes / KB:F0} KB",
                _ => $"{WorkingSetBytes} B"
            };
        }
    }
}
