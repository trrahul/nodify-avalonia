using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace Nodify.StateMachine
{
    /// <summary>
    /// [0] = available keys, [1] = the key type to filter by (instantiated as a static resource
    /// in XAML because multi-value converters can't be markup extensions).
    /// </summary>
    public class FilterBlackboardKeysConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count >= 2 && values[0] is IEnumerable<BlackboardKeyViewModel> keys && values[1] is BlackboardKeyType filter)
            {
                return keys.Where(k => k.Type == filter || filter == BlackboardKeyType.Object);
            }

            return values;
        }
    }
}
