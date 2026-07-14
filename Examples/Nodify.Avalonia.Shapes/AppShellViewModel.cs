using Nodify.Avalonia.Shared;
using Nodify.Avalonia.Shapes.Canvas;

namespace Nodify.Avalonia.Shapes
{
    public class AppShellViewModel : ObservableObject
    {
        public CanvasViewModel Canvas { get; } = new CanvasViewModel();
    }
}
