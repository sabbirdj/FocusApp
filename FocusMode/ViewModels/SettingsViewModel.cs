using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusMode.Services;

namespace FocusMode.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private bool _launchAtStartup;

    [ObservableProperty]
    private bool _showTrayIcon;

    [ObservableProperty]
    private bool _dryRunPreview;

    [ObservableProperty]
    private bool _autoResumeOnExit;

    [ObservableProperty]
    private string _newWhitelistEntry = string.Empty;

    public ObservableCollection<string> CustomWhitelist { get; } = new();

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [RelayCommand]
    private void LoadSettings()
    {
        var settings = _settingsService.Settings;
        LaunchAtStartup = settings.LaunchAtStartup;
        ShowTrayIcon = settings.ShowTrayIcon;
        DryRunPreview = settings.DryRunPreview;
        AutoResumeOnExit = settings.AutoResumeOnExit;

        CustomWhitelist.Clear();
        foreach (var entry in settings.CustomWhitelist)
            CustomWhitelist.Add(entry);
    }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        _settingsService.Settings.LaunchAtStartup = value;
        _settingsService.Save();

        try 
        {
            string startupPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Startup);
            string shortcutPath = System.IO.Path.Combine(startupPath, "FocusMode.lnk");
            
            if (value)
            {
                System.Type shellType = System.Type.GetTypeFromProgID("WScript.Shell");
                dynamic wshShell = System.Activator.CreateInstance(shellType);
                dynamic shortcut = wshShell.CreateShortcut(shortcutPath);
                string exePath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "FocusMode.exe");
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = System.AppContext.BaseDirectory;
                shortcut.Save();
            }
            else
            {
                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                }
            }
        }
        catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }

    partial void OnShowTrayIconChanged(bool value)
    {
        _settingsService.Settings.ShowTrayIcon = value;
        _settingsService.Save();
    }

    partial void OnDryRunPreviewChanged(bool value)
    {
        _settingsService.Settings.DryRunPreview = value;
        _settingsService.Save();
    }

    partial void OnAutoResumeOnExitChanged(bool value)
    {
        _settingsService.Settings.AutoResumeOnExit = value;
        _settingsService.Save();
    }

    [RelayCommand]
    private void AddWhitelistEntry()
    {
        if (!string.IsNullOrWhiteSpace(NewWhitelistEntry))
        {
            var entry = NewWhitelistEntry.Trim().ToLowerInvariant();
            if (!CustomWhitelist.Contains(entry))
            {
                _settingsService.Settings.CustomWhitelist.Add(entry);
                _settingsService.Save();
                CustomWhitelist.Add(entry);
            }
            NewWhitelistEntry = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveWhitelistEntry(string entry)
    {
        _settingsService.Settings.CustomWhitelist.Remove(entry);
        _settingsService.Save();
        CustomWhitelist.Remove(entry);
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        _settingsService.ResetToDefaults();
        LoadSettings();
    }
}

