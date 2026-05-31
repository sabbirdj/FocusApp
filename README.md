# FocusMode

FocusMode is a lightweight, high-performance utility for Windows that helps you maximize your system's resources (RAM & CPU) and eliminate distractions with a single click. Designed for gamers, developers, and power users, FocusMode temporarily "ghosts" unnecessary background applications so your active tasks get 100% of your machine's power.

## How It Works

Unlike traditional "game boosters" that forcefully kill background processes (risking data loss) or suspend them (causing Windows Explorer crashes), FocusMode uses a highly stable, non-destructive engine:

- **Memory Flushing:** Forces background apps to flush their working set (RAM) to the page file via `EmptyWorkingSet`, instantly freeing up gigabytes of physical memory.
- **CPU Throttling:** Lowers the CPU priority of background apps to `Idle`, ensuring they use exactly 0% CPU while your focused app is demanding resources.
- **Visual Ghosting:** Hides distracting windows from your screen and taskbar without actually terminating the applications.
- **Seamless Restoration:** When you're done, FocusMode restores all windows, memory access, and CPU priorities exactly as they were. No unsaved work is ever lost.

## Features

- ⚡ **One-Click Optimization:** Instantly ghost all non-essential apps.
- 🛡️ **Intelligent Safelisting:** Built-in protection prevents critical system processes (Session 0, `C:\Windows`, UWP infrastructure) from being affected, ensuring zero system instability.
- ⌨️ **Global Hotkeys:** Toggle FocusMode instantly using `Ctrl+Alt+F`.
- 🔋 **System Tray Integration:** Runs quietly in the background with quick access from the Windows system tray.
- 🎨 **Modern WinUI 3 Design:** A sleek, premium dark-mode interface with glassmorphism effects.

## Requirements

- Windows 10 (Version 1809 or later) or Windows 11
- .NET 8.0 Desktop Runtime

## Installation & Compilation

If you want to compile this project yourself:

1. Clone the repository:
   ```bash
   git clone https://github.com/sabbirdj/FocusApp.git
   ```
2. Open the solution in **Visual Studio 2022** (Ensure the `.NET desktop development` and `Windows App SDK` workloads are installed).
3. Build the solution in `Release` mode.
4. Alternatively, use the .NET CLI to publish a self-contained executable:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=false
   ```

## Usage

1. Launch **FocusMode**.
2. From the Dashboard, select the apps you want to *keep active* (your games or primary work apps).
3. Click **Activate Focus Mode** (or press `Ctrl+Alt+F`).
4. All other apps will vanish from the taskbar and release their RAM/CPU.
5. When finished, open the tray menu or the app and click **Restore** to seamlessly bring everything back.

## Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the [issues page](https://github.com/sabbirdj/FocusApp/issues).

## License

This project is open-source and available under the [MIT License](LICENSE).
