using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusMode.Models;
using FocusMode.Services;

namespace FocusMode.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ProcessManager _processManager;
    private readonly SystemStatsService _statsService;
    private readonly NavigationService _navigationService;
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private long _ramUsed;

    [ObservableProperty]
    private long _ramTotal;

    [ObservableProperty]
    private double _ramUsagePercent;

    [ObservableProperty]
    private int _processCount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isScanned;

    // Two separate lists for the UI
    public ObservableCollection<ProcessInfo> WindowedApps { get; } = new();
    public ObservableCollection<ProcessInfo> BackgroundProcesses { get; } = new();
    public ObservableCollection<ProcessInfo> SelectedFocusApps { get; } = new();

    public DashboardViewModel(
        ProcessManager processManager,
        SystemStatsService statsService,
        NavigationService navigationService,
        SettingsService settingsService)
    {
        _processManager = processManager;
        _statsService = statsService;
        _navigationService = navigationService;
        _settingsService = settingsService;

        _statsService.PropertyChanged += (s, e) =>
        {
            RamUsed = _statsService.UsedRam;
            RamTotal = _statsService.TotalRam;
            RamUsagePercent = _statsService.RamUsagePercent;
            ProcessCount = _statsService.ActiveProcessCount;
        };
    }

    [RelayCommand]
    private void LoadData()
    {
        RamUsed = _statsService.UsedRam;
        RamTotal = _statsService.TotalRam;
        RamUsagePercent = _statsService.RamUsagePercent;
        ProcessCount = _statsService.ActiveProcessCount;
    }

    /// <summary>
    /// Scan button clicked — populate both process lists.
    /// </summary>
    [RelayCommand]
    private void ScanProcesses()
    {
        WindowedApps.Clear();
        BackgroundProcesses.Clear();

        var windowed = _processManager.GetWindowedApps();
        foreach (var p in windowed)
        {
            if (string.IsNullOrEmpty(SearchText) ||
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                WindowedApps.Add(p);
            }
        }

        var background = _processManager.GetBackgroundProcesses();
        foreach (var p in background)
        {
            if (string.IsNullOrEmpty(SearchText) ||
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            {
                BackgroundProcesses.Add(p);
            }
        }

        IsScanned = true;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (IsScanned) ScanProcesses();
    }

    [RelayCommand]
    private void ToggleProcessSelection(ProcessInfo process)
    {
        if (process.IsSelected)
        {
            // Was just checked — add to focus apps
            if (!SelectedFocusApps.Any(p => p.Name.Equals(process.Name, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedFocusApps.Add(process);
            }
        }
        else
        {
            // Was unchecked — remove from focus apps
            var existing = SelectedFocusApps.FirstOrDefault(p => p.Name.Equals(process.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                SelectedFocusApps.Remove(existing);
        }
        ActivateFocusCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanActivateFocus))]
    private void ActivateFocus()
    {
        var focusAppNames = SelectedFocusApps.Select(p => p.Name).ToList();

        if (_settingsService.Settings.DryRunPreview)
        {
            _navigationService.NavigateTo(typeof(Pages.ProcessPreviewPage), focusAppNames);
        }
        else
        {
            var Hibernateable = _processManager.GetHibernateableProcesses(focusAppNames);
            foreach (var p in Hibernateable) p.IsSelected = true;
            var session = _processManager.ActivateFocusMode(Hibernateable, focusAppNames);
            _navigationService.NavigateTo(typeof(Pages.FocusActivePage), session);
        }
    }

    private bool CanActivateFocus() => SelectedFocusApps.Count > 0;

    /// <summary>
    /// Called when returning from FocusActivePage to reset state.
    /// </summary>
    public void Reset()
    {
        SelectedFocusApps.Clear();
        WindowedApps.Clear();
        BackgroundProcesses.Clear();
        IsScanned = false;
        SearchText = string.Empty;
        ActivateFocusCommand.NotifyCanExecuteChanged();
    }
}

