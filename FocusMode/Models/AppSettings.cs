namespace FocusMode.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Persisted user settings for the FocusMode application.
/// Serialized to / deserialized from a JSON file in the app's local data folder.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Whether FocusMode should launch automatically when the user logs in.
    /// </summary>
    [JsonPropertyName("launchAtStartup")]
    public bool LaunchAtStartup { get; set; } = false;

    /// <summary>
    /// Whether to display the system tray icon while FocusMode is running.
    /// </summary>
    [JsonPropertyName("showTrayIcon")]
    public bool ShowTrayIcon { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, activating focus mode shows a preview of processes
    /// that will be suspended before actually suspending them.
    /// </summary>
    [JsonPropertyName("dryRunPreview")]
    public bool DryRunPreview { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, all suspended processes are automatically resumed
    /// if the FocusMode application exits unexpectedly.
    /// </summary>
    [JsonPropertyName("autoResumeOnExit")]
    public bool AutoResumeOnExit { get; set; } = true;



    /// <summary>
    /// User-defined process names that should never be suspended,
    /// in addition to the built-in system whitelist.
    /// </summary>
    [JsonPropertyName("customWhitelist")]
    public List<string> CustomWhitelist { get; set; } = new();


}
