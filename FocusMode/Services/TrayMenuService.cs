// Copyright (c) FocusMode. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using FocusMode.Models;

namespace FocusMode.Services;

/// <summary>
/// Builds the dynamic system-tray context menu. Renders different layouts
/// depending on whether Focus Mode is currently active or inactive.
/// Supports single-click quick focus and multi-select mode for focusing
/// on multiple applications at once.
/// </summary>
public class TrayMenuService
{
    private readonly ProcessManager _processManager;

    private bool _multiSelectMode;
    private readonly List<string> _selectedApps = new();

    /// <summary>
    /// Callback invoked when the user selects one or more apps for Quick Focus.
    /// The argument is the list of selected process names.
    /// </summary>
    public Action<List<string>>? OnQuickFocus { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks "End Focus Mode".
    /// </summary>
    public Action? OnEndFocus { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks "Open FocusMode" to show the main window.
    /// </summary>
    public Action? OnOpenApp { get; set; }

    /// <summary>
    /// Callback invoked when the user clicks "Exit" to close the application.
    /// </summary>
    public Action? OnExit { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayMenuService"/> class.
    /// </summary>
    /// <param name="processManager">
    /// The process manager used to enumerate running user-visible processes.
    /// </param>
    public TrayMenuService(ProcessManager processManager)
    {
        _processManager = processManager;
    }

    /// <summary>
    /// Builds and returns the tray icon context menu. The menu layout
    /// depends on whether Focus Mode is currently active.
    /// </summary>
    /// <param name="isFocusActive">
    /// <c>true</c> if Focus Mode is currently active; <c>false</c> otherwise.
    /// </param>
    /// <param name="currentSession">
    /// The current <see cref="FocusSession"/>, or <c>null</c> when inactive.
    /// </param>
    /// <param name="elapsed">
    /// The elapsed time since Focus Mode was activated, or <c>null</c> when inactive.
    /// </param>
    /// <returns>A fully constructed <see cref="MenuFlyout"/> ready for display.</returns>
    public MenuFlyout BuildMenu(bool isFocusActive, FocusSession? currentSession, TimeSpan? elapsed)
    {
        // Reset multi-select state each time the menu is rebuilt
        _multiSelectMode = false;
        _selectedApps.Clear();

        return isFocusActive
            ? BuildActiveMenu(currentSession!, elapsed!.Value)
            : BuildInactiveMenu();
    }

    /// <summary>
    /// Builds the menu shown when Focus Mode is NOT active.
    /// Lists running apps the user can click to quick-focus.
    /// </summary>
    private MenuFlyout BuildInactiveMenu()
    {
        var menu = new MenuFlyout();

        // ── Header ──
        var header = new MenuFlyoutItem
        {
            Text = "⚡ Quick Focus",
            IsEnabled = false
        };
        menu.Items.Add(header);
        menu.Items.Add(new MenuFlyoutSeparator());

        // ── Running apps ──
        var processes = _processManager.GetWindowedApps();

        foreach (var process in processes)
        {
            var appItem = new MenuFlyoutItem
            {
                Text = $"{process.DisplayName}  ({process.FormattedMemory})"
            };

            // Single-click: quick-focus on this app alone
            var processName = process.Name;
            appItem.Click += (_, _) =>
            {
                OnQuickFocus?.Invoke(new List<string> { processName });
            };

            // "Add More" sub-item for multi-select
            var subItem = new MenuFlyoutSubItem
            {
                Text = $"{process.DisplayName}  ({process.FormattedMemory})"
            };

            var addMoreItem = new MenuFlyoutItem { Text = "➕ Add to Focus" };
            var capturedName = process.Name;
            addMoreItem.Click += (_, _) =>
            {
                _multiSelectMode = true;
                if (!_selectedApps.Contains(capturedName))
                {
                    _selectedApps.Add(capturedName);
                }
            };
            subItem.Items.Add(addMoreItem);

            menu.Items.Add(appItem);
        }

        // ── Multi-select launch button (appears when apps are selected) ──
        if (_multiSelectMode && _selectedApps.Count > 0)
        {
            menu.Items.Add(new MenuFlyoutSeparator());

            var launchItem = new MenuFlyoutItem
            {
                Text = $"▶ Focus Selected ({_selectedApps.Count})"
            };
            launchItem.Click += (_, _) =>
            {
                OnQuickFocus?.Invoke(new List<string>(_selectedApps));
            };
            menu.Items.Add(launchItem);
        }

        // ── Footer ──
        menu.Items.Add(new MenuFlyoutSeparator());

        var openItem = new MenuFlyoutItem { Text = "Open FocusMode" };
        openItem.Click += (_, _) => OnOpenApp?.Invoke();
        menu.Items.Add(openItem);

        var exitItem = new MenuFlyoutItem { Text = "Exit" };
        exitItem.Click += (_, _) => OnExit?.Invoke();
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// Builds the menu shown when Focus Mode IS active.
    /// Displays status, elapsed time, focused apps, and an end button.
    /// </summary>
    /// <param name="session">The currently active <see cref="FocusSession"/>.</param>
    /// <param name="elapsed">Time elapsed since Focus Mode was activated.</param>
    private MenuFlyout BuildActiveMenu(FocusSession session, TimeSpan elapsed)
    {
        var menu = new MenuFlyout();

        // ── Status header with elapsed time ──
        var statusHeader = new MenuFlyoutItem
        {
            Text = $"🟢 Focus Active — {FormatElapsed(elapsed)}",
            IsEnabled = false
        };
        menu.Items.Add(statusHeader);

        // ── End Focus button ──
        var endItem = new MenuFlyoutItem { Text = "⏹ End Focus Mode" };
        endItem.Click += (_, _) => OnEndFocus?.Invoke();
        menu.Items.Add(endItem);

        menu.Items.Add(new MenuFlyoutSeparator());

        // ── List of focus apps ──
        if (session.FocusApps.Count > 0)
        {
            var focusHeader = new MenuFlyoutItem
            {
                Text = "📌 Focus Apps:",
                IsEnabled = false
            };
            menu.Items.Add(focusHeader);

            foreach (var appName in session.FocusApps)
            {
                var appItem = new MenuFlyoutItem
                {
                    Text = $"  • {appName}",
                    IsEnabled = false
                };
                menu.Items.Add(appItem);
            }

            menu.Items.Add(new MenuFlyoutSeparator());
        }

        // ── Kill count ──
        var killedInfo = new MenuFlyoutItem
        {
            Text = $"💀 {session.SuspendedCount} apps killed • {FormatBytes(session.RamFreedBytes)} freed",
            IsEnabled = false
        };
        menu.Items.Add(killedInfo);

        menu.Items.Add(new MenuFlyoutSeparator());

        // ── Footer ──
        var openItem = new MenuFlyoutItem { Text = "Open FocusMode" };
        openItem.Click += (_, _) => OnOpenApp?.Invoke();
        menu.Items.Add(openItem);

        var exitItem = new MenuFlyoutItem { Text = "Exit" };
        exitItem.Click += (_, _) => OnExit?.Invoke();
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> into a human-friendly elapsed string
    /// (e.g. "1h 23m", "45m", "30s").
    /// </summary>
    /// <param name="elapsed">The elapsed time to format.</param>
    /// <returns>A formatted elapsed-time string.</returns>
    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
        {
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes:D2}m";
        }

        if (elapsed.TotalMinutes >= 1)
        {
            return $"{(int)elapsed.TotalMinutes}m";
        }

        return $"{(int)elapsed.TotalSeconds}s";
    }

    /// <summary>
    /// Formats bytes into a human-readable string (e.g. "1.2 GB", "340 MB").
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F0} MB";
        return $"{bytes / 1024.0:F0} KB";
    }
}

