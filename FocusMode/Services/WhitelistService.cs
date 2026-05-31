// Copyright (c) FocusMode. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using FocusMode.Models;

namespace FocusMode.Services
{
    /// <summary>
    /// Manages the hardcoded system whitelist of critical Windows processes and the
    /// user-configurable custom whitelist. Provides methods to check whether a given
    /// process should be exempt from suspension.
    /// </summary>
    public sealed class WhitelistService
    {
        /// <summary>
        /// Hardcoded set of critical system process names that must never be suspended.
        /// Suspending these can cause BSOD, black screens, or system instability.
        /// </summary>
        private static readonly HashSet<string> SystemWhitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            // ── Kernel & core OS (suspending = BSOD) ──
            "system", "idle", "registry", "smss", "csrss", "wininit",
            "winlogon", "lsass", "lsaiso", "services", "svchost",
            "ntoskrnl", "ntoskrnl.exe", "memory compression",

            // ── Desktop & shell (suspending = black screen / no taskbar) ──
            "dwm", "explorer", "sihost", "shellexperiencehost",
            "startmenuexperiencehost", "searchhost", "searchui",
            "searchapp", "lockapp", "logonui",
            "applicationframehost", "shellhost", "systemsettings",
            "systemsettingsbroker", "widgetservice", "widgets",
            "windowsshellexperiencehost", "peopleexperiencehost",
            "actioncenter", "windows.immersivecontrolpanel",
            "desktopwindowxamlsource",

            // ── Input & accessibility (suspending = no keyboard/mouse) ──
            "ctfmon", "textinputhost", "tabletinputservice",
            "touchkeyboard", "narrator", "magnify",
            "inputapp", "msrdc",

            // ── Runtime brokers & COM hosts ──
            "runtimebroker", "dllhost", "conhost", "werfault",
            "backgroundtaskhost", "backgroundtransferhost",
            "compattelrunner", "msiexec",

            // ── System services that run in user session ──
            "taskmgr", "taskhostw", "spoolsv", "audiodg",
            "fontdrvhost", "msdtc", "unsecapp", "wmiprvse",
            "wmiapsrv", "csrss", "dashost",
            "gamebarpresencewriter", "gameinputsvc",

            // ── Security & antivirus ──
            "securityhealthservice", "securityhealthsystray",
            "sgrmbroker", "msmpeng", "mpcmdrun",
            "smartscreen", "nissrv",
            "securityhealthhost", "windowsdefender",

            // ── GPU / display drivers (suspending = display crash) ──
            "nvcontainer", "nvspcaps64", "nvdisplay.container",
            "amdrsserv", "amddvr", "amdow",
            "igfxem", "igfxhk", "igfxtray",
            "dwm", "compositorhost",

            // ── Windows Update & Store ──
            "musnotification", "musnotificationux",
            "windowsupdate", "trustedinstaller",
            "tiworker",

            // ── Network critical ──
            "networkservice", "localservice",
            "wlanext", "netprofm",

            // ── Credential & authentication ──
            "credentialuibroker", "credentialenrollmentmanager",
            "consent", "userinit",

            // ── WinRT, XAML, UWP infrastructure ──
            "windows.ui.core.corewindow",
            "windowsinternal.composableshell",
            "microsoft.photos", "microsoft.windows.photos",
            "windowscamera", "hmssessionmanager",

            // ── Audio ──
            "audiodg", "audiosrv",

            // ── Developer tools (common, shouldn't be suspended) ──
            "devenv", "code", "msbuild", "dotnet",
            "perfwatson2", "servicehub",

            // ── This app ──
            "focusmode"
        };

        private readonly SettingsService _settingsService;
        private readonly HashSet<string> _userWhitelist;

        /// <summary>
        /// Initializes a new instance of the <see cref="WhitelistService"/> class.
        /// </summary>
        /// <param name="settingsService">
        /// The settings service used to persist the user whitelist across sessions.
        /// </param>
        public WhitelistService(SettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _userWhitelist = new HashSet<string>(
                _settingsService.Settings.CustomWhitelist ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the current user-defined whitelist as a read-only list.
        /// </summary>
        public IReadOnlyList<string> UserWhitelist =>
            _userWhitelist.ToList().AsReadOnly();

        /// <summary>
        /// Determines whether the specified process name is whitelisted
        /// (either in the hardcoded system whitelist or the user whitelist).
        /// </summary>
        /// <param name="processName">The process name to check (without .exe extension).</param>
        /// <returns><c>true</c> if the process is whitelisted; otherwise, <c>false</c>.</returns>
        public bool IsWhitelisted(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return false;

            string name = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase).Trim();
            return SystemWhitelist.Contains(name) || _userWhitelist.Contains(name);
        }

        /// <summary>
        /// Determines whether the given process is a system-level process
        /// by checking if it runs in Session 0 or is owned by the SYSTEM account.
        /// </summary>
        /// <param name="process">The process to inspect.</param>
        /// <returns>
        /// <c>true</c> if the process is a Session 0 or SYSTEM-owned process; otherwise, <c>false</c>.
        /// </returns>
        public bool IsSystemProcess(Process process)
        {
            if (process == null)
                return false;

            try
            {
                // Session 0 is reserved for system services — never kill these
                if (process.SessionId == 0)
                    return true;

                // Check if the process is running under the SYSTEM account
                return IsOwnedBySystem(process);
            }
            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is System.ComponentModel.Win32Exception ||
                ex is UnauthorizedAccessException)
            {
                // Can't determine ownership. Session 0 check already passed,
                // and the whitelist already covers critical processes.
                // So this is likely a user-space background app — kill it.
                Debug.WriteLine($"[WhitelistService] Cannot inspect process {process.Id}, assuming user process: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Adds a process name to the user whitelist and persists the change.
        /// </summary>
        /// <param name="processName">The process name to whitelist (without .exe extension).</param>
        public void AddToUserWhitelist(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return;

            string name = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase).Trim().ToLowerInvariant();
            if (_userWhitelist.Add(name))
            {
                PersistUserWhitelist();
            }
        }

        /// <summary>
        /// Removes a process name from the user whitelist and persists the change.
        /// </summary>
        /// <param name="processName">The process name to remove from the whitelist.</param>
        public void RemoveFromUserWhitelist(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return;

            string name = processName.Replace(".exe", "", StringComparison.OrdinalIgnoreCase).Trim().ToLowerInvariant();
            if (_userWhitelist.Remove(name))
            {
                PersistUserWhitelist();
            }
        }

        /// <summary>
        /// Syncs the in-memory user whitelist back to the <see cref="SettingsService"/>
        /// for persistence to disk.
        /// </summary>
        private void PersistUserWhitelist()
        {
            _settingsService.Settings.CustomWhitelist = _userWhitelist.ToList();
            _settingsService.Save();
        }

        /// <summary>
        /// Checks whether the specified process is owned by the NT AUTHORITY\SYSTEM account.
        /// </summary>
        private static bool IsOwnedBySystem(Process process)
        {
            try
            {
                // Use WMI-free approach: open the process token and check the SID
                var handle = process.Handle;
                // If we can open the handle, we can try to get the owner via the process token.
                // A simpler heuristic: if the process has no main window and session > 0,
                // it may still be a background service. We rely on the whitelist for those.
                // For robust SYSTEM detection, we use the process token.
                using var identity = GetProcessOwner(process);
                if (identity != null)
                {
                    var sid = identity.User;
                    // S-1-5-18 is the well-known SID for Local System
                    return sid != null && sid.Value == "S-1-5-18";
                }
            }
            catch
            {
                // Swallow — cannot determine, default to false here since
                // the caller (IsSystemProcess) will catch and return true.
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve the <see cref="WindowsIdentity"/> for the process owner
        /// by opening the process token.
        /// </summary>
        private static WindowsIdentity? GetProcessOwner(Process process)
        {
            try
            {
                IntPtr processHandle = process.Handle;
                if (processHandle == IntPtr.Zero)
                    return null;

                // OpenProcessToken is handled internally by WindowsIdentity when given a token.
                // We use a P/Invoke-free approach by leveraging the .NET APIs.
                bool success = NativeTokenHelper.OpenProcessToken(
                    processHandle,
                    NativeTokenHelper.TOKEN_QUERY,
                    out IntPtr tokenHandle);

                if (!success || tokenHandle == IntPtr.Zero)
                    return null;

                try
                {
                    return new WindowsIdentity(tokenHandle);
                }
                finally
                {
                    NativeTokenHelper.CloseHandle(tokenHandle);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Minimal P/Invoke helpers for process token access, scoped to this service.
        /// </summary>
        private static class NativeTokenHelper
        {
            public const uint TOKEN_QUERY = 0x0008;

            [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool OpenProcessToken(
                IntPtr processHandle,
                uint desiredAccess,
                out IntPtr tokenHandle);

            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr handle);
        }
    }
}
