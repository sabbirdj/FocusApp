using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusMode.Models;
using FocusMode.Services;
using Microsoft.UI.Xaml;

namespace FocusMode.ViewModels;

public partial class FocusActiveViewModel : ObservableObject
{
    private readonly ProcessManager _processManager;
    private readonly NavigationService _navigationService;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly SystemStatsService _statsService;
    private FocusSession? _currentSession;
    private DispatcherTimer? _timer;

    [ObservableProperty]
    private TimeSpan _elapsedTime;

    [ObservableProperty]
    private string _elapsedTimeDisplay = "00:00:00";

    [ObservableProperty]
    private int _SuspendedCount;

    [ObservableProperty]
    private long _ramFreed;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private int _remainingProcessCount;

    [ObservableProperty]
    private long _remainingRamUsed;

    [ObservableProperty]
    private string _formattedRemainingRam = "0 KB";

    public ObservableCollection<string> FocusApps { get; } = new();
    public ObservableCollection<string> FailedProcesses { get; } = new();

    public FocusSession? CurrentSession => _currentSession;

    public FocusActiveViewModel(
        ProcessManager processManager,
        NavigationService navigationService,
        DashboardViewModel dashboardViewModel,
        SystemStatsService statsService)
    {
        _processManager = processManager;
        _navigationService = navigationService;
        _dashboardViewModel = dashboardViewModel;
        _statsService = statsService;
    }

    public void StartSession(FocusSession session)
    {
        _currentSession = session;
        SuspendedCount = session.SuspendedCount;
        RamFreed = session.RamFreedBytes;
        FailedCount = 0;

        FocusApps.Clear();
        foreach (var app in session.FocusApps)
            FocusApps.Add(app);

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) =>
        {
            ElapsedTime = DateTime.UtcNow - session.StartTime;
            ElapsedTimeDisplay = ElapsedTime.ToString(@"hh\:mm\:ss");
            
            RemainingProcessCount = _statsService.ActiveProcessCount;
            RemainingRamUsed = _statsService.UsedRam;
            FormattedRemainingRam = FormatBytes(RemainingRamUsed);
        };
        _timer.Start();
    }

    [RelayCommand]
    private void EndFocusMode()
    {
        _timer?.Stop();
        _timer = null;

        if (_currentSession != null)
        {
            var result = _processManager.DeactivateFocusMode(_currentSession);
            FailedCount = result.FailedCount;

            FailedProcesses.Clear();
            foreach (var name in result.FailedProcessNames)
                FailedProcesses.Add(name);
        }

        _currentSession = null;

        // Reset dashboard so user can start again
        _dashboardViewModel.Reset();
        _navigationService.NavigateTo(typeof(Pages.DashboardPage));
    }

    public void Cleanup()
    {
        _timer?.Stop();
        _timer = null;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F0} MB";
        return $"{bytes / 1024.0:F0} KB";
    }
}


