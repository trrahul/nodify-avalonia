using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.EditorStates;
using Nodify.Avalonia.Helpers;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class ContainerState
    {
        internal sealed class Dragging : InputElementStateStack<ItemContainer>.DragState
        {
            private Point _initial;
            private Point _previous;
            private Point _current;

            // Set only via explicit cancel/end paths; a capture-loss exit counts as a non-canceled end, not a cancellation.
            public bool Canceled { get; private set; } = false;

            protected override bool CanCancel => NodifyEditor.AllowDraggingCancellation;

            protected override bool IsToggle => ContainerState.EnableToggledDraggingMode;

            protected override InputGesture DragGesture => Element.ActualGestures.ItemContainer.Drag;

            protected override InputGesture? CancelGesture => Element.ActualGestures.ItemContainer.CancelAction;

            public Dragging(InputElementStateStack<ItemContainer> stack) : base(stack)
            {
            }

            // Seed the drag origin from the event position in graph space; the container handles captured events before the editor refreshes Editor.MouseLocation.
            protected override void OnBegin(RoutedEventArgs e)
            {
                // Use the editor as PositionElement so DragState.EndDrag's right-drag
                // context-menu suppression threshold is measured in editor space.
                PositionElement = Element.Editor;

                _initial = GetGraphPosition(e);
                _current = _initial;
                Element.IsSelected = true;
                Element.IsPreviewingLocation = true;
                Element.RaiseEvent(new VectorEventArgs
                {
                    Vector = new Vector(_initial.X, _initial.Y),
                    RoutedEvent = ItemContainer.DragStartedEvent
                });
                _previous = _initial;
            }

            // Read the current event position so deltas aren't one event behind the captured container input.
            protected override void OnMouseMove(PointerEventArgs e)
            {
                _current = Element.Editor.GetLocationInsideEditor(e);
                var delta = _current - _previous;
                Element.RaiseEvent(new VectorEventArgs
                {
                    Vector = new Vector(delta.X, delta.Y),
                    RoutedEvent = ItemContainer.DragDeltaEvent
                });
                _previous = _current;
            }

            protected override void OnEnd(RoutedEventArgs e)
            {
                RaiseDragCompleted(canceled: false);
                base.OnEnd(e);  // pops the dragging state → SelectingState.Enter runs
            }

            protected override void OnCancel(RoutedEventArgs e)
            {
                RaiseDragCompleted(canceled: true);
                base.OnCancel(e);
            }

            private void RaiseDragCompleted(bool canceled)
            {
                Canceled = canceled;
                Element.IsPreviewingLocation = false;
                var delta = _current - _initial;
                Element.RaiseEvent(new DragCompletedEventArgs
                {
                    Vector = new Vector(delta.X, delta.Y),
                    Canceled = canceled,
                    RoutedEvent = ItemContainer.DragCompletedEvent
                });
            }

            private Point GetGraphPosition(RoutedEventArgs e)
                => e is PointerEventArgs pointerArgs
                    ? Element.Editor.GetLocationInsideEditor(pointerArgs)
                    : Element.Editor.MouseLocation;
        }
    }
}
