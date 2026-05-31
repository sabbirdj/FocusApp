namespace FocusMode.Helpers;

using System.Runtime.InteropServices;

/// <summary>
/// Contains all P/Invoke declarations for native Windows APIs used by FocusMode.
/// Includes process suspension/resumption (ntdll), working set management (psapi),
/// process handle operations (kernel32), and global hotkey registration (user32).
/// </summary>
public static partial class NativeMethods
{
    // ──────────────────────────────────────────────
    //  ntdll.dll — Process Suspend / Resume
    // ──────────────────────────────────────────────

    /// <summary>
    /// Suspends all threads in the specified process.
    /// </summary>
    /// <param name="processHandle">Handle to the process (requires PROCESS_SUSPEND_RESUME access).</param>
    /// <returns>NTSTATUS code; 0 indicates success.</returns>
    [LibraryImport("ntdll.dll")]
    public static partial uint NtSuspendProcess(IntPtr processHandle);

    /// <summary>
    /// Resumes all threads in the specified process.
    /// </summary>
    /// <param name="processHandle">Handle to the process (requires PROCESS_SUSPEND_RESUME access).</param>
    /// <returns>NTSTATUS code; 0 indicates success.</returns>
    [LibraryImport("ntdll.dll")]
    public static partial uint NtResumeProcess(IntPtr processHandle);

    // ──────────────────────────────────────────────
    //  psapi.dll — Working Set Management
    // ──────────────────────────────────────────────

    /// <summary>
    /// Removes as many pages as possible from the working set of the specified process.
    /// </summary>
    /// <param name="hProcess">Handle to the process (requires PROCESS_SET_QUOTA access).</param>
    /// <returns><c>true</c> if successful; otherwise <c>false</c>.</returns>
    [LibraryImport("psapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EmptyWorkingSet(IntPtr hProcess);

    // ──────────────────────────────────────────────
    //  kernel32.dll — Process Handle Operations
    // ──────────────────────────────────────────────

    /// <summary>
    /// Opens an existing local process object.
    /// </summary>
    /// <param name="dwDesiredAccess">The access rights requested for the process object.</param>
    /// <param name="bInheritHandle">If <c>true</c>, child processes inherit the handle.</param>
    /// <param name="dwProcessId">The identifier of the local process to be opened.</param>
    /// <returns>An open handle to the process, or <see cref="IntPtr.Zero"/> on failure.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    /// <param name="hObject">A valid handle to an open object.</param>
    /// <returns><c>true</c> if successful; otherwise <c>false</c>.</returns>
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(IntPtr hObject);

    // ──────────────────────────────────────────────
    //  user32.dll — Global Hotkey Registration
    // ──────────────────────────────────────────────

    /// <summary>
    /// Defines a system-wide hot key.
    /// </summary>
    /// <param name="hWnd">Handle to the window that will receive WM_HOTKEY messages.</param>
    /// <param name="id">Application-defined identifier for the hot key.</param>
    /// <param name="fsModifiers">Modifier key flags (MOD_ALT, MOD_CONTROL, etc.).</param>
    /// <param name="vk">The virtual-key code of the hot key.</param>
    /// <returns><c>true</c> if successful; otherwise <c>false</c>.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    /// <summary>
    /// Frees a hot key previously registered by the calling thread.
    /// </summary>
    /// <param name="hWnd">Handle to the window associated with the hot key.</param>
    /// <param name="id">The identifier of the hot key to be freed.</param>
    /// <returns><c>true</c> if successful; otherwise <c>false</c>.</returns>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    // ──────────────────────────────────────────────
    //  Process Access Rights Constants
    // ──────────────────────────────────────────────

    /// <summary>Required to suspend or resume a process.</summary>
    public const uint PROCESS_SUSPEND_RESUME = 0x0800;

    /// <summary>Required to call <see cref="EmptyWorkingSet"/>.</summary>
    public const uint PROCESS_SET_QUOTA = 0x0100;

    /// <summary>Required to query process information.</summary>
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;

    /// <summary>Combined access rights needed by FocusMode to manage a process.</summary>
    public const uint PROCESS_ALL_ACCESS = PROCESS_SUSPEND_RESUME | PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION;

    // ──────────────────────────────────────────────
    //  Hotkey Modifier Constants
    // ──────────────────────────────────────────────

    /// <summary>Alt key modifier.</summary>
    public const uint MOD_ALT = 0x0001;

    /// <summary>Ctrl key modifier.</summary>
    public const uint MOD_CONTROL = 0x0002;

    /// <summary>Shift key modifier.</summary>
    public const uint MOD_SHIFT = 0x0004;

    /// <summary>Windows key modifier.</summary>
    public const uint MOD_WIN = 0x0008;

    /// <summary>
    /// Prevents the hotkey from generating repeated WM_HOTKEY messages
    /// when the key is held down.
    /// </summary>
    public const uint MOD_NOREPEAT = 0x4000;

    // ──────────────────────────────────────────────
    //  Window Message Constants
    // ──────────────────────────────────────────────

    /// <summary>Posted when a registered hot key is pressed.</summary>
    public const int WM_HOTKEY = 0x0312;
}
