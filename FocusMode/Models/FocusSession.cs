namespace FocusMode.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an active or completed focus session.
/// Stores the backup of all suspended processes so they can be resumed.
/// </summary>
public class FocusSession
{
    /// <summary>
    /// UTC timestamp when focus mode was activated.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Backup of every process that was suspended during this session.
    /// Used to wake them up when focus mode ends.
    /// </summary>
    [JsonPropertyName("suspendedProcesses")]
    public List<SuspendedProcessBackup> SuspendedProcesses { get; set; } = new();

    /// <summary>
    /// The process names the user chose to keep alive (focus apps).
    /// </summary>
    [JsonPropertyName("focusApps")]
    public List<string> FocusApps { get; set; } = new();

    /// <summary>
    /// Total RAM freed in bytes by emptying working sets of suspended processes.
    /// </summary>
    [JsonPropertyName("ramFreedBytes")]
    public long RamFreedBytes { get; set; }

    /// <summary>
    /// Total number of processes suspended.
    /// </summary>
    [JsonPropertyName("suspendedCount")]
    public int SuspendedCount { get; set; }
}
