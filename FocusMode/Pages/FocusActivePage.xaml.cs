using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using FocusMode.Models;
using FocusMode.ViewModels;

namespace FocusMode.Pages;

public sealed partial class FocusActivePage : Page
{
    private FocusActiveViewModel _viewModel = null!;

    public FocusActivePage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        // Start pulse animations
        PulseStoryboard.Begin();
        ScaleStoryboard.Begin();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _viewModel = App.Services.GetRequiredService<FocusActiveViewModel>();
        DataContext = _viewModel;

        if (e.Parameter is FocusSession session)
        {
            _viewModel.StartSession(session);
        }

        // Bind UI
        UpdateUI();
        _viewModel.PropertyChanged += (s, args) =>
        {
            DispatcherQueue.TryEnqueue(UpdateUI);
        };
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        PulseStoryboard.Stop();
        ScaleStoryboard.Stop();
        _viewModel.Cleanup();
    }

    private void UpdateUI()
    {
        if (TimerText != null) TimerText.Text = _viewModel.ElapsedTimeDisplay;
        if (SuspendedCountText != null) SuspendedCountText.Text = _viewModel.SuspendedCount.ToString();
        if (RamFreedText != null) RamFreedText.Text = FormatBytes(_viewModel.RamFreed);
        if (FocusAppsList != null) FocusAppsList.ItemsSource = _viewModel.FocusApps;
        if (FailedBar != null) FailedBar.IsOpen = _viewModel.FailedCount > 0;
        if (FailedBar != null) FailedBar.Message = $"{_viewModel.FailedCount} processes could not be restored";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F0} MB";
        return $"{bytes / 1024.0:F0} KB";
    }

    private void EndFocusButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.EndFocusModeCommand.Execute(null);
    }
}

