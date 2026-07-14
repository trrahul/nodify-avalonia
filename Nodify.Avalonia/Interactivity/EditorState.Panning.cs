using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.Helpers;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class EditorState
    {
        /// <summary>
        /// Handles panning of the <see cref="NodifyEditor"/> viewport by dragging with the configured pan gesture.
        /// </summary>
        public class Panning : DragState<NodifyEditor>
        {
            private Point _previous;

            /// <inheritdoc />
            protected override InputGesture DragGesture => Element.ActualGestures.Editor.Pan;

            /// <inheritdoc />
            protected override InputGesture? CancelGesture => Element.ActualGestures.Editor.CancelAction;

            /// <inheritdoc />
            protected override bool CanBegin => IsPanningAllowed();

            /// <inheritdoc />
            protected override bool CanCancel => NodifyEditor.AllowPanningCancellation;

            /// <inheritdoc />
            protected override bool IsToggle => NodifyEditor.EnableToggledPanningMode;

            /// <inheritdoc />
            protected override bool HasContextMenu => Element.HasContextMenu;

            /// <summary>Initializes a new instance of <see cref="Panning"/>.</summary>
            public Panning(NodifyEditor editor) : base(editor)
            {
            }

            /// <inheritdoc />
            protected override void OnBegin(RoutedEventArgs e)
            {
                // Delegates to the public operation API (single implementation, shared with
                // keyboard/programmatic panning) — see NodifyEditor.BeginPanning().
                Element.BeginPanning();

                if (e is PointerEventArgs pe)
                {
                    _previous = pe.GetPosition(Element);
                }
            }

            /// <inheritdoc />
            protected override void OnMouseMove(PointerEventArgs e)
            {
                var current = e.GetPosition(Element);
                Element.UpdatePanning((current - _previous) / Element.ViewportZoom);
                _previous = current;
            }

            /// <inheritdoc />
            protected override void OnEnd(RoutedEventArgs e)
            {
                Element.EndPanning();
            }

            /// <inheritdoc />
            protected override void OnCancel(RoutedEventArgs e)
            {
                Element.CancelPanning();
            }

            private bool IsPanningAllowed()
                => !Element.DisablePanning
                    && (NodifyEditor.AllowPanningWhileSelecting || !Element.IsSelecting)
                    && (NodifyEditor.AllowPanningWhileCutting || !Element.IsCutting)
                    && (NodifyEditor.AllowPanningWhilePushingItems || !Element.IsPushingItems);
        }

        /// <summary>
        /// Handles panning of the <see cref="NodifyEditor"/> viewport using the mouse wheel with
        /// an optional modifier key.
        /// </summary>
        public class PanningWithMouseWheel : InputElementState<NodifyEditor>
        {
            /// <summary>Initializes a new instance of <see cref="PanningWithMouseWheel"/>.</summary>
            public PanningWithMouseWheel(NodifyEditor editor) : base(editor)
            {
            }

            /// <inheritdoc />
            protected override void OnMouseWheel(PointerWheelEventArgs e)
            {
                var gestures = Element.ActualGestures.Editor;

                if (gestures.PanWithMouseWheel && e.KeyModifiers == gestures.PanHorizontalModifierKey)
                {
                    double offset = System.Math.Sign(e.Delta.Y) * PointerHelper.PointerWheelDeltaForOneLine / 2d / Element.ViewportZoom;
                    Element.UpdatePanning(new Vector(offset, 0d));
                    e.Handled = true;
                }
                else if (gestures.PanWithMouseWheel && e.KeyModifiers == gestures.PanVerticalModifierKey)
                {
                    double offset = System.Math.Sign(e.Delta.Y) * PointerHelper.PointerWheelDeltaForOneLine / 2d / Element.ViewportZoom;
                    Element.UpdatePanning(new Vector(0d, offset));
                    e.Handled = true;
                }
            }
        }
    }
}
