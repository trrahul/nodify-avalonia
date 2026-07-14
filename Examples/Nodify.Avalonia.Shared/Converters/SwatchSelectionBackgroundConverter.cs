using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Nodify.Avalonia.Shared.Converters
{
    /// <summary>
    /// Multi-value converter: [0] = a swatch item's <see cref="Color"/>, [1] = <see cref="Controls.Swatches.SelectedColor"/>.
    /// Returns <see cref="Brushes.White"/> when the two colors are equal (highlighting the selected
    /// swatch), otherwise <see cref="Brushes.Transparent"/>.
    /// </summary>
    public class SwatchSelectionBackgroundConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count > 1 && values[0] is Color color && values[1] is Color selected && color == selected)
            {
                return Brushes.White;
            }

            return Brushes.Transparent;
        }
    }
}
