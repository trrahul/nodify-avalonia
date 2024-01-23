using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Nodify.Avalonia.Playground.Converters
{
    public class DebugConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine($"Value: {value} :: Parameter: {parameter}");
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine($"Value: {value} :: Parameter: {parameter}");
            return value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => this;
    }
}
