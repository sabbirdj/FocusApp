using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace FocusMode.Services;

/// <summary>
/// Polls system statistics (RAM usage, process count) on a periodic timer.
/// Exposes observable properties for data binding.
/// </summary>
public class SystemStatsService : INotifyPropertyChanged, IDisposable
{
    private readonly System.Timers.Timer _timer;
    private long _totalRam;
    private long _usedRam;
    private long _availableRam;
    private int _activeProcessCount;
    private double _ramUsagePercent;

    public event PropertyChangedEventHandler? PropertyChanged;

    public long TotalRam
    {
        get => _totalRam;
        private set { _totalRam = value; OnPropertyChanged(); }
    }

    public long UsedRam
    {
        get => _usedRam;
        private set { _usedRam = value; OnPropertyChanged(); }
    }

    public long AvailableRam
    {
        get => _availableRam;
        private set { _availableRam = value; OnPropertyChanged(); }
    }

    public int ActiveProcessCount
    {
        get => _activeProcessCount;
        private set { _activeProcessCount = value; OnPropertyChanged(); }
    }

    public double RamUsagePercent
    {
        get => _ramUsagePercent;
        private set { _ramUsagePercent = value; OnPropertyChanged(); }
    }

    public SystemStatsService()
    {
        _timer = new System.Timers.Timer(1000); // 1 second interval
        _timer.Elapsed += (_, _) => UpdateStats();
        UpdateStats(); // Initial read
    }

    /// <summary>
    /// Start polling system stats.
    /// </summary>
    public void Start()
    {
        _timer.Start();
    }

    /// <summary>
    /// Stop polling system stats.
    /// </summary>
    public void Stop()
    {
        _timer.Stop();
    }

    private void UpdateStats()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                TotalRam = (long)memStatus.ullTotalPhys;
                AvailableRam = (long)memStatus.ullAvailPhys;
                UsedRam = TotalRam - AvailableRam;
                RamUsagePercent = TotalRam > 0 ? (double)UsedRam / TotalRam * 100.0 : 0;
            }

            ActiveProcessCount = Process.GetProcesses().Length;
        }
        catch (Exception)
        {
            // Silently handle any errors during stats collection
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }

    #region P/Invoke

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    #endregion
}
