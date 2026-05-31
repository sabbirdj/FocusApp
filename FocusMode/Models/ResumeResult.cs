namespace FocusMode.Models;

/// <summary>
/// Result of ending focus mode and restoring killed processes.
/// </summary>
public class ResumeResult
{
    /// <summary>
    /// Number of processes successfully re-launched.
    /// </summary>
    public int RestoredCount { get; set; }

    /// <summary>
    /// Number of processes that failed to re-launch.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Names of processes that could not be re-launched.
    /// </summary>
    public List<string> FailedProcessNames { get; set; } = new();

    /// <summary>
    /// How long the focus session lasted.
    /// </summary>
    public TimeSpan SessionDuration { get; set; }
}
