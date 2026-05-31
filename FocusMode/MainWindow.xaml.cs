using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using FocusMode.Services;

namespace FocusMode;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Set custom title bar
        ExtendsContentIntoTitleBar = true;

        // Set up navigation service
        var navService = App.Services.GetRequiredService<NavigationService>();
        navService.Frame = NavFrame;

        // Intercept close to minimize to tray
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(900, 900));
        
        appWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            appWindow.Hide();
        };

        // Navigate to dashboard
        NavFrame.Navigate(typeof(Pages.DashboardPage));
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        NavFrame.Navigate(typeof(Pages.SettingsPage));
    }
}
