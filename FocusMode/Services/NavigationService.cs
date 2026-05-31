// Copyright (c) FocusMode. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace FocusMode.Services;

/// <summary>
/// Simple navigation helper wrapping <see cref="Frame.Navigate(Type, object)"/>
/// for page navigation within the application. Designed for constructor-injection
/// via DI; the hosting shell sets <see cref="Frame"/> after the window is created.
/// </summary>
public class NavigationService
{
    private Frame? _frame;

    /// <summary>
    /// Gets or sets the root navigation <see cref="Frame"/>.
    /// This must be set by the shell/window before any navigation calls.
    /// </summary>
    public Frame? Frame
    {
        get => _frame;
        set => _frame = value;
    }

    /// <summary>
    /// Gets a value indicating whether backward navigation is possible.
    /// </summary>
    public bool CanGoBack => _frame?.CanGoBack ?? false;

    /// <summary>
    /// Raised after a successful navigation. The event argument is the
    /// <see cref="Type"/> of the page that was navigated to.
    /// </summary>
    public event EventHandler<Type>? Navigated;

    /// <summary>
    /// Navigates to the specified page type using a generic type parameter.
    /// </summary>
    /// <typeparam name="T">
    /// The <see cref="Page"/> subclass to navigate to.
    /// </typeparam>
    /// <param name="parameter">
    /// Optional navigation parameter passed to the target page.
    /// </param>
    public void NavigateTo<T>(object? parameter = null) where T : Page
    {
        if (_frame == null) return;

        _frame.Navigate(typeof(T), parameter);
        Navigated?.Invoke(this, typeof(T));
    }

    /// <summary>
    /// Navigates to the specified page type.
    /// </summary>
    /// <param name="pageType">The <see cref="Type"/> of the page to navigate to.</param>
    /// <param name="parameter">
    /// Optional navigation parameter passed to the target page.
    /// </param>
    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame == null) return;

        _frame.Navigate(pageType, parameter);
        Navigated?.Invoke(this, pageType);
    }

    /// <summary>
    /// Navigates backward one entry in the back stack, if possible.
    /// </summary>
    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
        }
    }
}
