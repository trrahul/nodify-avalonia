using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.Helpers;
using Nodify.Avalonia.Helpers.Gestures;
using static Nodify.Avalonia.Helpers.SelectionHelper;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class EditorState
    {
        /// <summary>
        /// Handles rubber-band selection in the <see cref="NodifyEditor"/>.
        /// </summary>
        public class Selecting : DragState<NodifyEditor>
        {
            /// <inheritdoc />
            protected override InputGesture DragGesture => Element.ActualGestures.Editor.Selection.Select;

            /// <inheritdoc />
            protected override InputGesture? CancelGesture => Element.ActualGestures.Editor.Selection.Cancel;

            /// <inheritdoc />
            protected override bool CanBegin => Element.CanSelectMultipleItems && !Element.IsPanning && !Element.IsCutting && !Element.IsPushingItems;

            /// <inheritdoc />
            protected override bool CanCancel => NodifyEditor.AllowSelectionCancellation;

            /// <inheritdoc />
            protected override bool IsToggle => NodifyEditor.EnableToggledSelectingMode;

            /// <inheritdoc />
            protected override bool HasContextMenu => Element.HasContextMenu;

            /// <summary>Initializes a new instance of <see cref="Selecting"/>.</summary>
            public Selecting(NodifyEditor editor) : base(editor)
            {
            }

            /// <inheritdoc />
            protected override void OnBegin(RoutedEventArgs e)
            {
                // Delegates to the public operation API (single implementation, shared with
                // programmatic rubber-band selection) — see NodifyEditor.BeginSelecting(Point, SelectionType).
                var type = GetSelectionType(e);
                Element.BeginSelecting(Element.MouseLocation, type);
            }

            /// <inheritdoc />
            protected override void OnMouseMove(PointerEventArgs e)
            {
                Element.UpdateSelection(Element.MouseLocation);
            }

            /// <inheritdoc />
            protected override void OnEnd(RoutedEventArgs e)
            {
                Element.EndSelecting();
            }

            /// <inheritdoc />
            protected override void OnCancel(RoutedEventArgs e)
            {
                Element.CancelSelecting();
            }

            private SelectionType GetSelectionType(RoutedEventArgs e)
            {
                var selection = Element.ActualGestures.Editor.Selection;
                if (selection.Append.Matches(e.Source, e))
                {
                    return SelectionType.Append;
                }

                if (selection.Invert.Matches(e.Source, e))
                {
                    return SelectionType.Invert;
                }

                if (selection.Remove.Matches(e.Source, e))
                {
                    return SelectionType.Remove;
                }

                return SelectionType.Replace;
            }
        }
    }
}
