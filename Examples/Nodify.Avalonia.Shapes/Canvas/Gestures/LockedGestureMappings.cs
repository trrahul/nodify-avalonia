using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Shapes.Canvas
{
    public class LockedGestureMappings : EditorGestures
    {
        public static readonly LockedGestureMappings Instance = new LockedGestureMappings();

        public LockedGestureMappings()
        {
            Apply(UnboundGestureMappings.Instance);

            Editor.Pan.Value = new AnyGesture(
                new PointerGesture(MouseAction.LeftClick),
                new PointerGesture(MouseAction.RightClick),
                new PointerGesture(MouseAction.MiddleClick));
        }
    }
}
