using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace GitDesktop.App.Converters;

/// <summary>
/// Converts a resource-key string (e.g. "ThemePrimaryText") into the corresponding
/// <see cref="IBrush"/> from <see cref="Avalonia.Application.Current"/>'s resource dictionary.
/// Used in data templates where <c>{DynamicResource {Binding ForegroundKey}}</c> does not
/// reliably resolve in Avalonia compiled-XAML mode.
/// </summary>
public sealed class ResourceKeyBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            var app = Avalonia.Application.Current;
            if (app != null &&
                app.TryGetResource(key, ThemeVariant.Default, out var resource) &&
                resource is IBrush brush)
            {
                return brush;
            }
        }

        // Fallback: return a light-grey brush so text is always legible on dark backgrounds.
        return new SolidColorBrush(Color.Parse("#d4d4d4"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
