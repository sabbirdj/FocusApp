using System;

namespace FocusMode;

/// <summary>
/// Custom program entry point that sets required environment variables
/// for single-file self-contained deployment before WinUI initializes.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Required for PublishSingleFile: tells WinAppSDK where to find native DLLs
        Environment.SetEnvironmentVariable(
            "MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY",
            AppContext.BaseDirectory);

        global::WinRT.ComWrappersSupport.InitializeComWrappers();

        Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);

            _ = new App();
        });
    }
}
