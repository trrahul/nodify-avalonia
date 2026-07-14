using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Nodify.Avalonia.Shared.Controls;

namespace Nodify.Avalonia.Shared.Converters
{
    /// <summary>
    /// Returns <see langword="true"/> when the <see cref="ResizeDirections"/> value has the flag
    /// named by the converter parameter, <see langword="false"/> otherwise.
    /// </summary>
    internal class ResizeDirectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not ResizeDirections resizeDirections)
            {
                return false;
            }

            if (parameter is not null && Enum.TryParse(parameter.ToString(), out ResizeDirections direction))
            {
                return resizeDirections.HasFlag(direction);
            }

            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
