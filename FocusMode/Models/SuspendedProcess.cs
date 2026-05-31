namespace FocusMode.Models;

using System.Text.Json.Serialization;

/// <summary>
/// State backup of a process that was suspended during Focus Mode.
/// Stores everything needed to wake the process when focus ends.
/// </summary>
public class SuspendedProcessBackup
{
    /// <summary>
    /// The process name (e.g. "chrome", "slack").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The active process IDs that were suspended.
    /// </summary>
    [JsonPropertyName("pids")]
    public List<int> Pids { get; set; } = new();

    /// <summary>
    /// The window handles that were explicitly hidden and need to be shown on resume.
    /// </summary>
    [JsonPropertyName("hiddenWindowHandles")]
    public List<long> HiddenWindowHandles { get; set; } = new();

    /// <summary>
    /// Full path to the executable file.
    /// Used as fallback to re-launch if the suspended PID is lost.
    /// </summary>
    [JsonPropertyName("exePath")]
    public string ExePath { get; set; } = string.Empty;

    /// <summary>
    /// Command line arguments the process was running with (if retrievable).
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;

    /// <summary>
    /// The window title at the time of suspend (for user reference).
    /// </summary>
    [JsonPropertyName("windowTitle")]
    public string WindowTitle { get; set; } = string.Empty;

    /// <summary>
    /// Working set memory in bytes at the time of suspend.
    /// </summary>
    [JsonPropertyName("workingSetBytes")]
    public long WorkingSetBytes { get; set; }

    /// <summary>
    /// Whether this process was successfully resumed/re-launched.
    /// </summary>
    [JsonPropertyName("restored")]
    public bool Restored { get; set; }
}
