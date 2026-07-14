using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class EditorState
    {
        /// <summary>
        /// Handles the "push items" operation in the <see cref="NodifyEditor"/>: dragging splits
        /// the graph along an axis (Horizontal/Vertical) and moves the items past the push line.
        /// Built on the same dragging machinery as container dragging
        /// (<see cref="NodifyEditor.BeginDragging(System.Collections.Generic.IEnumerable{ItemContainer})"/>).
        /// </summary>
        public class PushingItems : DragState<NodifyEditor>
        {
            private Point _prevPosition;

            /// <inheritdoc />
            protected override InputGesture DragGesture => Element.ActualGestures.Editor.PushItems;

            /// <inheritdoc />
            protected override InputGesture? CancelGesture => Element.ActualGestures.Editor.CancelAction;

            /// <inheritdoc />
            // Must not start while another editor interaction is active.
            protected override bool CanBegin => !Element.IsSelecting && !Element.IsPanning && !Element.IsCutting;

            /// <inheritdoc />
            protected override bool CanCancel => NodifyEditor.AllowPushItemsCancellation;

            /// <inheritdoc />
            protected override bool IsToggle => NodifyEditor.EnableToggledPushingItemsMode;

            /// <inheritdoc />
            protected override bool HasContextMenu => Element.HasContextMenu;

            /// <summary>Initializes a new instance of <see cref="PushingItems"/>.</summary>
            public PushingItems(NodifyEditor editor) : base(editor)
            {
            }

            /// <inheritdoc />
            protected override void OnBegin(RoutedEventArgs e)
                => _prevPosition = Element.MouseLocation;

            /// <inheritdoc />
            protected override void OnMouseMove(PointerEventArgs e)
            {
                if (Element.IsPushingItems)
                {
                    var delta = Element.MouseLocation - _prevPosition;
                    Element.UpdatePushedArea(new Vector(delta.X, delta.Y));
                    _prevPosition = Element.MouseLocation;
                }
                else if (Math.Abs(Element.MouseLocation.X - _prevPosition.X) >= NodifyEditor.MouseActionSuppressionThreshold)
                {
                    Element.BeginPushingItems(_prevPosition, Orientation.Horizontal);
                }
                else if (Math.Abs(Element.MouseLocation.Y - _prevPosition.Y) >= NodifyEditor.MouseActionSuppressionThreshold)
                {
                    Element.BeginPushingItems(_prevPosition, Orientation.Vertical);
                }
            }

            /// <inheritdoc />
            protected override void OnEnd(RoutedEventArgs e)
                => Element.EndPushingItems();

            /// <inheritdoc />
            protected override void OnCancel(RoutedEventArgs e)
                => Element.CancelPushingItems();
        }
    }
}
