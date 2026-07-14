using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Nodify.Avalonia
{
    internal class SubtractConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count < 2) return 0d;
            if (values[0] is double a && values[1] is double b)
                return a - b;
            return 0d;
        }
    }
}
