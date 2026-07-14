using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Nodify.StateMachine
{
    /// <summary>
    /// [0] = the blackboard's available keys, [1] = the target <see cref="BlackboardKeyViewModel"/>,
    /// [2] (optional) = whether the key name is being edited.
    /// </summary>
    public class BlackboardKeyEditorConverter : IMultiValueConverter
    {
        /// <summary>
        /// Set to <c>true</c> for input keys (which can toggle between a literal value and a key
        /// reference) and <c>false</c> for output keys (always assigned a key reference).
        /// </summary>
        public bool CanChangeInputType { get; set; }

        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count >= 2 && values[0] is ICollection<BlackboardKeyViewModel> availableKeys && values[1] is BlackboardKeyViewModel target)
            {
                return new BlackboardKeyEditorViewModel
                {
                    AvailableKeys = availableKeys,
                    Target = target,
                    IsEditing = values.Count >= 3 && values[2] is bool b && b,
                    CanChangeInputType = CanChangeInputType && (target.Type != BlackboardKeyType.Object || target.CanChangeType),
                    CanChangeKeyType = target.CanChangeType
                };
            }

            return BindingOperations.DoNothing;
        }
    }
}
