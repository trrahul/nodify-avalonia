using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class EditorState
    {
        /// <summary>
        /// Handles the cutting operation in the <see cref="NodifyEditor"/>: dragging draws a line
        /// that removes all connections it intersects when released.
        /// </summary>
        public class Cutting : DragState<NodifyEditor>
        {
            /// <inheritdoc />
            protected override InputGesture DragGesture => Element.ActualGestures.Editor.Cutting;

            /// <inheritdoc />
            protected override InputGesture? CancelGesture => Element.ActualGestures.Editor.CancelAction;

            /// <inheritdoc />
            protected override bool CanBegin => !Element.IsSelecting && !Element.IsPanning && !Element.IsPushingItems;

            /// <inheritdoc />
            protected override bool CanCancel => NodifyEditor.AllowCuttingCancellation;

            /// <inheritdoc />
            protected override bool IsToggle => NodifyEditor.EnableToggledCuttingMode;

            /// <inheritdoc />
            protected override bool HasContextMenu => Element.HasContextMenu;

            /// <summary>Initializes a new instance of <see cref="Cutting"/>.</summary>
            public Cutting(NodifyEditor editor) : base(editor)
            {
            }

            /// <inheritdoc />
            protected override void OnBegin(RoutedEventArgs e)
                => Element.BeginCutting();

            /// <inheritdoc />
            protected override void OnMouseMove(PointerEventArgs e)
                => Element.UpdateCuttingLine(Element.MouseLocation);

            /// <inheritdoc />
            protected override void OnEnd(RoutedEventArgs e)
                => Element.EndCutting();

            /// <inheritdoc />
            protected override void OnCancel(RoutedEventArgs e)
                => Element.CancelCutting();
        }
    }
}
