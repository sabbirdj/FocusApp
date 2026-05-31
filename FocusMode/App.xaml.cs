using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;
using FocusMode.Services;
using FocusMode.ViewModels;
using H.NotifyIcon;

namespace FocusMode;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// Configures DI container, system tray, and handles crash recovery on startup.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private TaskbarIcon? _trayIcon;

    /// <summary>
    /// Gets the DI service provider for the application.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets the current App instance.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Gets the main window instance.
    /// </summary>
    public static MainWindow? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        
        // Configure DI
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        // Start system stats polling
        var statsService = Services.GetRequiredService<SystemStatsService>();
        statsService.Start();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services (singletons)
        services.AddSingleton<SettingsService>();
        services.AddSingleton<WhitelistService>();
        services.AddSingleton<SessionPersistenceService>();
        services.AddSingleton<ProcessManager>();
        services.AddSingleton<SystemStatsService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<TrayMenuService>();

        // ViewModels (SINGLETON — state persists across page navigations)
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ProcessPreviewViewModel>();
        services.AddSingleton<FocusActiveViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Load settings
        var settingsService = Services.GetRequiredService<SettingsService>();
        settingsService.Load();

        _window = new MainWindow();
        MainWindow = (MainWindow)_window;
        _window.Activate();

        // Configure tray menu callbacks
        var trayMenuService = Services.GetRequiredService<TrayMenuService>();
        trayMenuService.OnOpenApp = ShowWindow;
        trayMenuService.OnExit = () =>
        {
            // Auto-restore if configured
            if (settingsService.Settings.AutoResumeOnExit)
            {
                var sessionService = Services.GetRequiredService<SessionPersistenceService>();
                var session = sessionService.LoadSession();
                if (session != null)
                {
                    var processManager = Services.GetRequiredService<ProcessManager>();
                    processManager.DeactivateFocusMode(session);
                }
            }

            _trayIcon?.Dispose();
            Application.Current.Exit();
        };

        // Setup system tray icon with context menu
        SetupTrayIcon();

        // Check for crashed session
        var sessionPersistence = Services.GetRequiredService<SessionPersistenceService>();
        if (sessionPersistence.HasCrashedSession())
        {
            _ = HandleCrashedSessionAsync(sessionPersistence);
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "FocusMode — Right-click for menu"
        };

        // Set the icon from app resources
        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (System.IO.File.Exists(iconPath))
        {
            _trayIcon.Icon = new System.Drawing.Icon(iconPath);
        }

        // Left click: show/activate window
        _trayIcon.LeftClickCommand = new RelayCommand(ShowWindow);

        // Double click: show/activate window
        _trayIcon.DoubleClickCommand = new RelayCommand(ShowWindow);

        // Right-click context menu
        var contextMenu = new MenuFlyout();
        _trayIcon.ContextFlyout = contextMenu;
        _trayIcon.ForceCreate();

        // Build it initially
        BuildTrayMenu(contextMenu);

        // Rebuild on right click
        _trayIcon.RightClickCommand = new RelayCommand(() => BuildTrayMenu(contextMenu));
    }

    private void BuildTrayMenu(MenuFlyout contextMenu)
    {
        contextMenu.Items.Clear();

        var openItem = new MenuFlyoutItem
        {
            Text = "Open FocusMode",
            Icon = new FontIcon { Glyph = "\uE8A7" },
            Command = new RelayCommand(ShowWindow)
        };
        contextMenu.Items.Add(openItem);

        contextMenu.Items.Add(new MenuFlyoutSeparator());

        var sessionService = Services.GetRequiredService<SessionPersistenceService>();
        var processManager = Services.GetRequiredService<ProcessManager>();
        
        if (sessionService.LoadSession() != null)
        {
            // Focus Mode is active
            var stopItem = new MenuFlyoutItem
            {
                Text = "Restore / Stop Focus",
                Icon = new FontIcon { Glyph = "\uE71A" },
                Command = new RelayCommand(() => 
                {
                    var vm = Services.GetRequiredService<FocusActiveViewModel>();
                    vm.EndFocusModeCommand.Execute(null);
                    ShowWindow(); // Bring app to foreground
                })
            };
            contextMenu.Items.Add(stopItem);
        }
        else
        {
            // Focus Mode is not active
            var startItem = new MenuFlyoutSubItem
            {
                Text = "Start Focus",
                Icon = new FontIcon { Glyph = "\uE768" }
            };
            
            // Populate with windowed apps
            var apps = processManager.GetWindowedApps();
            foreach (var app in apps)
            {
                var appItem = new MenuFlyoutItem 
                { 
                    Text = app.DisplayName,
                    Command = new RelayCommand(() => 
                    {
                        var focusApps = new System.Collections.Generic.List<string> { app.Name };
                        var killable = processManager.GetHibernateableProcesses(focusApps);
                        processManager.ActivateFocusMode(killable, focusApps);
                    })
                };
                startItem.Items.Add(appItem);
            }

            if (apps.Count == 0)
            {
                var emptyItem = new MenuFlyoutItem { Text = "No running apps", IsEnabled = false };
                startItem.Items.Add(emptyItem);
            }
            
            contextMenu.Items.Add(startItem);
        }

        contextMenu.Items.Add(new MenuFlyoutSeparator());

        var exitItem = new MenuFlyoutItem
        {
            Text = "Exit App",
            Icon = new FontIcon { Glyph = "\uE711" },
            Command = new RelayCommand(() =>
            {
                var settingsService = Services.GetRequiredService<SettingsService>();
                if (settingsService.Settings.AutoResumeOnExit)
                {
                    var session = sessionService.LoadSession();
                    if (session != null)
                    {
                        processManager.DeactivateFocusMode(session);
                        System.Threading.Thread.Sleep(500); // Allow time for apps to start
                    }
                }

                _trayIcon?.Dispose();
                Application.Current.Exit();
            })
        };
        contextMenu.Items.Add(exitItem);
    }

    private void ShowWindow()
    {
        if (_window == null) return;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Show();
        _window.Activate();
    }

    private async System.Threading.Tasks.Task HandleCrashedSessionAsync(SessionPersistenceService sessionService)
    {
        // Wait a bit for the window to fully load
        await System.Threading.Tasks.Task.Delay(1500);

        if (MainWindow?.Content?.XamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = "⚠️ Session Recovery",
            Content = "A previous Focus session was not properly closed. " +
                      "Some apps were killed and not restored.\n\n" +
                      "Re-launch all killed apps now?",
            PrimaryButtonText = "Restore All",
            CloseButtonText = "Dismiss",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = MainWindow.Content.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var session = sessionService.LoadSession();
            if (session != null)
            {
                var processManager = Services.GetRequiredService<ProcessManager>();
                processManager.DeactivateFocusMode(session);
            }
        }
        else
        {
            sessionService.ClearSession();
        }
    }
}

