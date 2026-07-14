using Avalonia;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Shared;

namespace Nodify.Avalonia.Shapes.Canvas
{
    public class ConnectorViewModel : ObservableObject
    {
        public ConnectorViewModel(ConnectorPosition position)
        {
            Position = position;
        }

        private Point _anchor;
        public Point Anchor
        {
            get => _anchor;
            set => SetProperty(ref _anchor, value);
        }

        public ConnectorPosition Position { get; }
    }
}
