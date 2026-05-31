# FocusMode — Windows App Build Specification

> A Windows focus productivity app that suspends all non-essential processes and evicts their RAM to the pagefile, leaving only the user's chosen app(s) running with maximum system resources.

---

## Overview

**App Name:** FocusMode (or similar clean branding)  
**Platform:** Windows 10/11  
**Language:** C# (.NET 8+)  
**UI Framework:** WPF or WinUI 3 (prefer WinUI 3 for modern look)  
**Privileges Required:** Administrator (must request UAC elevation on launch)

---

## Core Functionality

### 1. Focus Mode — ON
When the user activates Focus Mode:
1. Enumerate all running processes
2. Filter out the **system whitelist** (see below) — never touch these
3. Filter out the user's **selected focus app(s)**
4. For all remaining processes:
   - Call `NtSuspendProcess()` → freezes CPU usage to 0%
   - Call `EmptyWorkingSet(processHandle)` → evicts process RAM to pagefile on SSD
5. Store a list of all suspended process IDs for later resume
6. Show the user a summary: how many processes suspended, how much RAM freed

### 2. Focus Mode — OFF
When the user deactivates Focus Mode:
1. Iterate through all stored suspended process IDs
2. Call `NtResumeProcess()` on each
3. Windows automatically pages RAM back in as each app needs it
4. Clear the suspended process list
5. Show resume summary

### 3. Crash Recovery
On app launch, check if a `suspended_session.json` file exists from a previous crashed session. If yes, prompt the user:
> "A previous Focus session was not properly closed. Resume all suspended processes now?"
Auto-resume if confirmed.

---

## System Whitelist — NEVER Suspend These

These processes must be hardcoded as untouchable. Suspending them will crash or destabilize Windows:

```
csrss.exe         // Client/Server Runtime — BSOD if frozen
lsass.exe         // Security/auth — system lockout if frozen
smss.exe          // Session manager
wininit.exe       // Windows initialization
services.exe      // Service control manager
svchost.exe       // Host for critical Windows services
ntoskrnl.exe      // Kernel
dwm.exe           // Desktop Window Manager — black screen if frozen
winlogon.exe      // Login/session handler
audiodg.exe       // Audio driver host
fontdrvhost.exe   // Font driver
spoolsv.exe       // Print spooler
explorer.exe      // Taskbar and desktop shell
taskmgr.exe       // Task manager (user escape hatch)
registry          // Registry process
system            // System process
idle              // CPU idle process
unsecapp.exe      // WMI sink
wmiprvse.exe      // WMI provider
focusmode.exe     // This app itself
```

**Additional safety rules:**
- Skip any process running in **Session 0** (system services session): `process.SessionId == 0`
- Skip any process owned by the **SYSTEM** user that is not in a known user-launched list
- Gracefully handle `AccessDeniedException` — some protected/anti-cheat processes will refuse suspension; log and skip them silently

---

## Win32 / P/Invoke Signatures Needed

```csharp
// Suspend a process
[DllImport("ntdll.dll")]
public static extern uint NtSuspendProcess(IntPtr processHandle);

// Resume a process
[DllImport("ntdll.dll")]
public static extern uint NtResumeProcess(IntPtr processHandle);

// Evict process RAM to pagefile
[DllImport("psapi.dll")]
public static extern bool EmptyWorkingSet(IntPtr hProcess);

// Open process with required access
[DllImport("kernel32.dll")]
public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
```

---

## UI Design Requirements

### Overall Aesthetic
- **Dark theme** — deep charcoal/near-black background (`#0F0F0F` or `#141414`)
- **Accent color** — electric blue or violet (`#6366F1` indigo or `#3B82F6` blue)
- **Minimal, clean** — think Raycast, Linear, or Arc Browser aesthetic
- **Glassmorphism or frosted panels** for cards
- Smooth animations: fade-ins, slide transitions, pulsing "active" state
- Custom window chrome — remove default Windows titlebar, use custom draggable header
- Rounded corners (12–16px radius on panels)

---

### Screen 1 — Home / Dashboard

**Layout:**
```
┌─────────────────────────────────────────────┐
│  ⬡ FocusMode              [─] [□] [✕]       │  ← custom titlebar, draggable
├─────────────────────────────────────────────┤
│                                             │
│   RAM Usage         Processes Running       │
│   [████░░░] 9.2GB   47 active               │  ← live stats cards
│                                             │
│  ┌─────────────────────────────────────┐    │
│  │  🎯  Your Focus App                  │    │
│  │  [Search or select an app...]  [+]  │    │  ← searchable app picker
│  │                                     │    │
│  │  ✓ Visual Studio Code               │    │
│  │  ✓ Chrome                           │    │
│  └─────────────────────────────────────┘    │
│                                             │
│       [ ⚡ ACTIVATE FOCUS MODE ]            │  ← big glowing CTA button
│                                             │
└─────────────────────────────────────────────┘
```

---

### Screen 2 — Focus Mode Active

**Layout:**
```
┌─────────────────────────────────────────────┐
│  ⬡ FocusMode   🟢 FOCUS ACTIVE   [─][□][✕] │
├─────────────────────────────────────────────┤
│                                             │
│         ╔═══════════════════╗               │
│         ║  FOCUS MODE ON    ║               │  ← animated glowing ring
│         ║   ⏱  00:42:17    ║               │  ← session timer
│         ╚═══════════════════╝               │
│                                             │
│   34 processes suspended                   │
│   6.1 GB RAM freed                         │  ← live stats
│                                             │
│   Focus Apps Running:                       │
│   • VS Code    • Chrome                    │
│                                             │
│       [ ⏹ END FOCUS MODE ]                 │  ← end button
│                                             │
│   [!] 3 processes could not be suspended   │  ← soft warning, collapsible
└─────────────────────────────────────────────┘
```

---

### Screen 3 — Process Preview (Dry Run)

Before activating, show the user a scrollable list of what WILL be suspended:
- Process name + icon
- Current RAM usage
- Checkbox to manually exclude any process
- "Proceed" and "Cancel" buttons

---

### Screen 4 — Settings

- Toggle: **Launch at Windows startup**
- Toggle: **Show system tray icon**
- Toggle: **Dry run preview before activating**
- Toggle: **Auto-resume on app exit**
- Hotkey: **Global hotkey to toggle focus mode** (e.g. `Ctrl+Shift+F`)
- Pagefile size warning threshold
- Custom whitelist editor (add extra processes to never suspend)

---

## Data Model

### `FocusSession.cs`
```csharp
public class FocusSession
{
    public DateTime StartTime { get; set; }
    public List<SuspendedProcess> SuspendedProcesses { get; set; }
    public List<string> FocusApps { get; set; }
    public long RamFreedBytes { get; set; }
}

public class SuspendedProcess
{
    public int Pid { get; set; }
    public string Name { get; set; }
    public long WorkingSetBytes { get; set; }
    public bool ResumedSuccessfully { get; set; }
}
```

Persist `FocusSession` to `%AppData%\FocusMode\suspended_session.json` when focus mode activates, delete on clean deactivation.

---

## Process Enumeration Logic

```csharp
// Pseudocode for safe process filtering
var allProcesses = Process.GetProcesses();
var toSuspend = allProcesses
    .Where(p => !SystemWhitelist.Contains(p.ProcessName.ToLower()))
    .Where(p => p.SessionId != 0)
    .Where(p => !FocusApps.Contains(p.ProcessName.ToLower()))
    .Where(p => p.Id != currentAppPid)
    .ToList();
```

---

## Error Handling Rules

| Scenario | Behavior |
|---|---|
| Process exits before suspend | Catch `InvalidOperationException`, skip silently |
| Access denied on suspend | Log to list, show count in UI as "could not suspend" |
| Pagefile disabled/too small | Show warning banner before activation |
| App crashes during focus | On next launch, detect session file and auto-resume |
| User force-closes via Task Manager | Session file handles recovery |

---

## Project Structure

```
FocusMode/
├── App.xaml
├── MainWindow.xaml
├── Views/
│   ├── DashboardView.xaml
│   ├── FocusActiveView.xaml
│   ├── ProcessPreviewView.xaml
│   └── SettingsView.xaml
├── ViewModels/
│   ├── DashboardViewModel.cs
│   ├── FocusActiveViewModel.cs
│   └── SettingsViewModel.cs
├── Services/
│   ├── ProcessManager.cs        ← core suspend/resume/EmptyWorkingSet logic
│   ├── WhitelistService.cs      ← hardcoded + user whitelist
│   ├── SessionPersistence.cs    ← JSON save/load for crash recovery
│   └── SystemStatsService.cs    ← RAM usage, process count polling
├── Models/
│   ├── FocusSession.cs
│   └── SuspendedProcess.cs
├── Helpers/
│   ├── NativeMethods.cs         ← all P/Invoke declarations
│   └── ProcessExtensions.cs
└── Assets/
    └── icons, fonts, etc.
```

---

## NuGet Packages to Use

| Package | Purpose |
|---|---|
| `CommunityToolkit.Mvvm` | MVVM boilerplate, source generators |
| `Microsoft.Extensions.DependencyInjection` | DI container |
| `Newtonsoft.Json` or `System.Text.Json` | Session persistence |
| `Hardcodet.NotifyIcon.Wpf` | System tray icon (if WPF) |

---

## Manifest — UAC Elevation

The app MUST declare administrator privileges in its manifest:

```xml
<!-- app.manifest -->
<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
```

---

## Nice-to-Have Features (Stretch Goals)

- **Session history** — log past focus sessions with duration and RAM saved
- **Focus score** — gamify: streaks, total hours focused, RAM saved lifetime
- **App usage during focus** — track if user switches away from focus app
- **Sound** — subtle activation/deactivation sound effect
- **Tray icon** — show focus status, quick toggle from system tray
- **Per-profile presets** — "Coding", "Gaming", "Writing" with different focus app sets

---

## Summary of Key Technical Points

1. Use `NtSuspendProcess` + `EmptyWorkingSet` — this is the core mechanism
2. The hardcoded whitelist is non-negotiable for stability
3. Skip Session 0 processes to avoid breaking Windows services
4. Persist session to disk immediately on activation for crash recovery
5. Run as Administrator — required for cross-process handle access
6. UI must feel premium: dark theme, smooth animations, custom chrome
7. Always show the user exactly what will happen before doing it (dry run option)
