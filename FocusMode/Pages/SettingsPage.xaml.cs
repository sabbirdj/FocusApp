using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FocusMode.ViewModels;

namespace FocusMode.Pages;

public sealed partial class SettingsPage : Page
{
    private SettingsViewModel _viewModel = null!;

    public SettingsPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        DataContext = _viewModel;
        _viewModel.LoadSettingsCommand.Execute(null);

        // Sync toggles
        LaunchAtStartupToggle.IsOn = _viewModel.LaunchAtStartup;
        ShowTrayIconToggle.IsOn = _viewModel.ShowTrayIcon;
        DryRunToggle.IsOn = _viewModel.DryRunPreview;
        AutoResumeToggle.IsOn = _viewModel.AutoResumeOnExit;

        WhitelistListView.ItemsSource = _viewModel.CustomWhitelist;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        var navService = App.Services.GetRequiredService<Services.NavigationService>();
        navService.GoBack();
    }

    private void LaunchAtStartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.LaunchAtStartup = LaunchAtStartupToggle.IsOn;
    }

    private void ShowTrayIconToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.ShowTrayIcon = ShowTrayIconToggle.IsOn;
    }

    private void DryRunToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.DryRunPreview = DryRunToggle.IsOn;
    }

    private void AutoResumeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.AutoResumeOnExit = AutoResumeToggle.IsOn;
    }

    private void AddWhitelist_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.NewWhitelistEntry = WhitelistEntryBox.Text;
        _viewModel.AddWhitelistEntryCommand.Execute(null);
        WhitelistEntryBox.Text = string.Empty;
    }

    private void RemoveWhitelist_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string entry)
        {
            _viewModel.RemoveWhitelistEntryCommand.Execute(entry);
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetToDefaultsCommand.Execute(null);

        // Refresh toggles
        LaunchAtStartupToggle.IsOn = _viewModel.LaunchAtStartup;
        ShowTrayIconToggle.IsOn = _viewModel.ShowTrayIcon;
        DryRunToggle.IsOn = _viewModel.DryRunPreview;
        AutoResumeToggle.IsOn = _viewModel.AutoResumeOnExit;
    }
}
