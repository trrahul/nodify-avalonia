using Avalonia;
using Avalonia.Media;
using Nodify.Avalonia.Shared;

namespace Nodify.Avalonia.Shapes.Canvas
{
    public class UserCursorViewModel : ObservableObject, ICanvasDecorator
    {
        private Point _location;
        public Point Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        public string? Name { get; set; }

        public Color Color { get; set; }
    }
}
