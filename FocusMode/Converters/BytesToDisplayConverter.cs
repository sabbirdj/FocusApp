namespace FocusMode.Converters;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts a byte count (<see cref="long"/>) to a human-readable size string
/// (e.g. "6.1 GB", "340 MB", "12 KB"). Intended for one-way data binding
/// of memory usage values in the UI.
/// </summary>
public class BytesToDisplayConverter : IValueConverter
{
    private const double KB = 1024;
    private const double MB = KB * 1024;
    private const double GB = MB * 1024;
    private const double TB = GB * 1024;

    /// <summary>
    /// Converts a <see cref="long"/> byte count to a formatted display string.
    /// </summary>
    /// <param name="value">The byte count (expected type: <see cref="long"/>).</param>
    /// <param name="targetType">Not used.</param>
    /// <param name="parameter">Not used.</param>
    /// <param name="language">Not used.</param>
    /// <returns>A human-readable string like "6.1 GB" or "340 MB".</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not long bytes)
        {
            return "0 B";
        }

        return bytes switch
        {
            >= (long)TB => $"{bytes / TB:F1} TB",
            >= (long)GB => $"{bytes / GB:F1} GB",
            >= (long)MB => $"{bytes / MB:F0} MB",
            >= (long)KB => $"{bytes / KB:F0} KB",
            _ => $"{bytes} B"
        };
    }

    /// <summary>
    /// Not supported. This converter is one-way only.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException("BytesToDisplayConverter is one-way only.");
    }
}
