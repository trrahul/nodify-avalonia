using Avalonia.Input;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Workflow.Designer
{
    internal static class DesignerGesturesExtensions
    {
        /// <summary>
        /// Makes an editor read-only by unbinding every gesture that could modify the workflow,
        /// then reassigns panning to any mouse button. The library's top-level
        /// <see cref="EditorGestures.Unbind"/> omits GroupingNode, so the relevant sub-gestures
        /// are unbound explicitly.
        /// </summary>
        public static void LockEditing(this EditorGestures gestures)
        {
            gestures.Editor.Selection.Unbind();
            gestures.Editor.SelectAll.Unbind();
            gestures.Editor.Cutting.Unbind();
            gestures.Editor.PushItems.Unbind();

            gestures.Editor.Keyboard.ToggleSelected.Unbind();
            gestures.Editor.Keyboard.DragSelection.Unbind();
            gestures.Editor.Keyboard.DeselectAll.Unbind();

            gestures.ItemContainer.Selection.Unbind();
            gestures.ItemContainer.Drag.Unbind();

            gestures.Connection.Disconnect.Unbind();
            gestures.Connection.Split.Unbind();
            gestures.Connection.Selection.Unbind();

            gestures.Connector.Connect.Unbind();
            gestures.Connector.Disconnect.Unbind();

            gestures.Editor.Pan.Value = new AnyGesture(
                new PointerGesture(MouseAction.LeftClick),
                new PointerGesture(MouseAction.RightClick),
                new PointerGesture(MouseAction.MiddleClick));
        }
    }
}
