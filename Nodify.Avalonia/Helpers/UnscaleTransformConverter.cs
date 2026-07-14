using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Nodify.Avalonia.Helpers
{
    internal class UnscaleTransformConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is double x && values[1] is double y)
            {
                return new ScaleTransform(1/x,1/y);
            }

            return new ScaleTransform(1, 1);
        }
    }

    public class NodifyConverters
    {
        public static IMultiValueConverter UnscaleDoubleConverter { get; } = new UnscaleDoubleConverter();
        public static IMultiValueConverter UnscaleTransformConverter { get; } = new UnscaleTransformConverter();
        public static IMultiValueConverter CuttingLineDeltaConverter { get; } = new CuttingLineDeltaConverter();
    }

    // Computes the CuttingLine's local EndPoint as (end - start) * ViewportZoom so it draws relative to a local
    // (0,0) at Canvas.Left/Top = start. Pre-scaling by zoom is required: the shared UnscaleTransformConverter
    // RenderTransform (1/zoom) and the outer viewport transform (zoom) would otherwise cancel, detaching the
    // cutting line from the cursor at any zoom != 1.
    internal class CuttingLineDeltaConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count >= 3 && values[0] is Point start && values[1] is Point end && values[2] is double zoom)
            {
                return new Point((end.X - start.X) * zoom, (end.Y - start.Y) * zoom);
            }

            return default(Point);
        }
    }

    internal class UnscaleDoubleConverter : IMultiValueConverter
    {

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is double x && values[1] is double y)
            {
                double result = x * y;
                return result;
            }

            return 0d;
        }
    }
}
