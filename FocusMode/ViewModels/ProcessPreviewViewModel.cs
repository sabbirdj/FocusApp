using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusMode.Models;
using FocusMode.Services;

namespace FocusMode.ViewModels;

public partial class ProcessPreviewViewModel : ObservableObject
{
    private readonly ProcessManager _processManager;
    private readonly NavigationService _navigationService;

    private List<string> _focusAppNames = new();

    [ObservableProperty]
    private long _totalRamToFree;

    [ObservableProperty]
    private int _processCount;

    public ObservableCollection<ProcessInfo> ProcessList { get; } = new();

    public ProcessPreviewViewModel(
        ProcessManager processManager,
        NavigationService navigationService)
    {
        _processManager = processManager;
        _navigationService = navigationService;
    }

    public void LoadProcesses(List<string> focusAppNames)
    {
        _focusAppNames = focusAppNames;
        ProcessList.Clear();

        var Hibernateable = _processManager.GetHibernateableProcesses(focusAppNames);
        long totalRam = 0;

        foreach (var p in Hibernateable)
        {
            p.IsSelected = true;
            ProcessList.Add(p);
            totalRam += p.WorkingSetBytes;
        }

        TotalRamToFree = totalRam;
        ProcessCount = Hibernateable.Count;
    }

    [RelayCommand]
    private void ToggleExclude(ProcessInfo process)
    {
        TotalRamToFree = ProcessList.Where(p => p.IsSelected).Sum(p => p.WorkingSetBytes);
        ProcessCount = ProcessList.Count(p => p.IsSelected);
    }

    [RelayCommand]
    private void Proceed()
    {
        var toHibernate = ProcessList.Where(p => p.IsSelected).ToList();
        var session = _processManager.ActivateFocusMode(toHibernate, _focusAppNames);
        _navigationService.NavigateTo(typeof(Pages.FocusActivePage), session);
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }
}


