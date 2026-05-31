using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FocusMode.Models;

namespace FocusMode.Services;

/// <summary>
/// Handles process discovery, filtering, and the core kill/restore logic.
/// </summary>
public class ProcessManager
{
    private readonly WhitelistService _whitelistService;
    private readonly SessionPersistenceService _sessionPersistenceService;

    public ProcessManager(
        WhitelistService whitelistService,
        SessionPersistenceService sessionPersistenceService)
    {
        _whitelistService = whitelistService;
        _sessionPersistenceService = sessionPersistenceService;
    }

    /// <summary>
    /// Returns processes that have a visible window (MainWindowTitle is not empty).
    /// Groups by process name, summing RAM and collecting all PIDs.
    /// </summary>
    public List<ProcessInfo> GetWindowedApps()
    {
        var currentPid = Environment.ProcessId;
        var processes = Process.GetProcesses();
        var grouped = new Dictionary<string, ProcessInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in processes)
        {
            try
            {
                if (p.Id == currentPid) continue;

                string windowTitle = "";
                try { windowTitle = p.MainWindowTitle; } catch { }

                if (string.IsNullOrWhiteSpace(windowTitle)) continue;

                bool isSystem = false;
                try { isSystem = _whitelistService.IsSystemProcess(p); } catch { }
                if (isSystem) continue;

                string processName = "";
                try { processName = p.ProcessName; } catch { continue; }

                if (_whitelistService.IsWhitelisted(processName)) continue;

                long workingSet = 0;
                try { workingSet = p.WorkingSet64; } catch { }

                if (!grouped.TryGetValue(processName, out var info))
                {
                    info = new ProcessInfo
                    {
                        Pid = p.Id,
                        Name = processName,
                        DisplayName = windowTitle,
                        WorkingSetBytes = 0,
                        IconPath = null,
                        IsWindowed = true
                    };
                    grouped[processName] = info;
                }

                info.AllPids.Add(p.Id);
                info.WorkingSetBytes += workingSet;
            }
            catch { /* Ignore */ }
            finally
            {
                p.Dispose();
            }
        }

        return grouped.Values.OrderByDescending(x => x.WorkingSetBytes).ToList();
    }

    /// <summary>
    /// Returns all other user processes that DON'T have a visible window.
    /// Groups by process name, summing RAM and collecting all PIDs.
    /// </summary>
    public List<ProcessInfo> GetBackgroundProcesses()
    {
        var currentPid = Environment.ProcessId;
        var processes = Process.GetProcesses();
        var grouped = new Dictionary<string, ProcessInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in processes)
        {
            try
            {
                if (p.Id == currentPid) continue;

                string windowTitle = "";
                try { windowTitle = p.MainWindowTitle; } catch { }

                if (!string.IsNullOrWhiteSpace(windowTitle)) continue; // Handled by GetWindowedApps

                bool isSystem = false;
                try { isSystem = _whitelistService.IsSystemProcess(p); } catch { }
                if (isSystem) continue;

                string processName = "";
                try { processName = p.ProcessName; } catch { continue; }

                if (_whitelistService.IsWhitelisted(processName)) continue;

                long workingSet = 0;
                try { workingSet = p.WorkingSet64; } catch { }

                if (!grouped.TryGetValue(processName, out var info))
                {
                    info = new ProcessInfo
                    {
                        Pid = p.Id,
                        Name = processName,
                        DisplayName = processName, // Fallback for background
                        WorkingSetBytes = 0,
                        IconPath = null,
                        IsWindowed = false
                    };
                    grouped[processName] = info;
                }

                info.AllPids.Add(p.Id);
                info.WorkingSetBytes += workingSet;
            }
            catch { /* Ignore */ }
            finally
            {
                p.Dispose();
            }
        }

        return grouped.Values.OrderByDescending(x => x.WorkingSetBytes).ToList();
    }

    /// <summary>
    /// Returns ALL processes (windowed + background) minus the selected focus apps.
    /// This represents the exact list of apps that will be suspended.
    /// </summary>
    public List<ProcessInfo> GetHibernateableProcesses(List<string> focusApps)
    {
        var focusSet = new HashSet<string>(focusApps, StringComparer.OrdinalIgnoreCase);
        
        var allApps = new List<ProcessInfo>();
        allApps.AddRange(GetWindowedApps());
        allApps.AddRange(GetBackgroundProcesses());

        return allApps
            .Where(p => !focusSet.Contains(p.Name))
            .ToList();
    }

    /// <summary>
    /// Activates focus mode by freezing the target processes and flushing their RAM to disk.
    /// Backs up executable paths and PIDs for resumption.
    /// </summary>
    public FocusSession ActivateFocusMode(List<ProcessInfo> toSuspend, List<string> focusAppNames)
    {
        var session = new FocusSession
        {
            StartTime = DateTime.UtcNow,
            FocusApps = focusAppNames
        };

        var safePids = GetSafePids(focusAppNames);
        long actualRamFreed = 0;

        var processesToFreeze = new List<(Process proc, List<long> handles, ProcessInfo appGroup, SuspendedProcessBackup? backupObj)>();

        foreach (var appGroup in toSuspend)
        {
            var pidsToSuspend = appGroup.AllPids.Where(pid => !safePids.Contains(pid)).ToList();
            if (pidsToSuspend.Count == 0) continue;

            appGroup.AllPids = pidsToSuspend;
            BackupProcessData(appGroup, session);
            
            var backupObj = session.SuspendedProcesses.FirstOrDefault(b => b.Name == appGroup.Name);

            foreach (var pid in pidsToSuspend)
            {
                try
                {
                    var proc = Process.GetProcessById(pid);
                    
                    // Safety Guards
                    if (proc.SessionId == 0) { proc.Dispose(); continue; }
                    string? exePath = proc.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                        if (exePath.StartsWith(winDir, StringComparison.OrdinalIgnoreCase)) { proc.Dispose(); continue; }
                    }
                    if (_whitelistService.IsWhitelisted(proc.ProcessName)) { proc.Dispose(); continue; }

                    // Hide Windows
                    var handles = new List<long>();
                    foreach (ProcessThread thread in proc.Threads)
                    {
                        EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                        {
                            if (IsWindowVisible(hWnd))
                            {
                                handles.Add(hWnd.ToInt64());
                                ShowWindow(hWnd, SW_HIDE);
                            }
                            return true;
                        }, IntPtr.Zero);
                    }

                    processesToFreeze.Add((proc, handles, appGroup, backupObj));
                }
                catch { }
            }
        }

        // IMPORTANT: Wait for Explorer to process the window hides so the taskbar doesn't hang
        if (processesToFreeze.Count > 0)
        {
            System.Threading.Thread.Sleep(250);
        }

        // Now "ghost" them safely by suspending all worker threads but keeping 1 UI thread alive
        foreach (var item in processesToFreeze)
        {
            try
            {
                uint uiThreadId = 0;
                try 
                {
                    if (item.proc.MainWindowHandle != IntPtr.Zero)
                    {
                        uiThreadId = GetWindowThreadProcessId(item.proc.MainWindowHandle, out _);
                    }
                } 
                catch { }

                var suspendedThisProc = new List<int>();

                foreach (ProcessThread pt in item.proc.Threads)
                {
                    // Fallback to find a UI thread if MainWindowHandle was missing
                    if (uiThreadId == 0)
                    {
                        EnumThreadWindows(pt.Id, (hWnd, lParam) =>
                        {
                            uiThreadId = (uint)pt.Id;
                            return false; // Found one, stop enumerating
                        }, IntPtr.Zero);
                    }

                    // Leave EXACTLY ONE UI thread alive to answer Explorer / Tray Icon messages
                    // This prevents the Windows Taskbar from hanging!
                    if (uiThreadId != 0 && pt.Id == uiThreadId)
                        continue;

                    IntPtr hThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pt.Id);
                    if (hThread != IntPtr.Zero)
                    {
                        SuspendThread(hThread);
                        CloseHandle(hThread);
                        suspendedThisProc.Add(pt.Id);
                    }
                }

                // Call NtResumeProcess in case it was fully suspended previously, just to be safe
                try { NtResumeProcess(item.proc.Handle); } catch { }

                try { EmptyWorkingSet(item.proc.Handle); } catch { }
                
                session.SuspendedCount++;
                if (item.backupObj != null)
                {
                    item.backupObj.HiddenWindowHandles.AddRange(item.handles);
                    item.backupObj.SuspendedThreadIds.AddRange(suspendedThisProc);
                }

                actualRamFreed += item.appGroup.WorkingSetBytes / item.appGroup.AllPids.Count;
            }
            catch { }
            finally
            {
                item.proc.Dispose();
            }
        }

        session.RamFreedBytes = actualRamFreed;

        // Save session to disk for crash recovery / auto-resume
        _sessionPersistenceService.SaveSession(session);

        return session;
    }

    /// <summary>
    /// Deactivates focus mode and wakes up all suspended applications.
    /// </summary>
    public ResumeResult DeactivateFocusMode(FocusSession session)
    {
        var result = new ResumeResult();

        foreach (var processData in session.SuspendedProcesses)
        {
            try
            {
                bool atLeastOneResumed = false;

                // 1. Try to resume the suspended PIDs
                foreach (var pid in processData.Pids)
                {
                    if (ResumeProcessByPid(pid, processData.Name, processData.HiddenWindowHandles, processData.SuspendedThreadIds))
                    {
                        atLeastOneResumed = true;
                    }
                }

                // 2. Fallback: If no PIDs could be resumed (e.g. process died), try relaunching
                if (!atLeastOneResumed)
                {
                    if (string.IsNullOrWhiteSpace(processData.ExePath) || !File.Exists(processData.ExePath))
                    {
                        result.FailedCount++;
                        result.FailedProcessNames.Add(processData.Name);
                        continue;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = processData.ExePath,
                        Arguments = processData.Arguments ?? "",
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(processData.ExePath),
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    };

                    Process.Start(startInfo);
                }
                
                result.RestoredCount++;
            }
            catch (Exception)
            {
                result.FailedCount++;
                result.FailedProcessNames.Add(processData.Name);
            }
        }

        // Clean up the session file
        _sessionPersistenceService.ClearSession();

        return result;
    }

    private void BackupProcessData(ProcessInfo appGroup, FocusSession session)
    {
        // Try to find the exe path and command line from at least one PID in the group
        foreach (var pid in appGroup.AllPids)
        {
            try
            {
                using var process = Process.GetProcessById(pid);
                string exePath = process.MainModule?.FileName ?? "";
                
                if (!string.IsNullOrWhiteSpace(exePath))
                {
                    string args = GetCommandLineArgsWmi(pid);
                    // Remove the exe path from the arguments string if present
                    if (!string.IsNullOrWhiteSpace(args) && args.Contains(exePath, StringComparison.OrdinalIgnoreCase))
                    {
                        args = args.Replace($"\"{exePath}\"", "").Trim();
                        args = args.Replace(exePath, "").Trim();
                    }

                    session.SuspendedProcesses.Add(new SuspendedProcessBackup
                    {
                        Name = appGroup.Name,
                        Pids = new List<int>(appGroup.AllPids),
                        ExePath = exePath,
                        Arguments = args,
                        WorkingSetBytes = appGroup.WorkingSetBytes
                    });
                    
                    return; // Only need one valid backup per app group
                }
            }
            catch
            {
                // Access denied or process exited, try next PID
            }
        }
        
        // If we got here, we couldn't back it up
        Debug.WriteLine($"[ProcessManager] Could not backup exe path for {appGroup.Name}");
    }

    [System.Runtime.InteropServices.DllImport("ntdll.dll", PreserveSig = false)]
    private static extern void NtSuspendProcess(IntPtr processHandle);

    [System.Runtime.InteropServices.DllImport("ntdll.dll", PreserveSig = false)]
    private static extern void NtResumeProcess(IntPtr processHandle);

    [System.Runtime.InteropServices.DllImport("psapi.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool EmptyWorkingSet(IntPtr hProcess);

    private delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);



    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private enum ThreadAccess : int
    {
        SUSPEND_RESUME = 0x0002
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint SuspendThread(IntPtr hThread);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint ResumeThread(IntPtr hThread);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const int SW_HIDE = 0;
    private const int SW_SHOWNOACTIVATE = 4; // Displays a window in its most recent size and position, without activating it.

    // SuspendProcessByPid was refactored directly into ActivateFocusMode for safety

    private bool ResumeProcessByPid(int pid, string expectedName, List<long> hiddenHandles, List<int> suspendedThreadIds)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            if (!proc.ProcessName.Equals(expectedName, StringComparison.OrdinalIgnoreCase)) return false;
            
            // Resume fully suspended process just in case
            try { NtResumeProcess(proc.Handle); } catch { }

            // Resume suspended threads
            foreach (var threadId in suspendedThreadIds)
            {
                IntPtr hThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)threadId);
                if (hThread != IntPtr.Zero)
                {
                    ResumeThread(hThread);
                    CloseHandle(hThread);
                }
            }

            // Restore specifically only the windows we explicitly hid,
            // without stealing focus (SW_SHOWNOACTIVATE)
            foreach (var handleVal in hiddenHandles)
            {
                IntPtr hWnd = new IntPtr(handleVal);
                ShowWindow(hWnd, SW_SHOWNOACTIVATE);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetCommandLineArgsWmi(int processId)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
            using var collection = searcher.Get();

            foreach (var obj in collection)
            {
                return obj["CommandLine"]?.ToString() ?? string.Empty;
            }
        }
        catch
        {
            // WMI can fail if we don't have admin rights or process already died
        }

        return string.Empty;
    }

    private HashSet<int> GetSafePids(List<string> focusAppNames)
    {
        var safePids = new HashSet<int>();
        var rootPids = new HashSet<int>();
        var focusAppsLower = new HashSet<string>(focusAppNames.Select(n => n.ToLowerInvariant()));
        
        var allProcesses = Process.GetProcesses();
        foreach (var p in allProcesses)
        {
            try
            {
                if (focusAppsLower.Contains(p.ProcessName.ToLowerInvariant()))
                {
                    rootPids.Add(p.Id);
                    safePids.Add(p.Id);
                }
            }
            catch { }
            finally { p.Dispose(); }
        }

        if (rootPids.Count == 0) return safePids;

        var parentToChildren = new Dictionary<int, List<int>>();
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process");
            using var collection = searcher.Get();
            foreach (var obj in collection)
            {
                var pidStr = obj["ProcessId"]?.ToString();
                var parentStr = obj["ParentProcessId"]?.ToString();
                
                if (int.TryParse(pidStr, out int childPid) && int.TryParse(parentStr, out int parentPid))
                {
                    if (!parentToChildren.TryGetValue(parentPid, out var list))
                    {
                        list = new List<int>();
                        parentToChildren[parentPid] = list;
                    }
                    list.Add(childPid);
                }
            }
        }
        catch { }

        var queue = new Queue<int>(rootPids);
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (parentToChildren.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    if (safePids.Add(child))
                    {
                        queue.Enqueue(child);
                    }
                }
            }
        }

        return safePids;
    }
}

