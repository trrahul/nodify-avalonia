using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Nodify.Avalonia.Playground.Converters
{
    /// <summary>
    /// Converts a string to a bool suitable for binding to <c>IsVisible</c>: true when the string is
    /// non-null/non-empty, false otherwise.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !string.IsNullOrEmpty(value as string);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
