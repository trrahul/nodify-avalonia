using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace Nodify.Avalonia.Workflow.Common
{
    /// <summary>
    /// Maps a <see cref="WorkflowIcon"/> value to a <see cref="Geometry"/> resource declared in
    /// <c>Themes/Icons.axaml</c> under the key <c>Icon.{Name}</c>. Falls back to <c>Icon.Warning</c>.
    /// </summary>
    public sealed class IconToGeometryConverter : IValueConverter
    {
        public static readonly IconToGeometryConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not WorkflowIcon icon || icon == WorkflowIcon.None)
            {
                return null;
            }

            var app = Application.Current;
            if (app is null)
            {
                return null;
            }

            var variant = app.ActualThemeVariant;

            if (app.TryGetResource($"Icon.{icon}", variant, out var res) && res is Geometry g)
            {
                return g;
            }

            if (app.TryGetResource("Icon.Warning", variant, out var fallback) && fallback is Geometry fg)
            {
                return fg;
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
