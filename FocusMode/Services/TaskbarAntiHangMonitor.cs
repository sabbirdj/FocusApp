using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FocusMode.Services;

public class TaskbarAntiHangMonitor
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("ntdll.dll", PreserveSig = false)]
    private static extern void NtSuspendProcess(IntPtr processHandle);

    [DllImport("ntdll.dll", PreserveSig = false)]
    private static extern void NtResumeProcess(IntPtr processHandle);

    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const uint WM_NULL = 0x0000;

    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    public void Start(Func<List<int>> getSuspendedPids)
    {
        Stop();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _monitorTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(500, token);
                
                try
                {
                    IntPtr trayWnd = FindWindow("Shell_TrayWnd", null);
                    if (trayWnd != IntPtr.Zero)
                    {
                        IntPtr result;
                        IntPtr ret = SendMessageTimeout(trayWnd, WM_NULL, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, 50, out result);
                        
                        if (ret == IntPtr.Zero)
                        {
                            var pids = getSuspendedPids();
                            
                            foreach (var pid in pids)
                            {
                                try
                                {
                                    using var proc = Process.GetProcessById(pid);
                                    NtResumeProcess(proc.Handle);
                                } catch { }
                            }

                            await Task.Delay(50, token);

                            foreach (var pid in pids)
                            {
                                try
                                {
                                    using var proc = Process.GetProcessById(pid);
                                    NtSuspendProcess(proc.Handle);
                                } catch { }
                            }
                        }
                    }
                }
                catch { }
            }
        }, token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }
}
