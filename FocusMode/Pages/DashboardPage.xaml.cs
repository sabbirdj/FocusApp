using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using FocusMode.Models;
using FocusMode.ViewModels;

namespace FocusMode.Pages;

public sealed partial class DashboardPage : Page
{
    private DashboardViewModel _viewModel = null!;

    public DashboardPage()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel = App.Services.GetRequiredService<DashboardViewModel>();
        DataContext = _viewModel;
        _viewModel.LoadDataCommand.Execute(null);

        // Subscribe to stats updates
        _viewModel.PropertyChanged += (s, args) =>
        {
            DispatcherQueue.TryEnqueue(UpdateStats);
        };

        // Subscribe to collection changes
        _viewModel.WindowedApps.CollectionChanged += (s, args) => DispatcherQueue.TryEnqueue(UpdateUI);
        _viewModel.BackgroundProcesses.CollectionChanged += (s, args) => DispatcherQueue.TryEnqueue(UpdateUI);
        _viewModel.SelectedFocusApps.CollectionChanged += (s, args) => DispatcherQueue.TryEnqueue(UpdateUI);

        UpdateStats();
        UpdateUI();
    }

    private void UpdateStats()
    {
        if (RamUsedText != null)
            RamUsedText.Text = $"{_viewModel.RamUsed / 1_073_741_824.0:F1}";
        if (ProcessCountText != null)
            ProcessCountText.Text = _viewModel.ProcessCount.ToString();
        if (RamProgressBar != null)
            RamProgressBar.Value = _viewModel.RamUsagePercent;
    }

    private void UpdateUI()
    {
        // Toggle visibility
        bool scanned = _viewModel.IsScanned;
        PreScanPlaceholder.Visibility = scanned ? Visibility.Collapsed : Visibility.Visible;
        WindowedSection.Visibility = scanned ? Visibility.Visible : Visibility.Collapsed;
        BackgroundSection.Visibility = scanned ? Visibility.Visible : Visibility.Collapsed;

        // Update counts
        WindowedCountRun.Text = scanned ? $" ({_viewModel.WindowedApps.Count})" : "";
        BackgroundCountRun.Text = scanned ? $" ({_viewModel.BackgroundProcesses.Count})" : "";
        SelectedCountText.Text = _viewModel.SelectedFocusApps.Count.ToString();

        // Bind lists
        WindowedListView.ItemsSource = _viewModel.WindowedApps;
        BackgroundListView.ItemsSource = _viewModel.BackgroundProcesses;

        // Enable/disable activate button
        ActivateButton.IsEnabled = _viewModel.SelectedFocusApps.Count > 0;
    }

    private void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ScanProcessesCommand.Execute(null);
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (_viewModel != null)
        {
            _viewModel.SearchText = sender.Text;
        }
    }

    private void ProcessCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is ProcessInfo process)
        {
            _viewModel.ToggleProcessSelectionCommand.Execute(process);
        }
    }

    private void ActivateButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ActivateFocusCommand.Execute(null);
    }
}
