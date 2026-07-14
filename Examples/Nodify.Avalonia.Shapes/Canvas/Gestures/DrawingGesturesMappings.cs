using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Shapes.Canvas
{
    public class DrawingGesturesMappings : EditorGestures
    {
        public static readonly DrawingGesturesMappings Instance = new DrawingGesturesMappings();

        public InputGestureRef Draw { get; }

        public DrawingGesturesMappings()
        {
            Apply(UnboundGestureMappings.Instance);

            Draw = new PointerGesture(MouseAction.LeftClick);
        }
    }
}
