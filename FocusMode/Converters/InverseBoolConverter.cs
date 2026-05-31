namespace FocusMode.Converters;

using Microsoft.UI.Xaml.Data;

/// <summary>
/// A two-way <see cref="IValueConverter"/> that inverts a boolean value.
/// Useful for binding UI elements where the logic needs to be negated
/// (e.g. disabling a control when a flag is <c>true</c>).
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    /// <summary>
    /// Inverts the incoming boolean value.
    /// </summary>
    /// <param name="value">The boolean value to invert.</param>
    /// <param name="targetType">Not used.</param>
    /// <param name="parameter">Not used.</param>
    /// <param name="language">Not used.</param>
    /// <returns>The logical negation of <paramref name="value"/>, or <c>false</c> if not a bool.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return false;
    }

    /// <summary>
    /// Inverts the value back (same operation as <see cref="Convert"/>).
    /// </summary>
    /// <param name="value">The boolean value to invert.</param>
    /// <param name="targetType">Not used.</param>
    /// <param name="parameter">Not used.</param>
    /// <param name="language">Not used.</param>
    /// <returns>The logical negation of <paramref name="value"/>, or <c>false</c> if not a bool.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return false;
    }
}
