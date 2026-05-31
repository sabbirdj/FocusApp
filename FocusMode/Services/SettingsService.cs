using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using FocusMode.Models;

namespace FocusMode.Services;

/// <summary>
/// Manages application settings persistence, loading from and saving to
/// %AppData%\FocusMode\settings.json. Supports auto-save with debounce.
/// </summary>
public sealed class SettingsService : IDisposable
{
    private static readonly string SettingsDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FocusMode");

    private static readonly string SettingsFilePath =
        Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly object _lock = new();
    private Timer? _debounceTimer;
    private bool _disposed;

    /// <summary>
    /// Gets the current settings instance.
    /// </summary>
    public AppSettings Settings { get; private set; }

    public SettingsService()
    {
        Settings = new AppSettings();
    }

    /// <summary>
    /// Loads settings from disk. Creates defaults on first run.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        Settings = loaded;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to load: {ex.Message}");
            }

            // First run — create defaults
            Settings = CreateDefaultSettings();
            Save();
        }
    }

    /// <summary>
    /// Persists settings to disk immediately.
    /// </summary>
    public void Save()
    {
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);
                string json = JsonSerializer.Serialize(Settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Failed to save: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Resets all settings to factory defaults.
    /// </summary>
    public void ResetToDefaults()
    {
        lock (_lock)
        {
            Settings = CreateDefaultSettings();
            Save();
        }
    }

    /// <summary>
    /// Schedules a debounced auto-save (500ms delay).
    /// </summary>
    public void ScheduleAutoSave()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ => Save(), null, 500, Timeout.Infinite);
    }

    private static AppSettings CreateDefaultSettings()
    {
        return new AppSettings
        {
            LaunchAtStartup = false,
            ShowTrayIcon = true,
            DryRunPreview = true,
            AutoResumeOnExit = true,
            CustomWhitelist = new List<string>()
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceTimer?.Dispose();
        Save();
    }
}
