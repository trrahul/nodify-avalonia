using System;
using System.Globalization;
using Avalonia.Data.Converters;
using global::Avalonia.Layout;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Playground.Editor;

namespace Nodify.Avalonia.Playground.Converters
{
    public class FlowToConnectorPositionConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ConnectionViewModel connection)
            {
                var connector = parameter is "Input" ? connection.Input : connection.Output;

                if (connector.Node is KnotNodeViewModel)
                {
                    var otherConnector = connection.Input == connector ? connection.Output : connection.Input;

                    if (otherConnector.Node is KnotNodeViewModel)
                    {
                        return ToPosition(connector == connection.Input ? ConnectorFlow.Input : ConnectorFlow.Output, connector.Node.Orientation);
                    }

                    return ToPosition(otherConnector.Flow == ConnectorFlow.Output ? ConnectorFlow.Input : ConnectorFlow.Output, connector.Node.Orientation);
                }

                return ToPosition(connector.Flow, connector.Node.Orientation);
            }

            return value;
        }

        private static ConnectorPosition ToPosition(ConnectorFlow flow, Orientation orientation)
        {
            if (orientation == Orientation.Horizontal)
            {
                return flow == ConnectorFlow.Output
                    ? ConnectorPosition.Right
                    : ConnectorPosition.Left;
            }

            return flow == ConnectorFlow.Output
                ? ConnectorPosition.Bottom
                : ConnectorPosition.Top;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
