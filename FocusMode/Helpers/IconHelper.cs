namespace FocusMode.Helpers;

using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// Helper class for extracting application icons from executable files
/// and saving them as PNG images to a local cache directory.
/// </summary>
public static class IconHelper
{
    // ──────────────────────────────────────────────
    //  P/Invoke — Shell Icon Extraction
    // ──────────────────────────────────────────────

    /// <summary>Shell file info flags: retrieve the icon handle.</summary>
    private const uint SHGFI_ICON = 0x000000100;

    /// <summary>Shell file info flags: retrieve the small (16×16) icon.</summary>
    private const uint SHGFI_SMALLICON = 0x000000001;

    /// <summary>Shell file info flags: retrieve the large (32×32) icon.</summary>
    private const uint SHGFI_LARGEICON = 0x000000000;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbSizeFileInfo,
        uint uFlags);

    /// <summary>
    /// Destroys an icon and frees any memory the icon occupied.
    /// </summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Contains information about a file object, populated by <see cref="SHGetFileInfo"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Extracts the icon from the specified executable and saves it as a
    /// PNG file in <paramref name="cacheDirectory"/>. If the icon has
    /// already been cached, the existing path is returned immediately.
    /// </summary>
    /// <param name="processPath">
    /// Full path to the executable file (e.g. "C:\Program Files\app\app.exe").
    /// </param>
    /// <param name="cacheDirectory">
    /// Directory where cached icon PNGs are stored.
    /// Will be created if it does not exist.
    /// </param>
    /// <returns>
    /// The absolute path to the cached PNG icon, or <c>null</c> if extraction failed.
    /// </returns>
    public static async Task<string?> GetProcessIconPathAsync(string processPath, string cacheDirectory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
            {
                return null;
            }

            Directory.CreateDirectory(cacheDirectory);

            // Build a deterministic cache key from the file name.
            var fileName = Path.GetFileNameWithoutExtension(processPath);
            var cacheFile = Path.Combine(cacheDirectory, $"{fileName}.png");

            // Return cached icon if it already exists.
            if (File.Exists(cacheFile))
            {
                return cacheFile;
            }

            // Run icon extraction on a background thread to keep UI responsive.
            return await Task.Run(() => ExtractAndSaveIcon(processPath, cacheFile));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IconHelper] Failed to extract icon for '{processPath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Returns a path to a built-in default application icon.
    /// This is used when no process-specific icon could be extracted.
    /// </summary>
    /// <returns>Absolute path to the default icon asset.</returns>
    public static string GetDefaultIconPath()
    {
        // Points to a bundled asset shipped with the app package.
        var appDir = AppContext.BaseDirectory;
        return Path.Combine(appDir, "Assets", "DefaultAppIcon.png");
    }

    // ──────────────────────────────────────────────
    //  Private Implementation
    // ──────────────────────────────────────────────

    /// <summary>
    /// Uses <c>SHGetFileInfo</c> to obtain the icon handle for a file,
    /// converts it to a bitmap, and saves it as a PNG.
    /// </summary>
    private static string? ExtractAndSaveIcon(string exePath, string outputPath)
    {
        var shfi = new SHFILEINFO();
        var result = SHGetFileInfo(
            exePath,
            0,
            ref shfi,
            (uint)Marshal.SizeOf<SHFILEINFO>(),
            SHGFI_ICON | SHGFI_LARGEICON);

        if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            // Convert HICON → System.Drawing.Icon → PNG bytes.
            using var icon = System.Drawing.Icon.FromHandle(shfi.hIcon);
            using var bitmap = icon.ToBitmap();
            bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            return outputPath;
        }
        finally
        {
            DestroyIcon(shfi.hIcon);
        }
    }
}
