using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Nodify.Avalonia.Shapes.Canvas;

namespace Nodify.Avalonia.Shapes.Converters
{
    public sealed class CanvasToolToCursorConverter : IValueConverter
    {
        public static readonly CanvasToolToCursorConverter Instance = new();

        // Each Cursor allocates a native handle; cache the two we ever return.
        private static readonly Cursor Arrow = new(StandardCursorType.Arrow);
        private static readonly Cursor Cross = new(StandardCursorType.Cross);

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is CanvasTool.None ? Arrow : Cross;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
