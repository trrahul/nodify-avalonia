using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Nodify.Avalonia.Shared.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public static readonly InverseBooleanConverter Instance = new InverseBooleanConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }

            throw new InvalidOperationException("The value must be of type bool.");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }

            throw new InvalidOperationException("The value must be of type bool.");
        }
    }
}
