using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Nodify.Avalonia.Playground.Editor;

namespace Nodify.Avalonia.Playground.Converters
{
    /// <summary>
    /// Circle (the default) returns <see cref="AvaloniaProperty.UnsetValue"/> so the control keeps
    /// its built-in connector template.
    /// </summary>
    public class ConnectorShapeToTemplateConverter : IValueConverter
    {
        public ControlTemplate? Square { get; set; }
        public ControlTemplate? Triangle { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ConnectorShape shape)
            {
                return shape switch
                {
                    ConnectorShape.Square => Square ?? AvaloniaProperty.UnsetValue,
                    ConnectorShape.Triangle => Triangle ?? AvaloniaProperty.UnsetValue,
                    _ => AvaloniaProperty.UnsetValue,
                };
            }

            return AvaloniaProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Circle returns <see cref="AvaloniaProperty.UnsetValue"/> so the control theme's default
    /// brush is not overridden.
    /// </summary>
    public class ConnectorShapeToBrushConverter : IValueConverter
    {
        public IBrush? Square { get; set; }
        public IBrush? Triangle { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ConnectorShape shape)
            {
                return shape switch
                {
                    ConnectorShape.Square => (object?)Square ?? AvaloniaProperty.UnsetValue,
                    ConnectorShape.Triangle => (object?)Triangle ?? AvaloniaProperty.UnsetValue,
                    _ => AvaloniaProperty.UnsetValue,
                };
            }

            return AvaloniaProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// [0] = ConnectorShape, [1] = fallback IBrush. Used for connection TextBackground, which
    /// (unlike BorderBrush) has no ControlTheme default to fall back to on its own.
    /// </summary>
    public class ConnectorShapeToBrushOrDefaultConverter : IMultiValueConverter
    {
        public IBrush? Square { get; set; }
        public IBrush? Triangle { get; set; }

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            IBrush? fallback = values.Count > 1 ? values[1] as IBrush : null;

            if (values.Count > 0 && values[0] is ConnectorShape shape)
            {
                return shape switch
                {
                    ConnectorShape.Square => (object?)Square ?? fallback ?? AvaloniaProperty.UnsetValue,
                    ConnectorShape.Triangle => (object?)Triangle ?? fallback ?? AvaloniaProperty.UnsetValue,
                    _ => (object?)fallback ?? AvaloniaProperty.UnsetValue,
                };
            }

            return (object?)fallback ?? AvaloniaProperty.UnsetValue;
        }
    }

    /// <summary>Circle falls back to <see cref="Default"/>.</summary>
    public class ConnectorShapeToOutlineBrushConverter : IValueConverter
    {
        public IBrush? Square { get; set; }
        public IBrush? Triangle { get; set; }
        public IBrush? Default { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ConnectorShape shape)
            {
                return shape switch
                {
                    ConnectorShape.Square => (object?)Square ?? Default ?? Brushes.Transparent,
                    ConnectorShape.Triangle => (object?)Triangle ?? Default ?? Brushes.Transparent,
                    _ => (object?)Default ?? Brushes.Transparent,
                };
            }

            return (object?)Default ?? Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Circle returns <see cref="Default"/>.</summary>
    public class ConnectorShapeToHeaderTemplateConverter : IValueConverter
    {
        public IDataTemplate? Default { get; set; }
        public IDataTemplate? Square { get; set; }
        public IDataTemplate? Triangle { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ConnectorShape shape)
            {
                return shape switch
                {
                    ConnectorShape.Square => (object?)Square ?? AvaloniaProperty.UnsetValue,
                    ConnectorShape.Triangle => (object?)Triangle ?? AvaloniaProperty.UnsetValue,
                    _ => (object?)Default ?? AvaloniaProperty.UnsetValue,
                };
            }

            return (object?)Default ?? AvaloniaProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
