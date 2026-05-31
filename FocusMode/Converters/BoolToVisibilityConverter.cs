namespace FocusMode.Converters;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Converts a <see cref="bool"/> value to a <see cref="Visibility"/> value.
/// <c>true</c> maps to <see cref="Visibility.Visible"/>;
/// <c>false</c> maps to <see cref="Visibility.Collapsed"/>.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean to a <see cref="Visibility"/> enum value.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    /// <param name="targetType">Not used.</param>
    /// <param name="parameter">
    /// Optional. Pass the string "Invert" to reverse the mapping
    /// (<c>true</c> → Collapsed, <c>false</c> → Visible).
    /// </param>
    /// <param name="language">Not used.</param>
    /// <returns><see cref="Visibility.Visible"/> or <see cref="Visibility.Collapsed"/>.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var boolValue = value is true;

        // Support an optional "Invert" parameter for convenience.
        if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Converts a <see cref="Visibility"/> value back to a boolean.
    /// </summary>
    /// <param name="value">The <see cref="Visibility"/> value.</param>
    /// <param name="targetType">Not used.</param>
    /// <param name="parameter">
    /// Optional. Pass "Invert" to reverse the mapping.
    /// </param>
    /// <param name="language">Not used.</param>
    /// <returns><c>true</c> if Visible; <c>false</c> if Collapsed.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var isVisible = value is Visibility.Visible;

        if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible;
    }
}
