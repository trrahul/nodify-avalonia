using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Nodify.Avalonia.Playground.Editor;

namespace Nodify.Avalonia.Playground.Converters
{
    /// <summary>
    /// Stable AutomationId for a node view-model; falls back to the runtime type name (plus a short
    /// discriminator) when the node has no <c>Title</c>.
    /// </summary>
    public class NodeAutomationIdConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? title = value switch
            {
                FlowNodeViewModel flow => flow.Title,
                CommentNodeViewModel comment => comment.Title,
                _ => null
            };

            if (!string.IsNullOrEmpty(title))
            {
                return $"Node_{title}";
            }

            if (value is NodeViewModel node)
            {
                return $"Node_{node.GetType().Name}_{node.GetHashCode():X8}";
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
