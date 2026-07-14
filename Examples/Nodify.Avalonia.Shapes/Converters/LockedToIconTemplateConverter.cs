using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Nodify.Avalonia.Shapes.Converters
{
    public sealed class LockedToIconTemplateConverter : IValueConverter
    {
        public static readonly LockedToIconTemplateConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string key = value is true ? "LockIcon" : "LockOpenIcon";
            if (Application.Current is { } app && app.TryFindResource(key, out object? resource))
            {
                return resource;
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
