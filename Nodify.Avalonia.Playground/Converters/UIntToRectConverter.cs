using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;

namespace Nodify.Avalonia.Playground.Converters
{
    public class UIntToRectConverter : MarkupExtension, IValueConverter
    {
        public uint Multiplier { get; set; } = 1;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint size = System.Convert.ToUInt32(value) * Multiplier;
            return new RelativeRect(default,new Size(size, size),RelativeUnit.Absolute);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
            => this;
    }
}
