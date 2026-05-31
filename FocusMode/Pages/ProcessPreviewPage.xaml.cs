using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using FocusMode.Models;
using FocusMode.ViewModels;

namespace FocusMode.Pages;

public sealed partial class ProcessPreviewPage : Page
{
    private ProcessPreviewViewModel _viewModel = null!;

    public ProcessPreviewPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _viewModel = App.Services.GetRequiredService<ProcessPreviewViewModel>();
        DataContext = _viewModel;

        if (e.Parameter is List<string> focusApps)
        {
            _viewModel.LoadProcesses(focusApps);
        }

        ProcessListView.ItemsSource = _viewModel.ProcessList;
        UpdateStats();
        _viewModel.PropertyChanged += (s, args) =>
        {
            DispatcherQueue.TryEnqueue(UpdateStats);
        };
    }

    private void UpdateStats()
    {
        if (HeaderText != null)
            HeaderText.Text = $"{_viewModel.ProcessCount} processes will be killed";
        if (RamText != null)
            RamText.Text = $"{FormatBytes(_viewModel.TotalRamToFree)} RAM will be freed";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F0} MB";
        return $"{bytes / 1024.0:F0} KB";
    }

    private void ExcludeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is ProcessInfo process)
        {
            _viewModel.ToggleExcludeCommand.Execute(process);
        }
    }

    private void ProceedButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ProceedCommand.Execute(null);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CancelCommand.Execute(null);
    }
}
