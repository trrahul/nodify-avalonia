using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;

namespace Nodify.Avalonia.Workflow.Common
{
    public sealed class IconNotNoneConverter : IValueConverter
    {
        public static readonly IconNotNoneConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is WorkflowIcon i && i != WorkflowIcon.None;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Opacity for an icon slot: 0 when the icon is <see cref="WorkflowIcon.None"/>, 1 otherwise.
    /// Opacity rather than visibility keeps the slot's layout space reserved.
    /// </summary>
    public sealed class IconToOpacityConverter : IValueConverter
    {
        public static readonly IconToOpacityConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is WorkflowIcon icon && icon != WorkflowIcon.None ? 1d : 0d;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class KeyNotNoneConverter : IValueConverter
    {
        public static readonly KeyNotNoneConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is Key k && k != Key.None;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class ModifierNotNoneConverter : IValueConverter
    {
        public static readonly ModifierNotNoneConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is KeyModifiers m && m != KeyModifiers.None;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Converts a nullable <see cref="Color"/> to a brush; null yields a muted default.</summary>
    public sealed class IconColorToBrushConverter : IValueConverter
    {
        public static readonly IconColorToBrushConverter Instance = new();

        private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.FromArgb(0xC5, 0xFF, 0xFF, 0xFF));

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color c)
            {
                return new SolidColorBrush(c);
            }

            var app = Application.Current;
            if (app is not null
                && app.TryGetResource("TextFillColorSecondaryBrush", app.ActualThemeVariant, out var resource)
                && resource is IBrush brush)
            {
                return brush;
            }

            return DefaultBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Returns <c>true</c> when the bound value is not <c>null</c> (and non-empty for strings).</summary>
    public sealed class NotNullConverter : IValueConverter
    {
        public static readonly NotNullConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value switch
            {
                null => false,
                string s => !string.IsNullOrEmpty(s),
                _ => true
            };

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Fill brush for a command icon: mirrors the icon's stroke brush when the command's
    /// <see cref="IconVariant"/> is <see cref="IconVariant.Filled"/>, otherwise no fill.
    /// Expects two bound values: the <see cref="IconVariant"/> and the icon's stroke brush.
    /// </summary>
    public sealed class FilledIconBrushConverter : IMultiValueConverter
    {
        public static readonly FilledIconBrushConverter Instance = new();

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            => values is [IconVariant.Filled, IBrush stroke, ..] ? stroke : null;
    }

    /// <summary>
    /// Left margin for a command label: collapses to zero when the command has no icon.
    /// </summary>
    public sealed class IconToLabelMarginConverter : IValueConverter
    {
        public static readonly IconToLabelMarginConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is WorkflowIcon icon && icon != WorkflowIcon.None
                ? new Thickness(8, 0, 0, 0)
                : new Thickness(0);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>Formats a viewport zoom (0..n) as a percentage string, e.g. <c>100%</c>.</summary>
    public sealed class ZoomToPercentConverter : IValueConverter
    {
        public static readonly ZoomToPercentConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is double d ? d.ToString("P0", culture) : "100%";

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
