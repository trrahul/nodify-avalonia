using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Nodify.Avalonia.Helpers
{
    internal class UnscaleTransformConverter : IMultiValueConverter
    {
        //public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
            
        //    var scaleTransform = (ScaleTransform)value;
        //    var transform = new ScaleTransform(1/scaleTransform.ScaleX,1/scaleTransform.ScaleY);
           
        //}

        //public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    return value;
        //}

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
    }

    internal class UnscaleDoubleConverter : IMultiValueConverter
    {

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values[0] is double && values[1] is double) //todo
            {
                double result = (double)values[0] * (double)values[1];
                return result;
            }

            return 0d;
        }
    }
}
